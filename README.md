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
      1. `descriptions/description.txt` Description
      2. `descriptions/input.txt` Description of input
      3. `descriptions/output.txt` Description of output
      4. `descriptions/hint.txt` Hint
   3. `samples/` Sample data
      1. `samples/test0.in` Input of sample
      2. `samples/test0.output` Output of sample
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

If you have addition files (such as images), add to `/data/` directory, and all files in this directory will be packed.

Have fun!

If you have any suggestions or you find any bugs, please tell me.
