$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot\..
try {
  Write-Host "Building DeskCall UI and Electron shell..."
  npm run build:ui

  Write-Host "Publishing DeskCall native helper..."
  npm run publish:helper

  Write-Host "Build complete."
}
finally {
  Pop-Location
}
