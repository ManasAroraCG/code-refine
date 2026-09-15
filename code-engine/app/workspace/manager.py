import tempfile
import subprocess
from pathlib import Path


class WorkspaceManager:

    def create_workspace(self):
        path = tempfile.mkdtemp(prefix="coderefine-")
        return Path(path)

    def clone_repository(self, repo_url, branch):
        workspace = self.create_workspace()

        subprocess.run([
            "git",
            "clone",
            "--branch",
            branch,
            "--single-branch",
            repo_url,
            str(workspace / "repo")
        ], check=True)

        return workspace / "repo"

    def checkout_pr(self, repo_url, pr_number):
        workspace = self.create_workspace()
        repo_path = workspace / "repo"

        subprocess.run(["git", "clone", repo_url, str(repo_path)], check=True)

        local_ref = f"pr-{pr_number}"
        subprocess.run([
            "git", "fetch", "origin",
            f"pull/{pr_number}/head:{local_ref}"
        ], cwd=repo_path, check=True)

        subprocess.run(["git", "checkout", local_ref], cwd=repo_path, check=True)

        return repo_path

    def get_changed_files(self, repo_path, base, head="HEAD"):
        self._ensure_ref(repo_path, base)

        result = subprocess.run(
            ["git", "diff", "--name-only", f"{base}...{head}"],
            cwd=repo_path,
            check=True,
            capture_output=True,
            text=True,
        )

        return [line for line in result.stdout.splitlines() if line]

    def _ensure_ref(self, repo_path, ref):
        """Fetch `ref` from origin if it isn't resolvable locally (e.g. base branch on a --single-branch clone)."""
        check = subprocess.run(
            ["git", "rev-parse", "--verify", "--quiet", ref],
            cwd=repo_path,
            capture_output=True,
            text=True,
        )
        if check.returncode == 0:
            return

        subprocess.run(
            ["git", "fetch", "origin", f"{ref}:{ref}"],
            cwd=repo_path,
            check=True,
            capture_output=True,
            text=True,
        )