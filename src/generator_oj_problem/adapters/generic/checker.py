from glob import glob
import os
from pathlib import Path
from typing import Iterable
from generator_oj_problem.models import Issue, Problem, Severity, TestCase
from generator_oj_problem.pipelines import Reader, Checker
from .paths import PathBuilder
from yaml import safe_load


class Generic(Checker):
    def check(self, reader: Reader) -> "Iterable[Issue]":
        problem = Problem()
        print("Load problem...")
        yield from reader.load(problem)
        yield from self._metadata(problem)
        yield from self._description(problem)
        print("Check sample cases...")
        for case in reader.samples():
            yield from self._testcase(case, "sample")
        print("Check test cases...")
        for case in reader.tests():
            yield from self._testcase(case, "test")

    def _metadata(self, problem: Problem) -> "Iterable[Issue]":
        print("Check metadata...")
        if problem.name.isspace():
            yield Issue("The name of the problem is missing.", Severity.Error)
        if problem.author.isspace():
            yield Issue("The author of the problem is missing.", Severity.Warning)
        if problem.time <= 0:
            yield Issue("The time limit must be positive.", Severity.Error)
        if problem.memory <= 0:
            yield Issue("The memory limit must be positive.", Severity.Error)

    def _description(self, problem: Problem) -> "Iterable[Issue]":
        print("Check description...")
        if problem.description.isspace():
            yield Issue("The description is missing.", Severity.Error)
        if problem.input.isspace():
            yield Issue("The input description is missing.", Severity.Warning)
        if problem.output.isspace():
            yield Issue("The output description is missing.", Severity.Warning)
        if problem.hint.isspace():
            yield Issue("The hint is missing.", Severity.Warning)
        if problem.solution.isspace():
            yield Issue("The solution is missing.", Severity.Warning)

    def _testcase(self, case: TestCase, type: str) -> "Iterable[Issue]":
        print(f"  Check {type} case {case.name}...")
        if case.input.isspace():
            yield Issue(f"The input of {type} {case.name} is missing.", Severity.Error)
        if case.output.isspace():
            yield Issue(f"The output of {type} {case.name} is missing.", Severity.Error)
