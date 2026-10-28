@echo off
rem Runs for ~10 seconds (ping is used as a sleep because "timeout" needs a console).
for /L %%i in (1,1,10) do (
  echo step %%i/10
  ping -n 2 127.0.0.1 >nul
)
echo finished
