# .claude/hooks/post-bash.ps1
# Se ejecuta después de cada comando Bash en Claude Code.
# Detecta git push exitoso y registra el contexto del proyecto en memory/push-log.md

$inputJson = $input | Out-String

# Solo actuar si fue un git push exitoso
if ($inputJson -notmatch '"git push') { exit 0 }
if ($inputJson -match '"exitCode":\s*[^0]') { exit 0 }  # push fallido, no registrar

$timestamp  = Get-Date -Format "yyyy-MM-dd HH:mm"
$branch     = git rev-parse --abbrev-ref HEAD 2>$null
$lastCommit = git log -1 --pretty="%h — %s" 2>$null
$recentLog  = git log -5 --oneline 2>$null | ForEach-Object { "  - $_" } | Out-String

$logEntry = @"

## Push · $timestamp

**Rama:** ``$branch``
**Último commit:** $lastCommit

**Últimos 5 commits:**
$recentLog
---
"@

$logPath = Join-Path $PSScriptRoot "..\..\memory\push-log.md"
$logPath = [System.IO.Path]::GetFullPath($logPath)

# Crear el archivo si no existe
if (-not (Test-Path $logPath)) {
    Set-Content -Path $logPath -Value "# Push Log — NewGestERP`n" -Encoding utf8
}

Add-Content -Path $logPath -Value $logEntry -Encoding utf8
Write-Host "[post-bash] Contexto guardado en memory/push-log.md"
