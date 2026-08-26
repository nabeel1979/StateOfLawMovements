# Shared SSH helper. Remote output is base64-wrapped so Arabic text survives
# the SSH/console codepage round-trip.

$SshHost = if ($env:QANOON_SSH_HOST) { $env:QANOON_SSH_HOST } else { '37.239.44.94' }
$SshPort = if ($env:QANOON_SSH_PORT) { [int]$env:QANOON_SSH_PORT } else { 22 }
$SshUser = if ($env:QANOON_SSH_USER) { $env:QANOON_SSH_USER } else { 'Administrator' }
$SshPass = $env:QANOON_SSH_PASS
if ([string]::IsNullOrEmpty($SshPass)) {
    throw 'Set QANOON_SSH_PASS before running the deployment/import helpers.'
}

function New-Session {
    Import-Module Posh-SSH -ErrorAction Stop
    $sec  = ConvertTo-SecureString $SshPass -AsPlainText -Force
    $cred = New-Object System.Management.Automation.PSCredential($SshUser, $sec)
    return New-SSHSession -ComputerName $SshHost -Port $SshPort -Credential $cred -AcceptKey -Force
}

function Invoke-Remote {
    param(
        [Parameter(Mandatory)] [int]    $SessionId,
        [Parameter(Mandatory)] [string] $Script,
        [int] $TimeoutSec = 300
    )
    $bytes = [System.Text.Encoding]::Unicode.GetBytes($Script)
    $enc   = [Convert]::ToBase64String($bytes)
    $res   = Invoke-SSHCommand -SessionId $SessionId -Command "powershell -NoProfile -EncodedCommand $enc" -TimeOut $TimeoutSec

    $text = ($res.Output -join "`n")
    # Remote scripts emit payload between markers, base64-encoded UTF-8.
    $m = [regex]::Match($text, '<<B64>>(.*?)<<\/B64>>', 'Singleline')
    if ($m.Success) {
        $payload = $m.Groups[1].Value -replace '\s', ''
        return [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payload))
    }
    return $text + "`n--- STDERR ---`n" + ($res.Error -join "`n")
}

# Prelude injected into remote scripts: DB connection + Emit helper.
$RemotePreludeBody = @'
$ErrorActionPreference = 'Stop'

function Get-Conn {
    Add-Type -AssemblyName System.Data | Out-Null
    $cn = New-Object System.Data.SqlClient.SqlConnection $cs
    $cn.Open()
    return $cn
}

function Invoke-Query([object]$cn, [string]$sql) {
    $cmd = $cn.CreateCommand()
    $cmd.CommandText = $sql
    $cmd.CommandTimeout = 180
    $da = New-Object System.Data.SqlClient.SqlDataAdapter $cmd
    $dt = New-Object System.Data.DataTable
    $da.Fill($dt) | Out-Null
    # Comma keeps PowerShell from unrolling the DataTable into DataRow objects.
    return ,$dt
}

function Invoke-NonQuery([object]$cn, [string]$sql, [hashtable]$prm) {
    $cmd = $cn.CreateCommand()
    $cmd.CommandText = $sql
    $cmd.CommandTimeout = 180
    if ($prm) {
        foreach ($k in $prm.Keys) {
            $v = $prm[$k]
            if ($null -eq $v) { $v = [DBNull]::Value }
            $cmd.Parameters.AddWithValue('@' + $k, $v) | Out-Null
        }
    }
    return $cmd.ExecuteNonQuery()
}

$script:OutLines = New-Object System.Collections.Generic.List[string]
function Say([string]$s) { $script:OutLines.Add($s) }
function Emit {
    $joined = ($script:OutLines -join "`n")
    $b = [System.Text.Encoding]::UTF8.GetBytes($joined)
    Write-Output '<<B64>>'
    Write-Output ([Convert]::ToBase64String($b))
    Write-Output '<</B64>>'
}
'@

# The prelude executes on the server, so the connection string has to be baked
# in from this machine's environment rather than read from the remote env.
# Deployment helpers don't touch the database, so this is resolved on demand.
function Get-RemotePrelude {
    $dbCs = $env:QANOON_DB_CS
    if ([string]::IsNullOrEmpty($dbCs)) {
        throw 'Set QANOON_DB_CS before running the database helpers.'
    }
    return "`$cs = '" + ($dbCs -replace "'", "''") + "'`n" + $script:RemotePreludeBody
}
