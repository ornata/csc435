echo off
rem edit the following line to provide the path to the
rem folder containing the gppg.exe program on your PC
path D:\Documents\Hacking\GPPG\gppg-distro-1.5.0\binaries;%PATH%
echo on
gppg /gplex CbParser.y
