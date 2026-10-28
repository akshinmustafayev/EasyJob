#!/bin/sh
# Takes two answers from the argument dialog: a name and a repeat count.
name="${1:-world}"
count="${2:-3}"
echo "Arguments received: name='$name' count=$count"
i=1
while [ "$i" -le "$count" ]; do
  echo "Hello, $name! ($i of $count)"
  i=$((i + 1))
done
