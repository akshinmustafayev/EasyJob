# Takes two answers from the argument dialog: a name and a repeat count.
import sys

name = sys.argv[1] if len(sys.argv) > 1 else "world"
count = int(sys.argv[2]) if len(sys.argv) > 2 else 3
print(f"Arguments received: name={name!r} count={count}")
for i in range(1, count + 1):
    print(f"Hello, {name}! ({i} of {count})")
