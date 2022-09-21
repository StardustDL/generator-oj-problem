import code
import logging
import os
import pathlib
import sys
from typing import Iterable

import click
import yaml
from click import BadArgumentUsage, BadOptionUsage, BadParameter
from click.exceptions import ClickException

from . import __version__
from .pipelines import Pipeline
from .adapters import build as buildAdapter, all as allAdapters
from .models import Issue, Severity


class AliasedGroup(click.Group):
    def get_command(self, ctx, cmd_name):
        rv = click.Group.get_command(self, ctx, cmd_name)
        if rv is not None:
            return rv
        matches = [x for x in self.list_commands(ctx)
                   if x.startswith(cmd_name)]
        if not matches:
            return None
        elif len(matches) == 1:
            return click.Group.get_command(self, ctx, matches[0])
        ctx.fail(f"Too many matches: {', '.join(sorted(matches))}")

    def resolve_command(self, ctx, args):
        # always return the full command name
        _, cmd, args = super().resolve_command(ctx, args)
        return cmd.name, cmd, args


icons = {
    Severity.Info: "🔵",
    Severity.Error: "🔴",
    Severity.Warning: "🟡"
}
results = {
    Severity.Info: "✅ Done successfully.",
    Severity.Error: "❌ Failed to process.",
    Severity.Warning: "❕ Done, but with warnings."
}


def printIssues(issues: "Iterable[Issue]", final: bool = True) -> Severity:
    maxLevel = Severity.Info
    for item in issues:
        print(f"{icons[item.level]} {item.message}")
        maxLevel = max(maxLevel, item.level)
    if final:
        print("-" * 50)
        print(results[maxLevel])
    return maxLevel


adapters = list(allAdapters())
pipeline: Pipeline = buildAdapter("generic")


@click.command(cls=AliasedGroup)
@click.pass_context
@click.version_option(__version__, package_name="aexpy", prog_name="aexpy", message="%(prog)s v%(version)s.")
@click.option("-a", "--adapter", type=click.Choice(adapters, case_sensitive=False), default="generic", help="Adapter to use.")
@click.option('-D', '--directory', type=click.Path(exists=True, file_okay=False, resolve_path=True, path_type=pathlib.Path), default=".", help="Path to working directory.")
def main(ctx=None, adapter: str = "generic", directory: pathlib.Path = ".") -> None:
    """
    Generator-OJ-Problem

    A command-line tool to generate Online-Judge problem.

    Repository: https://github.com/StardustDL/generator-oj-problem
    """

    os.chdir(directory)

    global pipeline
    pipeline = buildAdapter(adapter)


@main.command()
@click.option("-s", "--start", default=0, help="Start case id.")
@click.option("-c", "--count", default=10, help="The number of generated cases.")
@click.option("--sample", is_flag=True, help="Generate sample cases.")
@click.option("-r", "--rewrite", is_flag=True, help="Rewrite existed cases.")
def generate(start: int = 0, count: int = 10, sample: bool = False, rewrite: bool = False):
    """Generate input or output data."""

    if printIssues(pipeline.generate(start, count, sample, rewrite)) == Severity.Error:
        raise ClickException("Failed to generate.")


@main.command()
def trim():
    """Trim problem data (for end-of-line LF or CRLF)."""

    if printIssues(pipeline.trim()) == Severity.Error:
        raise ClickException("Failed to trim.")


@main.command()
def pack():
    """Pack the problem."""

    if printIssues(pipeline.pack()) == Severity.Error:
        raise ClickException("Failed to pack.")


@main.command()
def check():
    """Check validity of the problem."""

    if printIssues(pipeline.check()) == Severity.Error:
        raise ClickException("Failed to check.")


@main.command()
def initialize():
    """Initialize problem working directory."""

    if printIssues(pipeline.initialize()) == Severity.Error:
        raise ClickException("Failed to initialize.")


if __name__ == '__main__':
    main()
