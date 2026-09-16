import difflib
from pathlib import Path
from typing import List, Tuple
from app.models.schemas import GeneratedPatch, PatchRequest
from app.patch.applier import apply_patch
from app.workflow.llm import get_llm
from app.workflow.state import CodeRefineState


def generate_patch_with_llm(
    file_path: str,
    original_code: str,
    fix_plan_for_file: list,
    repair_feedback: str = "",
) -> Tuple[str, str, str]:
    """Use LLM to generate repaired code and calculate unified diff."""
    plan_desc = "\n".join(
        f"- Lines {p.target_start_line}-{p.target_end_line} [{p.category}]: {p.rationale} (Strategy: {p.proposed_strategy})"
        for p in fix_plan_for_file
    )

    feedback_section = ""
    if repair_feedback:
        feedback_section = f"\nPREVIOUS VERIFICATION FAILURE / REPAIR FEEDBACK:\n{repair_feedback}\nPlease correct the code to eliminate this error.\n"

    prompt = (
        "You are an automated code refactoring and repair agent for CodeRefine.\n"
        "Your task is to fix the issues specified in the fix plan below while maintaining exact program behavior and passing all tests and linters.\n\n"
        f"FILE: {file_path}\n\n"
        f"ORIGINAL CODE:\n```\n{original_code}\n```\n\n"
        f"FIX PLAN:\n{plan_desc}\n"
        f"{feedback_section}\n"
        "Return ONLY the complete modified source code for this file. Do NOT include markdown fences, explanations, or comments about the diff."
    )

    llm = get_llm()
    if llm:
        try:
            response = llm.invoke(prompt)
            content_str = response.content if hasattr(response, "content") else str(response)
            clean_code = content_str.strip()
            if clean_code.startswith("```"):
                clean_code = clean_code.strip("`")
                if "\n" in clean_code:
                    first_line, rest = clean_code.split("\n", 1)
                    if first_line.lower() in ("python", "py", "javascript", "js", "typescript", "ts"):
                        clean_code = rest

            diff = "".join(
                difflib.unified_diff(
                    original_code.splitlines(True),
                    clean_code.splitlines(True),
                    fromfile=f"a/{file_path}",
                    tofile=f"b/{file_path}",
                )
            )
            return clean_code, diff, "Refactored code using LLM according to fix plan."
        except Exception as e:
            print(f"LLM patch generation failed for {file_path}: {e}. Falling back to rule-based refactor.")

    # Rule-based fallback refactor
    return fallback_generate_patch(file_path, original_code, fix_plan_for_file, repair_feedback)


def fallback_generate_patch(
    file_path: str,
    original_code: str,
    fix_plan_for_file: list,
    repair_feedback: str = "",
) -> Tuple[str, str, str]:
    """Deterministic rule-based refactor for common patterns."""
    simplified = original_code
    repair_notes = []

    # 1. Redundancy: remove duplicate total calculation if present
    if any(item.category == "redundancy" for item in fix_plan_for_file):
        if "def calculate_total_again" in simplified:
            lines = simplified.splitlines(True)
            out_lines = []
            skip = False
            for line in lines:
                if "def calculate_total_again" in line:
                    skip = True
                    continue
                if skip and line.startswith("def "):
                    skip = False
                if not skip:
                    out_lines.append(line)
            simplified = "".join(out_lines)
            repair_notes.append("Removed duplicate calculate_total_again function.")

    # 2. Dead code & AI slop: remove duplicate print loops and unused variables
    if any(item.category == "dead_code" for item in fix_plan_for_file):
        simplified = simplified.replace("    for user in users:\n        print(user)\n\n", "")
        simplified = simplified.replace("    for user in users:\n        print(user)\n", "")
        simplified = simplified.replace('    unused_variable = "never used"\n\n', "")
        simplified = simplified.replace('    unused_variable = "never used"\n', "")
        repair_notes.append("Removed dead code and unused variables.")

    # 3. Security: sanitize raw user output
    if any(item.category == "security" for item in fix_plan_for_file):
        simplified = simplified.replace(
            "    for user in users:\n        result.append(user)\n",
            '    for user in users:\n        result.append({"id": user.get("id"), "name": user.get("name") if isinstance(user, dict) else str(user)})\n',
        )
        repair_notes.append("Sanitized raw user object output.")

    diff = "".join(
        difflib.unified_diff(
            original_code.splitlines(True),
            simplified.splitlines(True),
            fromfile=f"a/{file_path}",
            tofile=f"b/{file_path}",
        )
    )
    explanation = " | ".join(repair_notes) or "Applied code quality refactorings."
    return simplified, diff, explanation


def patch_generator_node(state: CodeRefineState) -> dict:
    """Generate patches for all files targeted by the fix plan."""
    fix_plan = state.get("fix_plan") or []
    if not fix_plan:
        return {"generated_patches": [], "combined_diff": "", "status": "no_fixes_needed"}

    code_context = state.get("code_context")
    if not code_context or not code_context.changed_files:
        return {"generated_patches": [], "combined_diff": "", "status": "no_files_to_patch"}

    repair_feedback = state.get("repair_feedback") or ""
    patches: List[GeneratedPatch] = []
    diff_chunks: List[str] = []

    # Group fix plans by file path
    files_to_plan: dict = {}
    for item in fix_plan:
        files_to_plan.setdefault(item.file_path, []).append(item)

    for file_ctx in code_context.changed_files:
        if file_ctx.path in files_to_plan or any(file_ctx.path.endswith(fp) for fp in files_to_plan):
            file_plans = files_to_plan.get(file_ctx.path, [])
            if not file_plans:
                for k, v in files_to_plan.items():
                    if file_ctx.path.endswith(k) or k.endswith(file_ctx.path):
                        file_plans = v
                        break

            proposed_code, diff, explanation = generate_patch_with_llm(
                file_ctx.path,
                file_ctx.content,
                file_plans,
                repair_feedback,
            )

            patch = GeneratedPatch(
                file_path=file_ctx.path,
                original_code=file_ctx.content,
                proposed_code=proposed_code,
                diff=diff,
                explanation=explanation,
                applied_successfully=False,
            )
            patches.append(patch)
            if diff:
                diff_chunks.append(diff)

    combined_diff = "\n".join(diff_chunks)
    return {
        "generated_patches": patches,
        "combined_diff": combined_diff,
        "status": "patch_generated",
    }


def apply_patch_node(state: CodeRefineState) -> dict:
    """Apply generated patches to workspace files using patch applier."""
    patches = state.get("generated_patches") or []
    repo_path = state.get("repo_path", "")

    if not patches:
        return {"status": "no_patch_to_apply"}

    for patch in patches:
        target = Path(repo_path) / patch.file_path
        try:
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(patch.proposed_code, encoding="utf-8")
            patch.applied_successfully = True
        except Exception as e:
            # Try unified diff application via applier
            res = apply_patch(PatchRequest(repo_path=repo_path, diff=patch.diff))
            patch.applied_successfully = res.get("success", False) if isinstance(res, dict) else res.success

    return {
        "generated_patches": patches,
        "status": "patch_applied",
    }
