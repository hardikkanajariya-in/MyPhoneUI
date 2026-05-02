$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot\..
$viteProcess = $null
try {
  Write-Host "Building DeskCall native helper..."
  dotnet build native/DeskCall.Helper/DeskCall.Helper.csproj -c Debug

  Write-Host "Starting DeskCall development environment..."
  $viteProcess = Start-Process -FilePath "cmd.exe" -ArgumentList "/c", "npm run dev:ui" -WorkingDirectory (Get-Location) -NoNewWindow -PassThru
  npx wait-on tcp:5173
  npm run electron:dev
}
finally {
  if ($null -ne $viteProcess -and -not $viteProcess.HasExited) {
    Stop-Process -Id $viteProcess.Id -Force
  }
  Pop-Location
}
