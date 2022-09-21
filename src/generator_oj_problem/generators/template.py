from generator_oj_problem.generators import data
from random import randint

x, y = randint(), randint()
data.In(x, y)
data.Out(x + y)

data.submit()