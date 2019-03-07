# generator-oj-problem

[![Package on NuGet](https://img.shields.io/nuget/vpre/generator-oj-problem.svg)]() [![Download on NuGet](https://img.shields.io/nuget/dt/generator-oj-problem.svg)](https://www.nuget.org/packages/generator-oj-problem/) ![](https://img.shields.io/badge/platform-windows%20%7C%20macos%20%7C%20linux-blue.svg) 

![](https://img.shields.io/librariesio/github/StardustDL/generator-oj-problem.svg) ![](https://img.shields.io/github/issues/StardustDL/generator-oj-problem.svg) [![](https://img.shields.io/github/license/StardustDL/generator-oj-problem.svg)](https://github.com/StardustDL/generator-oj-problem/blob/master/LICENSE.txt)

A CLI tool to generate Online-Judge Problem.

## Install

Use one of the following methods to install this tool:

### Portable

This methods do not install the tool into your system, so you must use the tool by the full-path.
You can also add it to the environment variable `PATH`.

1. Download the release for your OS (uploaded in the group).
2. Unzip the package
3. The tool is in the unzipped directory, named `gop`.

### Global

This methods install the tool into your system, and you can use the tool anywhere.

**Attention**: The tool depends on .NET Core, so you need to install .NET Core first.

1. Install .NET Core **SDK** for your OS.
   - Here is the [website](https://dotnet.microsoft.com/download) to download .NET Core.
2. If you want to use main version:
   1. Use this command to install `gop`:
   ```sh
   $ dotnet tool install -g generator-oj-problem --version 0.1.0-alpha
   ```
3. If you want to use latest version:
   1. Download the `nupkg` file in [release page](https://github.com/StardustDL/generator-oj-problem/releases),
   2. Enter the directory of the `nupkg` file, use this command to install `gop`:
   ```sh
   $ dotnet tool install -g generator-oj-problem --add-source ./ --version 0.1.0-alpha
   ```
4. After installed, use `gop` to run the tool. Use this command to check the tool has been installed:
```sh
$ gop --version

$ gop --help
```

## Getting Started

1. Use these command to create one directory for your problem:
```sh
$ mkdir problem
$ cd problem
$ gop init
```
2. Modify the files in the directory `problem`.
   1. `profile.json` Config information
   2. `descriptions/` All text for description. 
      1. The file with extension `.md` means it supports plain [CommonMark](https://commonmark.org/) by built-in render. 
         - **Attention**: No LaTeX supports.
      2. `descriptions/description.md` Description
      3. `descriptions/input.md` Description of input
      4. `descriptions/output.md` Description of output
      5. `descriptions/hint.md` Hint
      6. `descriptions/source.txt` Source
   3. `samples/` Sample data
      1. `samples/test0.in` Input of sample
      2. `samples/test0.out` Output of sample
   4. `tests/` Test data (same form to `samples/`)
   5. `src/std.cpp` Standard program
3.  After you finish standard program, compile it and fill the `StdRun` field in `profile.json` with the path of compiled executable file (or a list of commands to execute standard program if you use Python).
    - This enables local judger for `check` and `gen` commands you will use later.
4. Add all inputs for samples and tests, and finish step 3, then you can use this command to auto-generate outputs:
```sh
$ gop gen
```
5. After you finish things above, use this command to check whether your directory is available to pack:
```sh
$ gop check

# disable local judger (not recommended)
$ gop check --disable-local-judger
```
6. If you want to preview your problem, use this command:
```sh
$ gop preview

# preview by HTML
$ gop preview --html
```
7. After you fix up all **errors** and **warnings**, use this command to pack your problem and then you can submit the package:
```sh
$ gop pack

# disable local judger when check (not recommended)
$ gop pack --disable-local-judger

# force pack although checking failed
# (not recommended, and the package won't be accepted when submit)
$ gop pack --force
```

Addition:
- If you have addition files (such as images), add to `/extra/` directory, and all files in this directory will be packed.
- Use `--help` to see more information about this tool.

Have fun!

If you have any suggestions or you find any bugs, please tell me.

## Command

These are more details for the commands of `gop`.

### init

Initialize the problem. The current directory must be empty.

```sh
$ gop init
```

### preview

Preview the problem.

|Option|Description|
|-|-|
|`--html`|Generate an HTML document for previewing.|

```sh
$ gop preview
```

### gen

Generate data.

|Option|Description|
|-|-|
|`-o, --output`|Generate output data for samples and tests.|

```sh
$ gop gen
```

### check

Check whether the problem is available to pack.

|Option|Description|
|-|-|
|`--disable-local-judger`|Disable local judging for check.|

```sh
$ gop check
```

### pack

Pack the problem into one package to submit.

|Option|Description|
|-|-|
|`--force`|Pack although checking failing.|
|`--disable-local-judger`|Disable local judging for check.|
|`-p, --platform`|The target platform.|

```sh
$ gop pack
```