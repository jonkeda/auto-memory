# Test MCP server basic protocol
$ErrorActionPreference = "Stop"

$mcpPath = "e:\repos\Private\auto-memoryNet\net\src\AutoMemory.Mcp\bin\Release\net10.0\win-x64\session-recall-mcp.exe"

if (-not (Test-Path $mcpPath)) {
    Write-Error "MCP server not found at $mcpPath"
    exit 1
}

Write-Host "Testing MCP server..." -ForegroundColor Cyan

# Test 1: Initialize
Write-Host "`nTest 1: Initialize" -ForegroundColor Yellow
$initRequest = @'
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","clientInfo":{"name":"test-client","version":"0.1.0"}}}
'@

$initResponse = $initRequest | & $mcpPath | Select-Object -First 1
Write-Host "Response: $initResponse"

if ($initResponse -notmatch '"serverInfo"') {
    Write-Error "Initialize failed: no serverInfo in response"
    exit 1
}
Write-Host "✓ Initialize OK" -ForegroundColor Green

# Test 2: List tools
Write-Host "`nTest 2: List tools" -ForegroundColor Yellow
$listRequest = @'
{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
'@

$listResponse = "$initRequest`n$listRequest" | & $mcpPath | Select-Object -Last 1
Write-Host "Response: $listResponse"

if ($listResponse -notmatch '"recall.list"') {
    Write-Error "tools/list failed: no recall.list in response"
    exit 1
}

# Check all expected tools are present
$expectedTools = @('recall.list', 'recall.files', 'recall.checkpoints', 'recall.search', 'recall.show', 'recall.health', 'recall.schema_check')
foreach ($tool in $expectedTools) {
    if ($listResponse -notmatch $tool) {
        Write-Error "tools/list failed: missing $tool"
        exit 1
    }
}
Write-Host "✓ All 7 tools listed" -ForegroundColor Green

# Test 3: Call recall.health
Write-Host "`nTest 3: Call recall.health" -ForegroundColor Yellow
$healthRequest = @'
{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"recall.health","arguments":{"json":true}}}
'@

$healthResponse = "$initRequest`n$healthRequest" | & $mcpPath | Select-Object -Last 1
Write-Host "Response: $healthResponse"

if ($healthResponse -notmatch '"content"') {
    Write-Error "tools/call health failed: no content in response"
    exit 1
}
Write-Host "✓ recall.health executed" -ForegroundColor Green

Write-Host "`n✓ All MCP tests passed!" -ForegroundColor Green
