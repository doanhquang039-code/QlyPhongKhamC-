$connStr = "Server=127.0.0.1;Database=master;User Id=sa;Password=123456789;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
Write-Host "Connected to SQL Server."

$script = [System.IO.File]::ReadAllText("d:\QlyPhongKhamDesktop\init_database.sql")
$batches = $script -split "(?im)^\s*GO\s*$"

foreach ($b in $batches) {
    $trimmed = $b.Trim()
    if ($trimmed.Length -gt 0) {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $trimmed
        try {
            [void]$cmd.ExecuteNonQuery()
        } catch {
            Write-Error "Error executing batch: $($_.Exception.Message)"
            Write-Host "Batch snippet: $($trimmed.Substring(0, [Math]::Min(100, $trimmed.Length)))"
        }
    }
}

$conn.Close()
Write-Host "Database initialization completed successfully!"
