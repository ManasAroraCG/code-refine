import os
import re
import subprocess
import logging

import httpx

logger = logging.getLogger(__name__)

_GIT_USER_NAME = "CodeRefine Bot"
_GIT_USER_EMAIL = "coderefine-bot@noreply.github.com"


def _run_git(args: list[str], cwd: str, check: bool = True) -> subprocess.CompletedProcess:
    """Run a git command in the given directory."""
    return subprocess.run(
        ["git"] + args,
        cwd=cwd,
        check=check,
        capture_output=True,
        text=True,
    )


def _branch_exists(repo_path: str, branch_name: str) -> bool:
    result = _run_git(["rev-parse", "--verify", "--quiet", branch_name], cwd=repo_path, check=False)
    return result.returncode == 0


def commit_and_push(repo_path: str, branch_name: str, commit_message: str) -> None:
    """Create a new branch, stage all changes, commit, and push to origin.

    The caller is responsible for ensuring the patch has already been
    written to disk inside ``repo_path`` before calling this function.
    """
    # Configure identity so git commit works in headless / CI environments
    _run_git(["config", "user.name", _GIT_USER_NAME], cwd=repo_path)
    _run_git(["config", "user.email", _GIT_USER_EMAIL], cwd=repo_path)

    if _branch_exists(repo_path, branch_name):
        _run_git(["checkout", branch_name], cwd=repo_path)
    else:
        _run_git(["checkout", "-b", branch_name], cwd=repo_path)

    # Stage everything that was modified by the patch applier
    _run_git(["add", "-A"], cwd=repo_path)

    # Commit — allow empty just in case (shouldn't happen in practice)
    _run_git(["commit", "--allow-empty", "-m", commit_message], cwd=repo_path)

    # Push to origin
    token = os.getenv("GITHUB_TOKEN", "")
    if token:
        # Inject the token into the remote URL so git can authenticate over HTTPS
        remote_result = _run_git(["remote", "get-url", "origin"], cwd=repo_path)
        original_url = remote_result.stdout.strip()
        # Replace https://github.com/... with https://<token>@github.com/...
        authed_url = re.sub(r"https://", f"https://{token}@", original_url)
        _run_git(["remote", "set-url", "origin", authed_url], cwd=repo_path)

    _run_git(["push", "--force-with-lease", "origin", branch_name], cwd=repo_path)
    logger.info("Pushed improvement branch '%s'", branch_name)


def _parse_owner_repo(repo_url: str) -> tuple[str, str]:
    """Extract (owner, repo) from a GitHub URL.

    Supports both HTTPS (https://github.com/owner/repo[.git]) and
    SSH (git@github.com:owner/repo.git) formats.
    """
    repo_url = repo_url.strip()

    # HTTPS
    m = re.match(r"https?://github\.com/([^/]+)/([^/]+?)(?:\.git)?/?$", repo_url)
    if m:
        return m.group(1), m.group(2)

    # SSH
    m = re.match(r"git@github\.com:([^/]+)/([^/]+?)(?:\.git)?$", repo_url)
    if m:
        return m.group(1), m.group(2)

    raise ValueError(f"Cannot parse owner/repo from URL: {repo_url!r}")


def create_github_pr(
    repo_url: str,
    head_branch: str,
    base_branch: str,
    pr_title: str,
    pr_body: str,
) -> str:
    """Open a GitHub PR using the REST API and return the PR HTML URL.

    Requires ``GITHUB_TOKEN`` to be set in the environment.
    """
    token = os.getenv("GITHUB_TOKEN", "")
    if not token:
        raise EnvironmentError(
            "GITHUB_TOKEN is not set. Please add it to your .env file."
        )

    owner, repo = _parse_owner_repo(repo_url)

    payload = {
        "title": pr_title,
        "body": pr_body,
        "head": head_branch,
        "base": base_branch,
    }

    headers = {
        "Authorization": f"Bearer {token}",
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
    }

    response = httpx.post(
        f"https://api.github.com/repos/{owner}/{repo}/pulls",
        json=payload,
        headers=headers,
        timeout=30,
    )

    if response.status_code == 422:
        # GitHub returns 422 when a PR already exists for this head branch
        data = response.json()
        errors = data.get("errors", [])
        for err in errors:
            if "already exists" in str(err.get("message", "")):
                logger.warning("PR already exists for branch '%s', fetching its URL", head_branch)
                # Return the URL of the existing PR instead of crashing
                existing = httpx.get(
                    f"https://api.github.com/repos/{owner}/{repo}/pulls",
                    params={"head": f"{owner}:{head_branch}", "state": "open"},
                    headers=headers,
                    timeout=30,
                )
                existing.raise_for_status()
                pulls = existing.json()
                if pulls:
                    return pulls[0]["html_url"]

        raise httpx.HTTPStatusError(
            f"GitHub PR creation failed with 422: {response.text}",
            request=response.request,
            response=response,
        )

    response.raise_for_status()
    return response.json()["html_url"]
