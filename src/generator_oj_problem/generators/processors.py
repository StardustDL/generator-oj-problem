import os
import subprocess
from pathlib import Path
import sys
from typing import Iterable

from generator_oj_problem.models import Issue, Severity
from . import ENV_CASE_ID, ENV_REWRITE, ENV_TARGET, SUBMITED, Case


class TestGenerator:
    def __init__(self, root: Path) -> None:
        self.root = root
        self.file = root / "generator.py"

    def initialize(self) -> "Iterable[Issue]":
        if self.file.exists():
            yield Issue("Generator exists.", Severity.Warning)
        else:
            self.file.write_text(
                (Path(__file__).parent / "template.py").read_text())

    def generate(self, start: int, count: int, sample: bool = False, rewrite: bool = False) -> "Iterable[Issue]":
        if not self.file.exists() or self.file.is_dir():
            yield Issue("Generator is not found.", Severity.Error)
            return

        prefix = "sample" if sample else "test"
        target = self.root / f"{prefix}s"
        if not target.exists() or target.is_file():
            os.makedirs(target)
        for i in range(start, start + count):
            case = Case(i, target)
            print(f"Generate {prefix} case {i}...")
            try:
                result = subprocess.run(["-u", str(self.file)],
                                        executable=sys.executable,
                                        cwd=self.root,
                                        text=True,
                                        capture_output=True,
                                        env={**os.environ,
                                             ENV_CASE_ID: str(case.id),
                                             ENV_TARGET: str(case.target.resolve()),
                                             ENV_REWRITE: "1" if rewrite else "0",
                                             "PYTHONUTF8": "1"})
                stdout = result.stdout
                if stdout is None or not stdout.endswith(SUBMITED):
                    yield Issue(f"Generated data is not submitted for {prefix} case {case.id}, please call 'data.submit()' at the end of generator.", Severity.Warning)
                else:
                    stdout = stdout.replace(SUBMITED, "")
                if stdout:
                    yield Issue(f"Generator standard output for {prefix} case {case.id}:\n{stdout.strip()}", Severity.Info)
                if result.stderr:
                    yield Issue(f"Generator standard error for {prefix} case {case.id}:\n{result.stderr.strip()}", Severity.Warning)
                if result.returncode != 0:
                    raise Exception(
                        f"Generator exited with non-zero: {result.returncode}.")
                yield Issue(f"Generated {prefix} case {case.id}.")
            except Exception as ex:
                yield Issue(f"Failed to generate {prefix} case {case.id}: {ex}", Severity.Error)
