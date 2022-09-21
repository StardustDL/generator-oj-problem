# ![Generator-OJ-Problem](https://socialify.git.ci/StardustDL/generator-oj-problem/image?description=1&font=Bitter&forks=1&issues=1&language=1&owner=1&pulls=1&stargazers=1&theme=Light)

[![](https://github.com/StardustDL/generator-oj-problem/workflows/CI/badge.svg)](https://github.com/StardustDL/generator-oj-problem/actions) [![](https://img.shields.io/github/license/StardustDL/generator-oj-problem.svg)](https://github.com/StardustDL/generator-oj-problem/blob/master/LICENSE) [![](https://img.shields.io/pypi/v/generator-oj-problem)](https://pypi.org/project/generator-oj-problem/) [![Downloads](https://pepy.tech/badge/generator-oj-problem?style=flat)](https://pepy.tech/project/generator-oj-problem) ![](https://img.shields.io/pypi/implementation/generator-oj-problem.svg?logo=pypi) ![](https://img.shields.io/pypi/pyversions/generator-oj-problem.svg?logo=pypi) ![](https://img.shields.io/pypi/wheel/generator-oj-problem.svg?logo=pypi) ![](https://img.shields.io/badge/Linux-yes-success?logo=linux) ![](https://img.shields.io/badge/Windows-yes-success?logo=windows) ![](https://img.shields.io/badge/MacOS-yes-success?logo=apple) ![](https://img.shields.io/badge/BSD-yes-success?logo=freebsd)

A command-line tool to generate Online-Judge problem.

- Render problem descriptions in Markdown to HTML
- Check problem descriptions and data, including missing fields, UTF-8 encoding, end-of-line CRLF/LF
- Packing problem data in freeproblemset(hustoj) format
- Mechanism for generating input and output test data
- Easy to define adapters for other online-judge platform

Have fun! If you have any suggestions or find any bugs, please tell me.

## Install

```sh
pip install generator-oj-problem

# or use pipx for a standalone python environment
pip install pipx
pipx ensurepath
pipx install generator-oj-problem

gop --help
```

## Usage

```sh
# Initialize your problem
gop init

# Modify the files to write problem
ls .

# Generate 2 sample data from id 1
gop gen -s 1 -c 2 --sample
# Generate 5 test data from id 2
gop gen -s 2 -c 5

# Trim sample and test data
gop trim

# Check validaty
gop check

# Pack your problem in FreeProblemSet format
gop -a fps pack
```

> If you meet some encoding errors, ensure your Python interpreter runs in UTF-8 mode, e.g. adding **PYTHONUTF8=1** to your environment variables.

## Directory Structure

> Here is a demo problem [A + B Problem](https://github.com/StardustDL/generator-oj-problem/tree/master/demo). Details about [`problem.yml`](https://github.com/StardustDL/generator-oj-problem/tree/master/demo/problem.yml) and [`generator.py`](https://github.com/StardustDL/generator-oj-problem/tree/master/demo/generator.py) are given at the comments of the demo files.
> 
> The file with extension `.md` means it supports plain [CommonMark](https://commonmark.org/) by built-in render. **Attention**: No LaTeX and embeded image supports.

- `problem.yml` Problem metadata and configuration
- `description.md` Description
- `input.md` Description of input
- `output.md` Description of output
- `hint.md` Hint
- `solution.txt` Solution source code
- `generator.py` Generator for input or output data.
- `samples/` Sample data
   - `samples/0.in` Input of sample
   - `samples/0.out` Output of sample
- `tests/` Test data (same form to `samples/`)
