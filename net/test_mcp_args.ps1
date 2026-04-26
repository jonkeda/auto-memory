# Test MCP server tool call with arguments
$ErrorActionPreference = "Stop"

$mcpPath = "e:\repos\Private\auto-memoryNet\net\src\AutoMemory.Mcp\bin\Release\net10.0\win-x64\session-recall-mcp.exe"

Write-Host "Testing MCP tool call with arguments..." -ForegroundColor Cyan

# Initialize first
$initRequest = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","clientInfo":{"name":"test-client","version":"0.1.0"}}}'

# Test recall.list with limit argument
Write-Host "`nTest: recall.list with limit=3, json=true" -ForegroundColor Yellow
$listRequest = '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"recall.list","arguments":{"limit":3,"json":true}}}'

$listResponse = "$initRequest`n$listRequest" | & $mcpPath | Select-Object -Last 1
Write-Host "Response (first 200 chars): $($listResponse.Substring(0, [Math]::Min(200, $listResponse.Length)))..."

if ($listResponse -notmatch '"sessions"') {
    Write-Error "recall.list failed: no sessions in response"
    exit 1
}
Write-Host "✓ recall.list with arguments OK" -ForegroundColor Green

# Test recall.search with query argument
Write-Host "`nTest: recall.search with query='test'" -ForegroundColor Yellow
$searchRequest = '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"recall.search","arguments":{"query":"test","limit":5,"json":true}}}'

$searchResponse = "$initRequest`n$searchRequest" | & $mcpPath | Select-Object -Last 1
Write-Host "Response (first 200 chars): $($searchResponse.Substring(0, [Math]::Min(200, $searchResponse.Length)))..."

if ($searchResponse -notmatch '"content"') {
    Write-Error "recall.search failed: no content in response"
    exit 1
}
Write-Host "✓ recall.search with arguments OK" -ForegroundColor Green

Write-Host "`n✓ All argument parsing tests passed!" -ForegroundColor Green
