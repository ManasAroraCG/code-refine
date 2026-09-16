import operator
from typing import Annotated, List, Optional, TypedDict
from app.models.schemas import (
    CodeContext,
    Finding,
    GeneratedPatch,
    PlannedFix,
    RepairAttempt,
    VerificationResult,
)


class CodeRefineState(TypedDict):
    analysis_id: str
    repo_path: str
    changed_files: List[str]
    code_context: CodeContext
    static_findings: List[Finding]
    ai_findings: Annotated[List[Finding], operator.add]
    aggregated_findings: List[Finding]
    quality_score: float
    fix_plan: List[PlannedFix]
    generated_patches: List[GeneratedPatch]
    combined_diff: str
    verification_result: Optional[VerificationResult]
    retry_count: int
    max_retries: int
    repair_feedback: Optional[str]
    repair_history: List[RepairAttempt]
    status: str
