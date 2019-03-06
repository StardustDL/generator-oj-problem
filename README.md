# generator-oj-problem

A CLI tool to generate Online-Judge Problem.

# Getting Started

1. Download the release for your OS.
2. Enter the directory of `gop`.
3. Use these command to create one directory for your problem:
```sh
$ mkdir problem
$ cd problem
$ ../gop init
```
4. Modify the files in the directory `problem`.
   1. `config.json` Config information
   2. `descriptions/` All text for description. 
      1. The file with extension `.md` means it supports plain [CommonMark](https://commonmark.org/) by built-in render. 
         - **Attention**: No LaTex supports.
      2. `descriptions/description.md` Description
      3. `descriptions/input.md` Description of input
      4. `descriptions/output.md` Description of output
      5. `descriptions/hint.md` Hint
      6. `descriptions/source.txt` Source
   3. `samples/` Sample data
      1. `samples/test0.in` Input of sample
      2. `samples/test0.out` Output of sample
   4.  `tests/` Test data (same form to `samples/`)
   5.  `src/std.cpp` Standard program
5. After you finish things above, use this command to check whether your directory is available to pack:
```sh
$ ../gop check
```
6. If you want to preview your problem, use this command:
```sh
$ ../gop preview
```
7. After you fix up all **errors** and **warnings**, use this command to pack your problem and then you can submit the package:
```sh
$ ../gop pack
```

Addition:
- If you have addition files (such as images), add to `/extra/` directory, and all files in this directory will be packed.
- Use `--help` to see more information about this tool.

Have fun!

If you have any suggestions or you find any bugs, please tell me.
