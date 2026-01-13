@echo off
echo 正在发布……
dotnet publish UpdateAPI.csproj --output="bin\publish"
pause