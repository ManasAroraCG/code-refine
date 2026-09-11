import os
import re

from app.analysis.tools.python_tools import run_bandit, run_mypy, run_ruff
from app.models.schemas import Finding

MYPY_LINE_RE = re.compile(
    r"^(?P<file>.+):(?P<line>\d+): (?P<severity>\w+): (?P<message>.+?)(?:\s\[(?P<code>[\w\-]+)\])?$"
)


def normalize_path(repo_path, raw_path):
    """Make every tool's file path relative to repo_path, using forward slashes."""
    repo_abs = os.path.abspath(repo_path)
    raw_abs = raw_path if os.path.isabs(raw_path) else os.path.join(repo_path, raw_path)
    raw_abs = os.path.abspath(raw_abs)

    try:
        rel = os.path.relpath(raw_abs, repo_abs)
    except ValueError:
        rel = raw_path

    return rel.replace("\\", "/")


def parse_ruff_findings(repo_path, raw_findings):
    findings = []
    for item in raw_findings:
        findings.append(Finding(
            agent_type="static:ruff",
            file_path=normalize_path(repo_path, item.get("filename", "")),
            start_line=item.get("location", {}).get("row", 0),
            end_line=item.get("end_location", {}).get("row", 0),
            category=item.get("code") or "unknown",
            severity="warning",
            title=item.get("message", ""),
            description=item.get("message", ""),
        ))
    return findings


def parse_mypy_findings(repo_path, lines):
    findings = []
    for line in lines:
        match = MYPY_LINE_RE.match(line)
        if not match:
            continue

        line_no = int(match.group("line"))
        message = match.group("message").strip()
        findings.append(Finding(
            agent_type="static:mypy",
            file_path=normalize_path(repo_path, match.group("file")),
            start_line=line_no,
            end_line=line_no,
            category=match.group("code") or "mypy",
            severity=match.group("severity"),
            title=message,
            description=message,
        ))
    return findings


def parse_bandit_findings(repo_path, results):
    findings = []
    for item in results:
        line_no = item.get("line_number", 0)
        line_range = item.get("line_range") or [line_no]
        findings.append(Finding(
            agent_type="static:bandit",
            file_path=normalize_path(repo_path, item.get("filename", "")),
            start_line=line_no,
            end_line=line_range[-1],
            category=item.get("test_id", "unknown"),
            severity=item.get("issue_severity", "LOW").lower(),
            title=item.get("issue_text", ""),
            description=item.get("issue_text", ""),
        ))
    return findings


def run_static_analysis(repo_path, changed_files):
    """POC: normalize findings from ruff, mypy and bandit into the common Finding shape."""
    python_files = [f for f in changed_files if f.endswith(".py")]

    findings = []
    findings += parse_ruff_findings(repo_path, run_ruff(repo_path, python_files))
    findings += parse_mypy_findings(repo_path, run_mypy(repo_path, python_files))
    findings += parse_bandit_findings(repo_path, run_bandit(repo_path, python_files))

    return findings

