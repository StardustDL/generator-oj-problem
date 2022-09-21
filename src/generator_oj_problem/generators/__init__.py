from dataclasses import dataclass
import io
import os
from pathlib import Path
import sys


ENV_CASE_ID = "GOP_GENERATOR_CASE"
ENV_TARGET = "GOP_GENERATOR_TARGET"
ENV_REWRITE = "GOP_GENERATOR_REWRITE"
SUBMITED = "cfdf14756a566e9e3de87c1980d2fc715032276e"


@dataclass
class Case:
    id: int = 0
    target: Path = Path(".")
    input: "str | None" = None
    output: "str | None" = None
    rewrite: bool = False

    def In(self, *args, **kwargs):
        if self.input is None:
            self.input = ""
        with io.StringIO(self.input) as to:
            to.seek(len(self.input))
            print(*args, file=to, **kwargs)
            self.input = to.getvalue()

    def Out(self, *args, **kwargs):
        if self.output is None:
            self.output = ""
        with io.StringIO(self.output) as to:
            to.seek(len(self.output))
            print(*args, file=to, **kwargs)
            self.output = to.getvalue()

    @property
    def infile(self):
        return (self.target / f"{self.id}.in")

    @property
    def outfile(self):
        return (self.target / f"{self.id}.out")

    def submit(self):
        if self.input is not None:
            if self.infile.exists():
                if self.rewrite:
                    print(
                        f"Rewrite existed input file {self.infile}.", file=sys.stderr)
                else:
                    print(f"Input file {self.infile} exists.", file=sys.stderr)
                    exit(1)
            self.infile.write_text(self.input)
        if self.output is not None:
            if self.outfile.exists():
                if self.rewrite:
                    print(
                        f"Rewrite existed output file {self.outfile}.", file=sys.stderr)
                else:
                    print(
                        f"Output file {self.outfile} exists.", file=sys.stderr)
                    exit(1)
            self.outfile.write_text(self.output)
        print(SUBMITED, end="")


data: Case = Case()

try:
    data.id = int(os.getenv(ENV_CASE_ID) or 0)
    data.target = Path(os.getenv(ENV_TARGET) or ".")
    data.rewrite = os.getenv(ENV_REWRITE) == "1"
except:
    pass
