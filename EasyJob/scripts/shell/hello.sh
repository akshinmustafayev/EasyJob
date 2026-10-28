#!/bin/sh
echo "Hello from sh on $(uname -s)"
echo "Arguments: $*"
printf '\033[32mgreen\033[0m \033[1;31mbold red\033[0m \033[4munderlined\033[0m\n'
printf '\\c05EJteal tag/c05EJ and \\c04EJcrimson tag/c04EJ\n'
echo "This goes to stderr" >&2
echo "Working directory: $(pwd)"
