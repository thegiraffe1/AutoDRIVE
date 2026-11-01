param([Parameter(Mandatory=$true)][string]$Command, [int]$Port=4580)
$ErrorActionPreference = 'Stop'
$client = [System.Net.Sockets.TcpClient]::new()
try {
    $client.Connect('127.0.0.1', $Port)
    $client.ReceiveTimeout = 15000
    $stream = $client.GetStream()
    $writer = [System.IO.StreamWriter]::new($stream)
    $writer.AutoFlush = $true
    $writer.WriteLine($Command)
    $reader = [System.IO.StreamReader]::new($stream)
    $reader.ReadLine()
} finally { $client.Dispose() }
