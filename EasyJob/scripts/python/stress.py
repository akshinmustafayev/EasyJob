# 20 000 lines as fast as possible: checks that the output panel stays responsive.
for i in range(1, 20001):
    print(f"line {i:5d} " + "x" * (i % 60))
print("done")
