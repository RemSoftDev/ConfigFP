param(
    [Parameter(Mandatory=$true, HelpMessage="Specifies the path the .bacpac to import.")]  
    [string]$bacpacPath,
    [Parameter(Mandatory=$true, HelpMessage="connectionString to SQL Server in format: user id=sa;password=1qaz!QAZ;Data Source=LVAGPLTP2642;Database=Sitecore_core; Integrated Security=false;")]  
    [string]$connectionString 
)

& "C:\Program Files (x86)\Microsoft SQL Server\140\DAC\bin\SqlPackage.exe" /Action:Import /SourceFile:$bacpacPath /TargetConnectionString:$connectionString 
