import os
from pathlib import Path
from typing import Iterable

from .models import Issue, Problem, Severity, TestCase
from . import getWorkingDirectory


class Initializer:
    def initialize(self, root: Path) -> "Iterable[Issue]":
        pass


class Reader:
    def load(self, problem: Problem) -> "Iterable[Issue]":
        pass

    def samples(self) -> Iterable[TestCase]:
        pass

    def tests(self) -> Iterable[TestCase]:
        pass


class Loader:
    def build(self, root: Path) -> Reader:
        pass


class Checker:
    def check(self, reader: Reader) -> "Iterable[Issue]":
        pass


class Packer:
    def pack(self, reader: Reader, dist: Path) -> "Iterable[Issue]":
        pass


class Pipeline:
    def __init__(self, initializer: "Initializer | None", loader: "Loader | None", checker: "Checker | None", packer: "Packer | None") -> None:
        self.initializer = initializer
        self.loader = loader
        self.checker = checker
        self.packer = packer
        self.root = getWorkingDirectory()

    def initialize(self):
        if self.initializer is None:
            yield Issue("The initializer is disabled.", Severity.Error)
        else:
            yield from self.initializer.initialize(self.root)

    def check(self):
        if self.loader is None:
            yield Issue("The loader is disabled.", Severity.Error)
        elif self.checker is None:
            yield Issue("The checker is disabled.", Severity.Error)
        else:
            yield from self.checker.check(self.loader.build(self.root))

    def pack(self):
        if self.loader is None:
            yield Issue("The loader is disabled.", Severity.Error)
        elif self.packer is None:
            yield Issue("The packer is disabled.", Severity.Error)
        else:
            dist = self.root / "dist"
            if not dist.exists() or dist.is_file():
                os.makedirs(dist)
            yield from self.packer.pack(self.loader.build(self.root), dist)

    def generate(self, start: int, count: int, sample: bool = False, rewrite: bool = False):
        if self.loader is None:
            yield Issue("The loader is disabled.", Severity.Error)
        else:
            from generator_oj_problem.generators.processors import TestGenerator
            yield from TestGenerator(self.root, self.loader.build(self.root)).generate(start, count, sample, rewrite)

    def trim(self):
        if self.loader is None:
            yield Issue("The loader is disabled.", Severity.Error)
        else:
            from generator_oj_problem.generators.trimmers import TestTrimmer
            yield from TestTrimmer(self.root, self.loader.build(self.root)).trim()
