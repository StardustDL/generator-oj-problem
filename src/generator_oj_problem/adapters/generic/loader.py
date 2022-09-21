from glob import glob
import os
from pathlib import Path
from typing import Iterable
from generator_oj_problem.models import Issue, Problem, Severity, TestCase
from generator_oj_problem.pipelines import Loader, Reader
from .paths import PathBuilder
from yaml import safe_load


class GenericReader(Reader):
    def __init__(self, root: Path) -> None:
        super().__init__()
        self.root = root
        self.paths = PathBuilder(root)

    def load(self, problem: Problem) -> "Iterable[Issue]":
        paths = self.paths

        if not paths.description.exists():
            yield Issue("Description does NOT exist.", Severity.Warning)
        else:
            problem.description = paths.description.read_text()

        if not paths.input.exists():
            yield Issue("Input description does NOT exist.", Severity.Warning)
        else:
            problem.input = paths.input.read_text()

        if not paths.output.exists():
            yield Issue("Output description does NOT exist.", Severity.Warning)
        else:
            problem.output = paths.output.read_text()

        if not paths.hint.exists():
            yield Issue("Hint does NOT exist.", Severity.Warning)
        else:
            problem.hint = paths.hint.read_text()

        if not paths.solution.exists():
            yield Issue("Solution does NOT exist.", Severity.Warning)
        else:
            problem.solution = paths.solution.read_text()

        if not paths.metadata.exists():
            yield Issue("Metadata does NOT exist.", Severity.Warning)
        else:
            try:
                metadata: dict = safe_load(paths.metadata.read_text())
                problem.name = metadata.get("name", "")
                problem.author = metadata.get("author", "")
                problem.time = float(metadata.get("time", 1.0))
                problem.memory = float(metadata.get("memory", 128.0))
                problem.solutionLanguage = metadata.get(
                    "solutionLanguage", "C++")
                problem.crlf = metadata.get("crlf", False)
            except:
                yield Issue("Metadata is in wrong format.", Severity.Error)

        return problem

    def _getCases(self, root: Path):
        if not root.exists() or root.is_file():
            return
        for infile in root.glob("*.in"):
            case = TestCase(infile.stem, infile.read_bytes())
            outfile = infile.with_suffix(".out")
            if outfile.exists():
                case.routput = outfile.read_bytes()
            yield case

    def samples(self) -> Iterable[TestCase]:
        return self._getCases(self.paths.samples)

    def tests(self) -> Iterable[TestCase]:
        return self._getCases(self.paths.tests)


class Generic(Loader):
    def build(self, root: Path) -> Reader:
        return GenericReader(root)

    def initialize(self, root: Path) -> "Iterable[Issue]":
        paths = PathBuilder(root)

        if paths.description.exists():
            yield Issue("Description exists.", Severity.Warning)
        else:
            paths.description.write_text("""Calculate `a+b`.""")

        if paths.input.exists():
            yield Issue("Input description exists.", Severity.Warning)
        else:
            paths.input.write_text("""Two integer `a`, `b` (0<=a,b<=10).""")

        if paths.output.exists():
            yield Issue("Output description exists.", Severity.Warning)
        else:
            paths.output.write_text("""Output `a+b`.""")

        if paths.hint.exists():
            yield Issue("Hint exists.", Severity.Warning)
        else:
            paths.hint.write_text("""Standard program:

```cpp
#include <iostream>
using namespace std;
int main()
{
    int a, b;
    cin >> a >> b;
    cout << a + b << endl;
    return 0;
}
```""")

        if paths.metadata.exists():
            yield Issue("Metadata exists.", Severity.Warning)
        else:
            paths.metadata.write_text(safe_dump({
                "name": "A + B Problem",
                "author": "",
                "timelimit": 1.0,
                "memorylimit": 128.0
            }))

        if not paths.samples.exists() or paths.samples.is_file():
            os.makedirs(paths.samples)

        if not paths.tests.exists() or paths.tests.is_file():
            os.makedirs(paths.tests)

        def genIO(testdir: Path, name: str, input: str, output: str):
            infile = testdir / f"{name}.in"
            outfile = testdir / f"{name}.out"

            if infile.exists() or outfile.exists():
                yield Issue(f"Test case {name} exists in {testdir}")
            else:
                infile.write_text(input)
                outfile.write_text(output)

        yield from genIO(paths.samples, "0", "1 2\n", "3\n")
        yield from genIO(paths.tests, "0", "2 3\n", "5\n")
        yield from genIO(paths.tests, "1", "20 22\n", "42\n")
