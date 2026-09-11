import json
import subprocess


def run_ruff(repo_path, files):
    """Run ruff's linter on the given files and return its raw JSON findings."""
    if not files:
        return []

    result = subprocess.run(
        ["ruff", "check", "--output-format=json", *files],
        cwd=repo_path,
        capture_output=True,
        text=True,
    )

    if not result.stdout.strip():
        return []

    return json.loads(result.stdout)


def run_mypy(repo_path, files):
    """Run mypy's type checker on the given files and return its raw output lines."""
    if not files:
        return []

    result = subprocess.run(
        ["mypy", "--show-error-codes", "--no-error-summary", *files],
        cwd=repo_path,
        capture_output=True,
        text=True,
    )

    return [line for line in result.stdout.splitlines() if line]


def run_bandit(repo_path, files):
    """Run bandit's security scanner on the given files and return its raw JSON results."""
    if not files:
        return []

    result = subprocess.run(
        ["bandit", "-f", "json", *files],
        cwd=repo_path,
        capture_output=True,
        text=True,
    )

    # bandit writes log lines to stdout before the JSON payload
    json_start = result.stdout.find("{")
    if json_start == -1:
        return []

    return json.loads(result.stdout[json_start:]).get("results", [])

