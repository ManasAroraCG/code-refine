import time
from pathlib import Path

from app.models.schemas import VerificationCheck, VerificationResult
from app.sandbox.docker_runner import ensure_image, run_in_sandbox

CHECK_TIMEOUT = 60


def _has_tests(workspace_path):
    root = Path(workspace_path)
    return next(root.rglob("test_*.py"), None) is not None or next(root.rglob("*_test.py"), None) is not None


def _run_check(check_type, workspace_path, command):
    start = time.monotonic()
    result = run_in_sandbox(workspace_path, command, timeout=CHECK_TIMEOUT)
    duration_ms = int((time.monotonic() - start) * 1000)

    status = "passed" if result["exit_code"] == 0 else "failed"
    output = (result["stdout"] + result["stderr"]).strip()

    return VerificationCheck(check_type=check_type, status=status, output=output, duration_ms=duration_ms)


def _skipped(check_type, reason):
    return VerificationCheck(check_type=check_type, status="skipped", output=reason, duration_ms=0)


def run_verification(workspace_path):
    """Run syntax -> lint -> type check -> tests -> build -> security checks inside the Docker sandbox
    and aggregate them into one verdict, matching the `verification_results` table shape per-check."""
    ensure_image()

    checks = [
        _run_check("syntax", workspace_path, "python -m compileall -q ."),
        _run_check("lint", workspace_path, "ruff check ."),
        _run_check("type_check", workspace_path, "mypy --ignore-missing-imports ."),
    ]

    if _has_tests(workspace_path):
        checks.append(_run_check("tests", workspace_path, "pytest -q"))
    else:
        checks.append(_skipped("tests", "no test files found"))

    # no universal build step for a pure-Python workspace in this MVP
    checks.append(_skipped("build", "no build step configured for this workspace"))

    checks.append(_run_check("security", workspace_path, "bandit -r -q ."))

    verdict = "passed" if all(c.status in ("passed", "skipped") for c in checks) else "failed"

    return VerificationResult(verdict=verdict, checks=checks)
