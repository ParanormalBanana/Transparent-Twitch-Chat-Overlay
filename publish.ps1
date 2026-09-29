param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

Write-Host "Building NativeChat..." -ForegroundColor Cyan
Push-Location .\NativeChatClient
if (-not (Test-Path .\node_modules)) {
    Write-Host "Restoring NativeChat npm dependencies..." -ForegroundColor Cyan
    npm.cmd ci
    if ($LASTEXITCODE -ne 0) { Write-Host "npm ci failed." -ForegroundColor Red; exit 1 }
}
npm.cmd run build
if ($LASTEXITCODE -ne 0) { Write-Host "npm build failed." -ForegroundColor Red; exit 1 }
Pop-Location

Write-Host "Publishing .NET app..." -ForegroundColor Cyan
dotnet publish .\TransparentTwitchChatWPF\TransparentTwitchChatWPF.csproj -c Release -o .\publish
if ($LASTEXITCODE -ne 0) { Write-Host "dotnet publish failed." -ForegroundColor Red; exit 1 }

$env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"

Write-Host "Packaging with vpk..." -ForegroundColor Cyan
vpk pack --packId TransparentTwitchChat --packVersion $Version --packDir .\publish --mainExe TransparentTwitchChatWPF.exe --releaseNotes .\CHANGELOG.md
if ($LASTEXITCODE -ne 0) { Write-Host "vpk packaging failed." -ForegroundColor Red; exit 1 }

Write-Host "Uploading to GitHub..." -ForegroundColor Cyan
$token = gh auth token
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($token)) { Write-Host "GitHub login required (gh auth login)." -ForegroundColor Red; exit 1 }
vpk upload github --repoUrl https://github.com/ParanormalBanana/Transparent-Twitch-Chat-Overlay --token $token --publish --releaseName "v$Version"
if ($LASTEXITCODE -ne 0) { Write-Host "vpk upload failed." -ForegroundColor Red; exit 1 }

Write-Host "Done! Published version $Version" -ForegroundColor Green
