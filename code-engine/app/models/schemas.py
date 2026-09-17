import re
import uuid
from typing import Any, List, Optional
from pydantic import BaseModel, Field, model_validator


# ==============================
# Workspace & Context Models
# ==============================

class CloneRequest(BaseModel):
    repo_url: str
    branch: Optional[str] = None
    pr_number: Optional[int] = None
    base_branch: Optional[str] = None


class WorkspaceResponse(BaseModel):
    workspace_id: str
    repo_path: str
    changed_files: List[str] = []


class ContextRequest(BaseModel):
    repo_path: str
    changed_files: List[str]


class Symbol(BaseModel):
    name: str
    symbol_type: str  # "function", "class", "async_function"
    start_line: int
    end_line: int


class ChangedFileContext(BaseModel):
    path: str
    content: str
    language: str = "python"
    symbols: List[Symbol] = Field(default_factory=list)


class CodeContext(BaseModel):
    changed_files: List[ChangedFileContext] = Field(default_factory=list)


# ==============================
# Finding Models
# ==============================

class Finding(BaseModel):
    id: str = Field(default_factory=lambda: f"fnd-{uuid.uuid4().hex[:8]}")
    agent_type: str = "ai:quality"
    category: str = "unknown"
    severity: str = "low"  # "info" | "low" | "medium" | "high" | "critical"
    file: str = ""
    file_path: Optional[str] = None
    start_line: int = 0
    end_line: int = 0
    title: str = ""
    description: str = ""
    recommendation: str = ""
    confidence: float = 0.85
    fix_available: bool = True

    @model_validator(mode="before")
    @classmethod
    def normalize_fields(cls, values: Any) -> Any:
        if isinstance(values, dict):
            # 1. Support both 'file' and 'file_path'
            f = values.get("file") or values.get("file_path") or ""
            values["file"] = f
            values["file_path"] = f

            # 2. Normalize confidence (coercing strings like "high", "medium", "0.95")
            conf = values.get("confidence")
            if isinstance(conf, str):
                conf_lower = conf.strip().lower()
                if conf_lower in ("high", "critical", "very high"):
                    values["confidence"] = 0.95
                elif conf_lower in ("medium", "med", "moderate"):
                    values["confidence"] = 0.80
                elif conf_lower in ("low", "info"):
                    values["confidence"] = 0.60
                else:
                    try:
                        values["confidence"] = float(conf)
                    except ValueError:
                        values["confidence"] = 0.85
            elif conf is None:
                values["confidence"] = 0.85
            elif isinstance(conf, (int, float)):
                values["confidence"] = max(0.0, min(1.0, float(conf)))

            # 3. Ensure start_line and end_line are integers
            for line_field in ("start_line", "end_line"):
                val = values.get(line_field)
                if isinstance(val, str):
                    try:
                        digits = re.sub(r"\D", "", val)
                        values[line_field] = int(digits) if digits else 0
                    except ValueError:
                        values[line_field] = 0
                elif val is None:
                    values[line_field] = 0

            # 4. Auto-generate ID if missing
            if not values.get("id"):
                values["id"] = f"fnd-{uuid.uuid4().hex[:8]}"

        return values


# ==============================
# Planning & Patch Models
# ==============================

class PlannedFix(BaseModel):
    finding_id: str = ""
    file_path: str
    target_start_line: int = 0
    target_end_line: int = 0
    category: str = ""
    rationale: str = ""
    proposed_strategy: str = ""


class GeneratedPatch(BaseModel):
    file_path: str
    original_code: str
    proposed_code: str
    diff: str = ""
    explanation: str = ""
    applied_successfully: bool = False


class PatchRequest(BaseModel):
    repo_path: str
    # unified diff mode
    diff: Optional[str] = None
    # line-range replacement mode
    file_path: Optional[str] = None
    start_line: Optional[int] = None
    end_line: Optional[int] = None
    new_code: Optional[str] = None


class PatchResult(BaseModel):
    success: bool
    diff: str = ""
    error: Optional[str] = None


# ==============================
# Verification Models
# ==============================

class VerifyRequest(BaseModel):
    repo_path: str


class VerificationCheck(BaseModel):
    check_type: str
    status: str  # "passed" | "failed" | "skipped" | "pass" | "fail"
    output: str = ""
    duration_ms: int = 0


class VerificationResult(BaseModel):
    verdict: str = "passed"  # "passed" | "failed"
    checks: List[VerificationCheck] = Field(default_factory=list)
    summary: str = ""


class RepairAttempt(BaseModel):
    attempt_number: int
    failed_checks: List[VerificationCheck] = Field(default_factory=list)
    feedback_prompt: str = ""
    generated_diff: str = ""


# ==============================
# Workflow & Backend DTOs
# ==============================

class AnalyzeRequest(BaseModel):
    analysis_id: Optional[str] = None
    repository_path: Optional[str] = None
    repo_path: Optional[str] = None
    # GitHub context — required for improvement branch + PR creation
    repo_url: Optional[str] = None
    pr_number: Optional[int] = None
    base_branch: Optional[str] = None
    changed_files: List[str] = Field(default_factory=list)
    language: Optional[str] = "python"
    agents: List[str] = Field(default_factory=list)

    @model_validator(mode="before")
    @classmethod
    def sync_paths(cls, values: Any) -> Any:
        if isinstance(values, dict):
            p = values.get("repository_path") or values.get("repo_path") or ""
            values["repository_path"] = p
            values["repo_path"] = p
            if not values.get("analysis_id"):
                values["analysis_id"] = f"run-{uuid.uuid4().hex[:8]}"
        return values


class AgentFindingDto(BaseModel):
    agent: str
    file: str
    start_line: int
    end_line: int
    severity: str
    category: Optional[str] = None
    title: str
    description: Optional[str] = None
    recommendation: Optional[str] = None
    confidence: float = 0.85
    fix_available: bool = True


class AnalyzeResponse(BaseModel):
    analysis_id: str
    findings: List[AgentFindingDto] = Field(default_factory=list)
    quality_score: Optional[float] = None


class WorkflowResponse(BaseModel):
    analysis_id: str
    status: str
    quality_score: float
    findings: List[Finding] = Field(default_factory=list)
    fix_plan: List[PlannedFix] = Field(default_factory=list)
    generated_patches: List[GeneratedPatch] = Field(default_factory=list)
    combined_diff: str = ""
    verification_result: Optional[VerificationResult] = None
    retry_count: int = 0
    repair_history: List[RepairAttempt] = Field(default_factory=list)
    # Improvement branch & PR fields
    improvement_branch: Optional[str] = None
    improvement_pr_url: Optional[str] = None
