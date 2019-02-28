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
   1. `config.json` Config informations
   2. `description.txt` Description
   3. `input.txt` Description of input
   4. `output.txt` Description of output
   5. `hint.txt` Hint
   6. `samples/` Sample data
   7. `sample/test0.in` Input of sample
   8. `sample/test0.output` Output of sample
   9. `tests/` Test data (same form to `samples/`)
5. After you finish things above, use this command to check whether your directory is available to pack:
```sh
$ ../gop check
```
6. After you fix up all errors, use this command to pack your problem and then you can submit the package:
```sh
$ ../gop pack
```

Have fun!
