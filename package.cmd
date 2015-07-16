@echo off
nuget pack ExpressionEvaluator\ExpressionEvaluator.csproj -Properties Configuration=Release -OutputDirectory "ExpressionEvaluator\bin\Release"
pause
