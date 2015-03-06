@echo off
nuget pack ExpressionEvaluator.csproj -Properties Configuration=Release -OutputDirectory "bin\Release"
pause
