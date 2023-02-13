# Migration Commands
Add-Migration -Name "Initial" -verbose

Add-Migration -Context "EngineDataContext" -Name "Initial" -Project ResumableFunction.Engine.Data.Sqlite -Verbose -StartupProject ResumableFunction.Engine.Service


# Force Migration

# Update DataBase
Update-Database -verbose
Update-Database -Context "EngineDataContext" -StartupProject ResumableFunction.Engine.Service

# Remove-Migration 
Remove-Migration
Remove-Migration -Project ResumableFunction.Engine.Data.SqlServer -Verbose -StartupProject ResumableFunction.Engine.Service

# Commands Page
https://learn.microsoft.com/en-us/ef/core/cli/powershell