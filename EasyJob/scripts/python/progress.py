# A progress bar that rewrites its own line with \r: the output panel shows only the final state of the line.
import random
import sys
import time

run = random.randint(100, 999)
total = 30
for i in range(total + 1):
    bar = "#" * i + "-" * (total - i)
    sys.stdout.write(f"\r[run {run}] [{bar}] {i * 100 // total:3d}%")
    sys.stdout.flush()
    time.sleep(0.15)
print()
print(f"[run {run}] finished")
