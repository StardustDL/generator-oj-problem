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
    rinput: bytes = b""
    routput: bytes = b""

    @property
    def input(self):
        return self.rinput.decode("utf-8")

    @property
    def output(self):
        return self.routput.decode("utf-8")


@dataclass
class Problem:
    name: str = ""
    author: str = ""
    time: float = 1.0
    memory: float = 128.0
    solution: str = ""
    solutionLanguage: str = "C++"
    crlf: bool = False

    description: str = ""
    input: str = ""
    output: str = ""
    hint: str = ""
