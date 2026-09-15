import subprocess
import uuid
from pathlib import Path

SANDBOX_IMAGE = "code-refine-sandbox:latest"
SANDBOX_DOCKERFILE_DIR = Path(__file__).resolve().parents[3] / "infrastructure" / "sandbox"

MEMORY_LIMIT = "512m"
CPU_LIMIT = "1"
PIDS_LIMIT = "256"


def ensure_image():
    """Build the sandbox execution image if it isn't already present locally."""
    check = subprocess.run(
        ["docker", "image", "inspect", SANDBOX_IMAGE],
        capture_output=True,
        text=True,
    )
    if check.returncode == 0:
        return

    subprocess.run(
        ["docker", "build", "-t", SANDBOX_IMAGE, str(SANDBOX_DOCKERFILE_DIR)],
        check=True,
        capture_output=True,
        text=True,
    )


def run_in_sandbox(workspace_path, command, timeout=60):
    """Run `command` inside an ephemeral, network-isolated container.

    The host workspace is mounted read-only and copied into the container's own
    filesystem before running, so the host copy is never mutated and no bind-mount
    write access is ever granted to untrusted code. The container is always removed,
    including when the run times out.
    """
    container_name = f"coderefine-sandbox-{uuid.uuid4().hex[:12]}"

    docker_cmd = [
        "docker", "run",
        "--rm",
        "--name", container_name,
        "--network", "none",
        "--memory", MEMORY_LIMIT,
        "--cpus", CPU_LIMIT,
        "--pids-limit", PIDS_LIMIT,
        "-v", f"{workspace_path}:/mnt/src:ro",
        SANDBOX_IMAGE,
        "sh", "-c",
        # bind-mounting from an NTFS host marks every file executable (no real perm bits to preserve);
        # normalize to 644 so ruff's EXE002 doesn't fire on that mount artifact
        f"cp -r /mnt/src /work && find /work -type f -exec chmod 644 {{}} + && cd /work && {command}",
    ]

    try:
        result = subprocess.run(docker_cmd, capture_output=True, text=True, timeout=timeout)
        return {
            "exit_code": result.returncode,
            "stdout": result.stdout,
            "stderr": result.stderr,
            "timed_out": False,
        }
    except subprocess.TimeoutExpired:
        # --rm only cleans up on normal exit; force-remove since we killed the client mid-run.
        subprocess.run(["docker", "rm", "-f", container_name], capture_output=True, text=True)
        return {
            "exit_code": -1,
            "stdout": "",
            "stderr": f"command timed out after {timeout}s",
            "timed_out": True,
        }
