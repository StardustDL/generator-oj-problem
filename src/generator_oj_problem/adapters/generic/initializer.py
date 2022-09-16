import os
from pathlib import Path
from typing import Iterable
from generator_oj_problem.models import Issue, Severity
from generator_oj_problem.pipelines import Initializer
from .paths import PathBuilder
from yaml import safe_dump


class Generic(Initializer):
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

        std = """#include <iostream>
using namespace std;
int main()
{
    int a, b;
    cin >> a >> b;
    cout << a + b << endl;
    return 0;
}
"""

        if paths.solution.exists():
            yield Issue("Solution program exists.", Severity.Warning)
        else:
            paths.solution.write_text(std)

        if paths.hint.exists():
            yield Issue("Hint exists.", Severity.Warning)
        else:
            paths.hint.write_text(f"""Standard program:

```cpp
{std}
```""")

        if paths.metadata.exists():
            yield Issue("Metadata exists.", Severity.Warning)
        else:
            paths.metadata.write_text(safe_dump({
                "name": "A + B Problem",
                "author": "",
                "time": 1.0,
                "memory": 128.0,
                "solutionLanguage": "C++"
            }))

        if not paths.samples.exists() or paths.samples.is_file():
            os.makedirs(paths.samples)

        if not paths.tests.exists() or paths.tests.is_file():
            os.makedirs(paths.tests)

        def genIO(testdir: Path, name: str, input: str, output: str, type: str):
            infile = testdir / f"{name}.in"
            outfile = testdir / f"{name}.out"

            if infile.exists() or outfile.exists():
                yield Issue(f"{type.title()} case {name} exists in {testdir}", Severity.Warning)
            else:
                infile.write_text(input)
                outfile.write_text(output)

        yield from genIO(paths.samples, "0", "1 2\n", "3\n", "sample")
        yield from genIO(paths.tests, "0", "2 3\n", "5\n", "test")
        yield from genIO(paths.tests, "1", "20 22\n", "42\n", "test")
