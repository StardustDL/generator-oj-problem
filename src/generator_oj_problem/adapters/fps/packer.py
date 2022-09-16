from datetime import datetime
from glob import glob
import os
from pathlib import Path
from typing import Iterable
from generator_oj_problem.models import Issue, Problem, Severity, TestCase
from generator_oj_problem.pipelines import Reader, Packer
from generator_oj_problem import __version__
from yaml import safe_load
from xml.etree.ElementTree import Element, ElementTree, SubElement
from xml.dom.minidom import Document
import mistune


class Fps(Packer):
    def markdown(self, content: str):
        md = mistune.create_markdown(
            plugins=["url", "task_lists", "def_list", "abbr"])
        return md(content)

    def pack(self, reader: Reader, dist: Path) -> "Iterable[Issue]":
        problem = Problem()
        print("Load problem...")
        yield from reader.load(problem)

        doc = Document()

        root = doc.createElement("fps")
        doc.appendChild(root)
        root.setAttribute("url", "https://github.com/zhblue/freeproblemset/")
        root.setAttribute("version", "1.2")

        gen = doc.createElement("generator")
        root.appendChild(gen)
        gen.setAttribute("name", "HUSTOJ")
        gen.setAttribute("url", "https://github.com/zhblue/hustoj/")

        item = doc.createElement("item")
        root.appendChild(item)

        print("Pack description...")

        sub = doc.createElement("title")
        item.appendChild(sub)
        sub.appendChild(doc.createCDATASection(problem.name))

        sub = doc.createElement("time_limit")
        item.appendChild(sub)
        sub.setAttribute("unit", "s")
        sub.appendChild(doc.createCDATASection(str(int(problem.time))))

        sub = doc.createElement("memory_limit")
        item.appendChild(sub)
        sub.setAttribute("unit", "mb")
        sub.appendChild(doc.createCDATASection(str(int(problem.memory))))

        sub = doc.createElement("description")
        item.appendChild(sub)
        sub.appendChild(doc.createCDATASection(
            self.markdown(problem.description)))

        sub = doc.createElement("input")
        item.appendChild(sub)
        sub.appendChild(doc.createCDATASection(self.markdown(problem.input)))

        sub = doc.createElement("output")
        item.appendChild(sub)
        sub.appendChild(doc.createCDATASection(self.markdown(problem.output)))

        print("Pack sample data...")

        for case in reader.samples():
            print(f"  Pack sample {case.name}...")
            subin = doc.createElement("sample_input")
            subin.appendChild(doc.createCDATASection(
                "\n".join(case.input.splitlines()) + "\n"))

            subout = doc.createElement("sample_output")
            subout.appendChild(doc.createCDATASection(
                "\n".join(case.output.splitlines()) + "\n"))

            item.appendChild(subin)
            item.appendChild(subout)

        print("Pack test data...")

        for case in reader.tests():
            print(f"  Pack test {case.name}...")
            subin = doc.createElement("test_input")
            subin.appendChild(doc.createCDATASection(
                "\n".join(case.input.splitlines()) + "\n"))

            subout = doc.createElement("test_output")
            subout.appendChild(doc.createCDATASection(
                "\n".join(case.output.splitlines()) + "\n"))

            item.appendChild(subin)
            item.appendChild(subout)

        print("Pack extra...")

        hint = f"""{problem.hint}

*Generated at {datetime.now()} by [generator-oj-problem](https://github.com/StardustDL/generator-oj-problem) v{__version__}.*
"""

        if not hint.isspace():
            sub = doc.createElement("hint")
            item.appendChild(sub)
            sub.appendChild(doc.createCDATASection(
                self.markdown(hint)))

        if not problem.author.isspace():
            sub = doc.createElement("source")
            item.appendChild(sub)
            sub.appendChild(doc.createCDATASection(problem.author))

        if not problem.solution.isspace():
            sub = doc.createElement("solution")
            item.appendChild(sub)
            sub.setAttribute("language", problem.solutionLanguage)
            sub.appendChild(doc.createCDATASection(problem.solution))

        print("Save dist...")
        try:
            fp = dist / "fps.xml"
            with open(fp, "w", encoding="utf-8") as f:
                doc.writexml(f, encoding="utf-8")
            yield Issue(f"Saved to {fp}.")
        except Exception as ex:
            yield Issue(f"Failed to save: {ex}", Severity.Error)
