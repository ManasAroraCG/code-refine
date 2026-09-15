# Dev 3 Plan — Code Analysis + Docker Sandbox (`code-engine`)

> Owner: Dev 3
> Primary ownership: Code analysis + Docker
> Secondary responsibilities: Git workspace, AST, static tools, patch application, verification

This service (`code-engine/`) is the component that turns a raw GitHub PR/branch into a
**safe, isolated, analyzable workspace**, runs deterministic static analysis, applies
AI-generated patches, and verifies those patches inside Docker before anything reaches
a human for approval.

Reference: root [instructions.md](../instructions.md) — sections 8 (GitHub/repo handling),
11 (context pipeline), 12 (deterministic vs AI analysis), 13 (patch strategy),
14 (verification engine), 15 (Docker sandbox).

## 1. What Dev 3 owns in the architecture

```text
ASP.NET Core (Dev 1)
        │  calls FastAPI (Dev 2's /analyze, /generate-fix, /verify)
        ▼
FastAPI + LangGraph (Dev 2)
        │  needs: workspace, context, static findings, patch apply, verification
        ▼
code-engine (Dev 3)   <-- this plan
        │
        ├── Workspace Manager   → clone/checkout PR or branch safely
        ├── Context Builder     → changed files, AST, imports, related tests
        ├── Static Analysis     → lint / type-check / security (deterministic)
        ├── Patch Applier       → apply targeted diffs to workspace files
        └── Docker Sandbox      → run checks in isolation, aggregate verdict
```

Dev 3's output is consumed by Dev 2's agents (context + static findings) and by the
verification step that gates human approval. Dev 3 does **not** talk to GitHub directly
for write operations (that's Dev 1) — only read/clone access is needed here.

## 2. Target module layout

```text
code-engine/
  app/
    main.py                     # FastAPI app entrypoint (exists)
    workspace/
      manager.py                # clone/checkout (exists, extend it)
    context/
      builder.py                # changed files, AST, imports, related code/tests
      ast_utils.py               # language-aware parsing helpers (py ast / tree-sitter)
    analysis/
      runner.py                  # orchestrates deterministic tool runs
      tools/
        python_tools.py          # ruff, mypy/pyright wrappers
        js_tools.py               # eslint, tsc wrappers
        security_tools.py        # semgrep / bandit wrappers
    patch/
      applier.py                 # apply unified diffs to files in a workspace
      models.py                  # Patch/PatchResult pydantic models
    sandbox/
      docker_runner.py            # build/run ephemeral container, capture output
      verification.py             # aggregate syntax/lint/type/test/build/security -> verdict
    api/
      routes.py                   # /workspace, /context, /static-analysis, /apply-patch, /verify
    models/
      schemas.py                  # shared request/response pydantic schemas
  tests/
    ...
  requirements.txt
  Dockerfile                       # image used to run this service itself
```

Sandbox execution images (the containers used to *run untrusted code*) live under
`infrastructure/sandbox/` — separate from the `code-engine` service's own Dockerfile.

## 3. Component responsibilities (detailed)

### 3.1 Workspace Manager (`workspace/manager.py`)
Already has `create_workspace()` and `clone_repository()`. Extend with:
- `checkout_pr(repo_url, pr_number)` — fetch a PR ref (e.g. `refs/pull/<n>/head`) instead of just a branch.
- `get_changed_files(repo_path, base, head)` — `git diff --name-only base...head`.
- `cleanup(workspace_path)` — delete temp dir after use (called by sandbox teardown).
- Ensure clones are **shallow** (`--depth=1`) where possible to keep things fast.
- Never execute arbitrary commands from repo content itself outside the sandbox.

### 3.2 Context Builder (`context/builder.py`)
Implements section 11 of the plan — build the *structured context* sent to AI agents:
- List of changed files (from Workspace Manager).
- Changed functions/classes (via `ast` for Python, or a lightweight parser for JS/TS).
- Relevant imports / referenced symbols.
- Related test files (naive heuristic first: same name + `test_`/`.test.` prefix/suffix).
- Emit a single `CodeContext` pydantic object — this is the contract with Dev 2.

### 3.3 Static Analysis Runner (`analysis/`)
Deterministic tools only — no LLM calls here (keeps cost down per section 12):
- Python: `ruff` (lint), `mypy` or `pyright` (types).
- JS/TS: `eslint`, `tsc --noEmit`.
- Security: `semgrep` (cross-language) and/or `bandit` for Python.
- Normalize every tool's output into a common `Finding` shape (file, start/end line,
  severity, category, title, description) — mirrors the `findings` DB table so Dev 2/Dev 5
  don't need to re-map fields.

### 3.3a AI Analysis (temporary stand-in, `analysis/ai_stub.py` + `ai_runner.py`)
Dev 2 hasn't built the real agent service yet, so this is a **temporary** placeholder to be
deleted once Dev 2's LangGraph/NIM agents exist:
- Calls Gemini directly per changed file, seeded with that file's static findings so the
  model doesn't waste tokens re-reporting what ruff/mypy/bandit already caught.
- Normalizes output into the same `Finding` shape (`agent_type="ai:<category>"`).
- `analysis/aggregator.py` combines static + AI findings into one list via `POST /analyze` —
  this is the shape to forward downstream to a future fix-planner.
- Verified working end-to-end: static findings for lint/security issues, AI finding for a
  duplicated/dead-code loop, no overlap between the two.

### 3.4 Patch Applier (`patch/applier.py`)
Implements section 13:
- Accepts a structured patch (file path, original code, proposed code, or a unified diff).
- Applies it to a file inside a given workspace (use `git apply` for unified diffs, or
  direct line-range replacement for simple structured patches).
- Returns success/failure + resulting diff for storage in the `patches` table.
- Must be idempotent and reject patches that don't cleanly apply (surface as failure,
  not a partial/corrupt write).

### 3.5 Docker Sandbox + Verification (`sandbox/`)
Implements sections 14 and 15 — the core differentiator:
- `docker_runner.py`: spins up an ephemeral container from a controlled base image,
  mounts (copy, not bind-mount for isolation) the patched workspace, runs commands,
  captures stdout/stderr/exit code, and **always destroys the container** afterward
  (`try/finally`).
- `verification.py`: runs, in order, syntax check → lint → type check → tests → build →
  security scan; aggregates into one verdict: `passed`, `failed`, plus per-check results
  matching the `verification_results` table shape.
- Enforce timeouts and resource limits (CPU/memory) per container run — untrusted
  AI-modified code must never be able to hang or exhaust the host.
- No network access inside the sandbox container unless a check explicitly requires it
  (e.g. dependency install) — deny-by-default.

### 3.6 API layer (`api/routes.py`)
Expose internal endpoints that Dev 2's FastAPI/LangGraph orchestrator calls (or that live
in the same FastAPI app if this is one service):
- `POST /workspace` → clone/checkout, returns workspace id + path
- `POST /context` → build `CodeContext` for changed files
- `POST /static-analysis` → run deterministic tools, return `Finding[]`
- `POST /apply-patch` → apply one patch, return diff + status
- `POST /verify` → run sandbox verification, return aggregated verdict

These map directly onto the `POST /analyze`, `POST /generate-fix`, `POST /verify` contracts
in section 20 — confirm final request/response shapes with Dev 2 before Day 3.

## 4. Day-by-day plan (aligned to the 9-day hackathon plan)

- **Day 1 — Skeleton**
  - [x] `WorkspaceManager.create_workspace` / `clone_repository` (done)
  - [x] Add `checkout_pr` + `get_changed_files`
  - [x] Basic Python AST parse proof-of-concept in `context/ast_utils.py`
  - [x] FastAPI route stub: `POST /workspace`

- **Day 2 — Cloning + first context pass**
  - [x] Workspace cloning fully working (PR + branch)
  - [x] `context/builder.py` returns changed files + naive AST symbol list
  - [ ] Confirm `CodeContext` schema with Dev 2

- **Day 3 — First vertical slice (critical checkpoint)**
  - [x] Support the single path: PR → clone → context → findings (static + temporary AI stand-in via `POST /analyze`)
  - [x] `Finding` schema locked (shared by ruff/mypy/bandit/AI stand-in; confirm with Dev 2 once their agents exist)

- **Day 4 — Static tools for remaining agents**
  - [x] Integrate `ruff` wrapper (Python lint findings via `POST /static-analysis`)
  - [x] Integrate `mypy` (type-check findings)
  - [x] Integrate `bandit` for the Security agent's deterministic half

- **Day 5 — Patch application**
  - [x] `patch/applier.py`: apply structured patch / unified diff to a workspace file
  - [x] Return before/after diff for the `patches` table

- **Day 6 — Verification engine**
  - [x] Docker sandbox: syntax, lint, type check, tests, build, security
  - [x] Aggregate verdict (`passed`/`failed` per check + overall) — repair-retry hook is owned upstream by Dev 2 (their repair loop calls back into `/generate-fix` → `/apply-patch` → `/verify`)

- **Day 7 — Support GitHub write-back**
  - [ ] Ensure workspace/patch outputs are in a form Dev 1 can commit/push directly
  - [ ] No direct GitHub write access from `code-engine` itself

- **Day 8 — Integration + polish**
  - [ ] Error handling for clone failures, patch conflicts, sandbox timeouts
  - [ ] Tighten container resource/time limits

- **Day 9 — Demo stabilization**
  - [ ] Freeze; run the full demo PR through workspace → context → static analysis →
        patch apply → verify at least 5x to confirm stability

## 5. Contracts to confirm early with other devs

- **With Dev 2 (FastAPI/LangGraph):** `CodeContext` schema, `Finding` schema, patch
  request/response shape, verification verdict shape.
- **With Dev 5 (DB/DevOps):** `findings`, `patches`, `verification_results` table columns
  must match the pydantic models 1:1 to avoid mapping bugs.
- **With Dev 1 (ASP.NET/GitHub):** who owns the actual `git commit`/`git push`/PR creation
  (Dev 1) vs who owns clone/checkout/patch-apply/verify (Dev 3, this service).

## 6. Security/reliability notes specific to this service

- Treat every cloned repo and every AI-generated patch as **untrusted input**.
- All execution of repo code happens inside Docker, never on the host running FastAPI.
- No secrets/tokens should be baked into sandbox images; use short-lived, scoped
  credentials only where a check genuinely needs network/dependency access.
- Always clean up temp workspaces and containers, including on error paths.
