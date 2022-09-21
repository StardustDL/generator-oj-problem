from generator_oj_problem.generators import data
from random import randint

x, y = randint(1, 1000), randint(1, 1000)
data.In(x, y)
data.Out(x + y)

data.submit()