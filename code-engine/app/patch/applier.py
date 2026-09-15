import difflib
import subprocess
from pathlib import Path


def apply_line_range_patch(repo_path, file_path, start_line, end_line, new_code):
    """Replace lines [start_line, end_line] (1-indexed, inclusive) in a workspace file with new_code."""
    target = Path(repo_path) / file_path

    if not target.is_file():
        return {"success": False, "diff": "", "error": f"file not found: {file_path}"}

    original_lines = target.read_text(encoding="utf-8").splitlines(keepends=True)

    if start_line < 1 or end_line < start_line or end_line > len(original_lines):
        return {
            "success": False,
            "diff": "",
            "error": f"invalid line range {start_line}-{end_line} for file with {len(original_lines)} lines",
        }

    new_lines = new_code.splitlines(keepends=True)
    if new_lines and not new_lines[-1].endswith("\n") and end_line < len(original_lines):
        new_lines[-1] += "\n"

    patched_lines = original_lines[:start_line - 1] + new_lines + original_lines[end_line:]

    diff = "".join(difflib.unified_diff(
        original_lines,
        patched_lines,
        fromfile=f"a/{file_path}",
        tofile=f"b/{file_path}",
    ))

    # no-op patch (already applied) - don't touch the file, just report it cleanly
    if not diff:
        return {"success": True, "diff": "", "error": None}

    target.write_text("".join(patched_lines), encoding="utf-8")

    return {"success": True, "diff": diff, "error": None}


def apply_unified_diff(repo_path, diff_text):
    """Apply a unified diff via `git apply`, rejecting (not partially writing) anything that doesn't apply cleanly."""
    check = subprocess.run(
        ["git", "apply", "--check", "-"],
        cwd=repo_path,
        input=diff_text,
        capture_output=True,
        text=True,
    )
    if check.returncode != 0:
        return {"success": False, "diff": "", "error": f"patch does not apply cleanly: {check.stderr.strip()}"}

    result = subprocess.run(
        ["git", "apply", "-"],
        cwd=repo_path,
        input=diff_text,
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        return {"success": False, "diff": "", "error": f"git apply failed: {result.stderr.strip()}"}

    return {"success": True, "diff": diff_text, "error": None}


def apply_patch(request):
    """Dispatch a PatchRequest to unified-diff or line-range application, whichever it specifies."""
    if request.diff:
        return apply_unified_diff(request.repo_path, request.diff)

    if None in (request.file_path, request.start_line, request.end_line, request.new_code):
        return {
            "success": False,
            "diff": "",
            "error": "provide either 'diff', or all of 'file_path'/'start_line'/'end_line'/'new_code'",
        }

    return apply_line_range_patch(
        request.repo_path, request.file_path, request.start_line, request.end_line, request.new_code
    )

