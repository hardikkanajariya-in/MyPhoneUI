$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot\..
try {
  Write-Host "Building DeskCall native helper..."
  dotnet build native/DeskCall.Helper/DeskCall.Helper.csproj -c Debug

  Write-Host "Starting DeskCall development environment..."
  npx concurrently -k -n vite,electron -c cyan,blue "npm run dev:ui" "npx wait-on tcp:5173 && npm run electron:dev"
}
finally {
  Pop-Location
}
