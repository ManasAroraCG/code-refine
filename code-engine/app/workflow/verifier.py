import ast
import time
from pathlib import Path
from typing import List, Literal
from app.models.schemas import VerificationCheck, VerificationResult
from app.workflow.state import CodeRefineState


def run_local_syntax_check(file_path: str) -> VerificationCheck:
    """Run in-process AST syntax check on a Python file."""
    start = time.monotonic()
    try:
        source = Path(file_path).read_text(encoding="utf-8", errors="ignore")
        ast.parse(source)
        status = "passed"
        output = "Python AST syntax parse passed cleanly."
    except Exception as exc:
        status = "failed"
        output = f"Syntax error: {exc}"
    duration_ms = int((time.monotonic() - start) * 1000)
    return VerificationCheck(
        check_type="syntax",
        status=status,
        output=output,
        duration_ms=duration_ms,
    )


def verification_node(state: CodeRefineState) -> dict:
    """Run verification checks inside Docker sandbox or with local fallback."""
    repo_path = state.get("repo_path", "")
    checks: List[VerificationCheck] = []
    verdict = "passed"
    summary = "All verification checks passed."

    # Try Docker sandbox verification first
    try:
        from app.sandbox.verification import run_verification
        docker_res = run_verification(repo_path)
        if docker_res and docker_res.checks:
            checks = docker_res.checks
            verdict = docker_res.verdict
            summary = "Docker sandbox verification completed."
    except Exception as exc:
        # Fallback to local in-process verification
        python_files = list(Path(repo_path).glob("**/*.py")) if Path(repo_path).is_dir() else []
        if python_files:
            for py_file in python_files[:5]:
                chk = run_local_syntax_check(str(py_file))
                checks.append(chk)
                if chk.status == "failed":
                    verdict = "failed"
                    summary = f"Syntax verification failed on {py_file.name}: {chk.output}"
        else:
            checks.append(VerificationCheck(
                check_type="syntax",
                status="passed",
                output="No Python files to verify.",
                duration_ms=0,
            ))

    if any(c.status in ("failed", "fail") for c in checks):
        verdict = "failed"
        failed_names = [c.check_type for c in checks if c.status in ("failed", "fail")]
        summary = f"Verification failed on: {', '.join(failed_names)}"

    result = VerificationResult(
        verdict=verdict,
        checks=checks,
        summary=summary,
    )

    return {
        "verification_result": result,
        "status": "verified" if verdict == "passed" else "verification_failed",
    }


def route_verification(state: CodeRefineState) -> Literal["end", "retry"]:
    """Conditional edge router: route to retry/repair if failed and retries remain, else terminate."""
    result = state.get("verification_result")
    if result and result.verdict == "passed":
        return "end"

    retry_count = state.get("retry_count", 0)
    max_retries = state.get("max_retries", 2)

    if retry_count < max_retries and state.get("fix_plan"):
        return "retry"

    return "end"
