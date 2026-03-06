#!/bin/bash
# Script to run a test using the Menace Modkit MCP server

set -e

TEST_FILE="$1"

if [ -z "$TEST_FILE" ]; then
    echo "Usage: $0 <test_file>"
    exit 1
fi

if [ ! -f "$TEST_FILE" ]; then
    echo "Error: Test file not found: $TEST_FILE"
    exit 1
fi

# Path to MCP server binary
MCP_SERVER="/home/poss/Documents/Code/Menace/MenaceAssetPacker/src/Menace.Modkit.Mcp/bin/Release/net10.0/Menace.Modkit.Mcp"

if [ ! -f "$MCP_SERVER" ]; then
    echo "Error: MCP server not found at $MCP_SERVER"
    echo "Building MCP server..."
    dotnet build /home/poss/Documents/Code/Menace/MenaceAssetPacker/src/Menace.Modkit.Mcp/Menace.Modkit.Mcp.csproj -c Release
fi

# Create MCP request to call test_run tool
REQUEST=$(cat <<EOF
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test-runner","version":"1.0.0"}}}
{"jsonrpc":"2.0","id":2,"method":"initialized"}
{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"test_run","arguments":{"test":"$TEST_FILE","autoLaunch":true,"timeout":60}}}
EOF
)

# Run MCP server and send request
echo "$REQUEST" | "$MCP_SERVER"
