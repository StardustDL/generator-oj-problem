# ![Generator-OJ-Problem](https://socialify.git.ci/StardustDL/generator-oj-problem/image?description=1&font=Bitter&forks=1&issues=1&language=1&owner=1&pulls=1&stargazers=1&theme=Light)

[![](https://github.com/StardustDL/generator-oj-problem/workflows/CI/badge.svg)](https://github.com/StardustDL/generator-oj-problem/actions) [![](https://img.shields.io/github/license/StardustDL/generator-oj-problem.svg)](https://github.com/StardustDL/generator-oj-problem/blob/master/LICENSE) [![](https://img.shields.io/pypi/v/generator-oj-problem)](https://pypi.org/project/generator-oj-problem/) [![Downloads](https://pepy.tech/badge/generator-oj-problem?style=flat)](https://pepy.tech/project/generator-oj-problem) ![](https://img.shields.io/pypi/implementation/generator-oj-problem.svg?logo=pypi) ![](https://img.shields.io/pypi/pyversions/generator-oj-problem.svg?logo=pypi) ![](https://img.shields.io/pypi/wheel/generator-oj-problem.svg?logo=pypi) ![](https://img.shields.io/badge/Linux-yes-success?logo=linux) ![](https://img.shields.io/badge/Windows-yes-success?logo=windows) ![](https://img.shields.io/badge/MacOS-yes-success?logo=apple) ![](https://img.shields.io/badge/BSD-yes-success?logo=freebsd)

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

# Check validaty
gop check

# Pack your problem in FreeProblemSet format
gop -a fps pack
```

> If you meet some encoding errors, ensure your Python interpreter runs in UTF-8 mode, e.g. adding **PYTHONUTF8=1** to your environment variables.
