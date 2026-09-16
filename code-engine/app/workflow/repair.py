from typing import List
from app.models.schemas import RepairAttempt
from app.workflow.patcher import patch_generator_node
from app.workflow.state import CodeRefineState


def retry_patch_generator_node(state: CodeRefineState) -> dict:
    """Prepare repair feedback from failed checks and re-generate patch."""
    retry_count = state.get("retry_count", 0) + 1
    v_res = state.get("verification_result")

    failed_checks = []
    error_lines = []
    if v_res and v_res.checks:
        for c in v_res.checks:
            if c.status in ("failed", "fail"):
                failed_checks.append(c)
                error_lines.append(f"[{c.check_type}] {c.output}")

    repair_feedback = "\n".join(error_lines) or (
        v_res.summary if v_res else "Verification failed. Refine patch to resolve errors."
    )

    # Log to repair history
    attempt = RepairAttempt(
        attempt_number=retry_count,
        failed_checks=failed_checks,
        feedback_prompt=repair_feedback,
        generated_diff=state.get("combined_diff", ""),
    )
    history: List[RepairAttempt] = list(state.get("repair_history") or [])
    history.append(attempt)

    # Update state with feedback and retry count, then invoke patch generation
    updated_state = dict(state)
    updated_state["repair_feedback"] = repair_feedback
    updated_state["retry_count"] = retry_count
    updated_state["repair_history"] = history

    patch_update = patch_generator_node(updated_state)

    return {
        "generated_patches": patch_update.get("generated_patches", []),
        "combined_diff": patch_update.get("combined_diff", ""),
        "retry_count": retry_count,
        "repair_feedback": repair_feedback,
        "repair_history": history,
        "status": "retry_patch_generated",
    }
