import platform
import sys

print(f"Hello from Python {platform.python_version()} on {platform.system()}")
print(f"Interpreter: {sys.executable}")
print(f"Arguments: {sys.argv[1:]}")
print("\033[32mgreen\033[0m \033[1;31mbold red\033[0m \033[4munderlined\033[0m \033[38;2;120;200;255mtruecolor\033[0m")
print("This goes to stderr", file=sys.stderr)
