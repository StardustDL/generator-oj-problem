from dataclasses import dataclass, field
from enum import IntEnum


class Severity(IntEnum):
    Info = 0
    Warning = 10
    Error = 20


@dataclass
class Issue:
    message: str
    level: Severity = Severity.Info


@dataclass
class TestCase:
    name: str = ""
    input: str = ""
    output: str = ""


@dataclass
class Problem:
    name: str = ""
    author: str = ""
    time: float = 1.0
    memory: float = 128.0
    solution: str = ""
    solutionLanguage: str = "C++"

    description: str = ""
    input: str = ""
    output: str = ""
    hint: str = ""
