import importlib
import pkgutil
from ..pipelines import Pipeline


def all():
    for item in pkgutil.iter_modules(__path__):
        if item.ispkg:
            yield item.name


def build(adapter: str) -> Pipeline:
    try:
        module = importlib.import_module(f".{adapter}", __package__)
    except ImportError:
        raise Exception(f"No such adapter {adapter}")

    from . import generic

    initializer = (getattr(module, "getInitializer", None) or generic.getInitializer)()
    loader = (getattr(module, "getLoader", None) or generic.getLoader)()
    checker = (getattr(module, "getChecker", None) or generic.getChecker)()
    packer = (getattr(module, "getPacker", None) or generic.getPacker)()

    return Pipeline(initializer, loader, checker, packer)
