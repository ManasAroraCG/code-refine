import json
import shutil
import subprocess
import sys


def _get_tool_cmd(tool_name: str, args: list) -> list:
    """Resolve command using which or python -m module."""
    if shutil.which(tool_name):
        return [tool_name, *args]
    return [sys.executable, "-m", tool_name, *args]


def run_ruff(repo_path, files):
    """Run ruff's linter on the given files and return its raw JSON findings."""
    if not files:
        return []

    cmd = _get_tool_cmd("ruff", ["check", "--output-format=json", *files])
    try:
        result = subprocess.run(
            cmd,
            cwd=repo_path,
            capture_output=True,
            text=True,
        )
        if not result.stdout.strip():
            return []
        return json.loads(result.stdout)
    except Exception:
        return []


def run_mypy(repo_path, files):
    """Run mypy's type checker on the given files and return its raw output lines."""
    if not files:
        return []

    cmd = _get_tool_cmd("mypy", ["--show-error-codes", "--no-error-summary", *files])
    try:
        result = subprocess.run(
            cmd,
            cwd=repo_path,
            capture_output=True,
            text=True,
        )
        return [line for line in result.stdout.splitlines() if line]
    except Exception:
        return []


def run_bandit(repo_path, files):
    """Run bandit's security scanner on the given files and return its raw JSON results."""
    if not files:
        return []

    cmd = _get_tool_cmd("bandit", ["-f", "json", *files])
    try:
        result = subprocess.run(
            cmd,
            cwd=repo_path,
            capture_output=True,
            text=True,
        )
        json_start = result.stdout.find("{")
        if json_start == -1:
            return []
        return json.loads(result.stdout[json_start:]).get("results", [])
    except Exception:
        return []
