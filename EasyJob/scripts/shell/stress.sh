#!/bin/sh
i=1
while [ "$i" -le 5000 ]; do
  echo "line $i of 5000"
  i=$((i + 1))
done
echo "done"
