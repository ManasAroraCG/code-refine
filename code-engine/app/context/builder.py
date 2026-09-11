from pathlib import Path

from app.context.ast_utils import extract_symbols
from app.models.schemas import ChangedFileContext, CodeContext


def build_context(repo_path, changed_files):
    """POC: return each changed file's full content plus its top-level symbols."""
    results = []

    for rel_path in changed_files:
        file_path = Path(repo_path) / rel_path
        if not file_path.is_file():
            continue

        content = file_path.read_text(encoding="utf-8", errors="ignore")

        symbols = []
        if file_path.suffix == ".py":
            symbols = extract_symbols(content)

        results.append(ChangedFileContext(path=rel_path, content=content, symbols=symbols))

    return CodeContext(changed_files=results)
