# PowerShell script to remove already-tracked files that should be ignored
# Run this script from the repository root

Write-Host "Cleaning up tracked files that should be ignored..." -ForegroundColor Yellow

# Remove tracked files that match .gitignore patterns
git rm -r --cached Library/ 2>$null
git rm -r --cached Temp/ 2>$null
git rm -r --cached Obj/ 2>$null
git rm -r --cached Build/ 2>$null
git rm -r --cached Builds/ 2>$null
git rm -r --cached Logs/ 2>$null
git rm -r --cached UserSettings/ 2>$null
git rm -r --cached .vs/ 2>$null
git rm -r --cached GeneratedAssets_deleted/ 2>$null

# Remove tracked solution/project files
git rm --cached *.sln 2>$null
git rm --cached *.slnx 2>$null
git rm --cached *.csproj 2>$null
git rm --cached *.unityproj 2>$null

Write-Host "`nDone! Now commit these changes:" -ForegroundColor Green
Write-Host "  git add .gitignore" -ForegroundColor Cyan
Write-Host "  git commit -m 'Update .gitignore and remove tracked build files'" -ForegroundColor Cyan

