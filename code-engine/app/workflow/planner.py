from typing import List
from app.models.schemas import PlannedFix
from app.workflow.state import CodeRefineState


STRATEGY_MAP = {
    "redundancy": "Remove duplicate logic and consolidate into a single shared helper or function.",
    "efficiency": "Optimize loop traversals and eliminate redundant iterations over the dataset.",
    "dead_code": "Remove unused variables, dead code blocks, and unnecessary boilerplate.",
    "security": "Sanitize and mask raw sensitive outputs and apply safe access patterns.",
}


def fix_planner_node(state: CodeRefineState) -> dict:
    """Analyze aggregated findings and create an actionable, non-conflicting fix plan."""
    findings = state.get("aggregated_findings") or []
    plan: List[PlannedFix] = []

    for finding in findings:
        if finding.fix_available and finding.confidence >= 0.70:
            category_clean = finding.category.lower()
            strategy = STRATEGY_MAP.get(
                category_clean,
                "Refactor code to resolve the identified quality issue while preserving program behavior.",
            )

            plan.append(
                PlannedFix(
                    finding_id=finding.id,
                    file_path=finding.file or finding.file_path or "",
                    target_start_line=finding.start_line,
                    target_end_line=finding.end_line,
                    category=finding.category,
                    rationale=finding.recommendation or finding.description or finding.title,
                    proposed_strategy=strategy,
                )
            )

    if not plan:
        return {"fix_plan": [], "status": "no_fixes_needed"}

    return {"fix_plan": plan, "status": "fixes_planned"}
