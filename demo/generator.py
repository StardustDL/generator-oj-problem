# Import the data container from generator-oj-problem
from generator_oj_problem.generators import data

# Write your generator code
from random import randint
x, y = randint(1, 1000), randint(1, 1000)

# Write to input data, just like `print(x, y, file=input)`
data.In(x, y)

# Write to output data, just like `print(x, y, file=output)`
data.Out(x + y)

# Always keep this invocation at the end of the generator
#   to submit your data.
data.submit()
