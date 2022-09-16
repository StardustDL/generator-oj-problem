import os
import pathlib

__version__ = "0.0.1"


def getAppDirectory() -> pathlib.Path:
    return pathlib.Path(__file__).parent.resolve()


def getWorkingDirectory() -> pathlib.Path:
    return pathlib.Path(os.getcwd()).resolve()
