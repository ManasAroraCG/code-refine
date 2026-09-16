from typing import Dict, List, Tuple
from app.models.schemas import Finding
from app.workflow.state import CodeRefineState


SEVERITY_WEIGHTS = {
    "critical": 25,
    "high": 15,
    "medium": 10,
    "low": 5,
    "info": 2,
    "warning": 5,
}


def aggregator_node(state: CodeRefineState) -> dict:
    """Merge static findings and multi-agent AI findings, deduplicate, and compute quality score."""
    static_list = state.get("static_findings") or []
    ai_list = state.get("ai_findings") or []
    merged = list(static_list) + list(ai_list)

    deduped: Dict[Tuple[str, str, str], Finding] = {}
    for finding in merged:
        # Normalize key based on file and core issue description/title
        key = (
            finding.file.lower(),
            finding.title.lower().strip(),
            finding.description.lower().strip()[:60],
        )
        if key not in deduped:
            deduped[key] = finding
        else:
            # If AI and static found the same thing, prefer higher confidence
            if finding.confidence > deduped[key].confidence:
                deduped[key] = finding

    aggregated: List[Finding] = list(deduped.values())

    # Calculate overall repository quality score (100.0 baseline)
    total_deductions = sum(
        SEVERITY_WEIGHTS.get(item.severity.lower(), 5) for item in aggregated
    )
    quality_score = max(0.0, 100.0 - total_deductions)

    return {
        "aggregated_findings": aggregated,
        "quality_score": round(quality_score, 2),
        "status": "aggregated",
    }
