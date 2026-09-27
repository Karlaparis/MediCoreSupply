# Builds the API and deploys it to Azure App Service (app-medicore-mcs01).
# Requires the .NET SDK and the Azure CLI, signed in with `az login`.
# Usage, from the repo root: .\backend\deploy-api.ps1

$ErrorActionPreference = 'Stop'

$resourceGroup = 'rg-medicore-dev'
$appName = 'app-medicore-mcs01'

$project = Join-Path $PSScriptRoot 'MediCoreSupply.Api'
# Build output goes next to the repo folder, so it stays out of git.
$publishDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\medicore-publish'))
$zipPath = "$publishDir.zip"

# Start clean: dotnet publish never deletes files left over from an earlier build.
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Write-Host "Publishing $project to $publishDir"
dotnet publish $project -c Release -o $publishDir
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

# tar writes zip paths with forward slashes. Windows PowerShell's Compress-Archive
# writes backslashes, which the Linux App Service rejects (Kudu returns 400).
Write-Host "Creating $zipPath"
tar.exe -a -c -f $zipPath -C $publishDir *
if ($LASTEXITCODE -ne 0) { throw 'Creating the zip failed.' }

# --clean true empties /home/site/wwwroot first, so files from earlier deployments don't linger.
Write-Host "Deploying to $appName"
az webapp deploy --resource-group $resourceGroup --name $appName --src-path $zipPath --type zip --clean true
if ($LASTEXITCODE -ne 0) { throw 'Deployment failed.' }

Write-Host 'Deployment finished.'
