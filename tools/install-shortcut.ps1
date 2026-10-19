param([Parameter(Mandatory)][string]$Target, [Parameter(Mandatory)][string]$Shortcut)
$sh = New-Object -ComObject WScript.Shell
$l = $sh.CreateShortcut($Shortcut)
$l.TargetPath = $Target
$l.WorkingDirectory = Split-Path $Target
$l.IconLocation = "$Target,0"
$l.Description = "KS Fit Remote (unofficial, not affiliated with KingSmith)"
$l.Save()
