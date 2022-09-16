from pathlib import Path


class PathBuilder:
    def __init__(self, root: Path) -> None:
        self.root = root
        self.description = root / "description.md"
        self.input = root / "input.md"
        self.output = root / "output.md"
        self.hint = root / "hint.md"
        self.metadata = root / "problem.yml"
        self.samples = root / "samples"
        self.tests = root / "tests"
        self.solution = root / "solution.txt"
