@echo off
rem Takes two answers from the argument dialog: a name and a repeat count.
set NAME=%1
set COUNT=%2
if "%NAME%"=="" set NAME=world
if "%COUNT%"=="" set COUNT=3
echo Arguments received: name=%NAME% count=%COUNT%
for /L %%i in (1,1,%COUNT%) do echo Hello, %NAME%! (%%i of %COUNT%)
