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
