#!/bin/sh
# Runs for ~8 seconds. Start it several times to see separate run panels.
id=$$
i=1
while [ "$i" -le 20 ]; do
  echo "[run $id] step $i/20"
  sleep 0.4
  i=$((i + 1))
done
printf '[run %s] \033[32mfinished\033[0m\n' "$id"
