from pathlib import Path
from typing import Iterable

from ..models import Issue, Problem, Severity, TestCase
from ..pipelines import Reader
from . import Case


class TestTrimmer:
    def __init__(self, root: Path, reader: Reader) -> None:
        self.root = root
        self.reader = reader

    def trim(self) -> "Iterable[Issue]":
        problem = Problem()
        yield from self.reader.load(problem)

        def item(case: TestCase, type: str):
            print(f"  Trim {type} case {case.name}...")
            target = Case(case.name, self.root /
                          f"{type}s", rewrite=True, crlf=problem.crlf)
            for l in case.input.rstrip().rstrip("\r\n").splitlines():
                target.In(l.rstrip("\r\n"))
            for l in case.output.rstrip().rstrip("\r\n").splitlines():
                target.Out(l.rstrip("\r\n"))
            target.submit(silence=True)
            yield Issue(f"Trimmed {type} case {case.name}.", Severity.Info)

        for case in self.reader.samples():
            yield from item(case, "sample")

        for case in self.reader.tests():
            yield from item(case, "test")
