from typing import List, Optional

from pydantic import BaseModel


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


class ChangedFileContext(BaseModel):
    path: str
    content: str
    symbols: List[dict] = []


class CodeContext(BaseModel):
    changed_files: List[ChangedFileContext]


class Finding(BaseModel):
    agent_type: str
    file_path: str
    start_line: int
    end_line: int
    category: str
    severity: str
    title: str
    description: str


class PatchRequest(BaseModel):
    repo_path: str
    # unified diff mode
    diff: Optional[str] = None
    # line-range replacement mode (all four required together when diff is not given)
    file_path: Optional[str] = None
    start_line: Optional[int] = None
    end_line: Optional[int] = None
    new_code: Optional[str] = None


class PatchResult(BaseModel):
    success: bool
    diff: str
    error: Optional[str] = None


class VerifyRequest(BaseModel):
    repo_path: str


class VerificationCheck(BaseModel):
    check_type: str
    status: str  # "passed" | "failed" | "skipped"
    output: str
    duration_ms: int


class VerificationResult(BaseModel):
    verdict: str  # "passed" | "failed"
    checks: List[VerificationCheck]
