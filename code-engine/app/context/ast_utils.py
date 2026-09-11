import ast
from pathlib import Path


def extract_symbols(source):
    """Return top-level function/class defs found in a Python source string."""
    tree = ast.parse(source)
    symbols = []

    for node in ast.walk(tree):
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)):
            symbols.append({
                "name": node.name,
                "type": type(node).__name__,
                "start_line": node.lineno,
                "end_line": getattr(node, "end_lineno", node.lineno),
            })

    return symbols


def extract_symbols_from_file(file_path):
    source = Path(file_path).read_text(encoding="utf-8")
    return extract_symbols(source)
