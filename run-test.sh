#!/bin/bash
# Run a test via the MCP test_run tool
# This script uses stdio to communicate with the MCP server

# Path to test file
TEST_FILE="${1:-tests/template-validation/validate_SkillTemplate.json}"

if [ ! -f "$TEST_FILE" ]; then
    echo "Error: Test file not found: $TEST_FILE"
    exit 1
fi

# Create MCP request JSON
REQUEST=$(cat <<EOF
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "test_run",
    "arguments": {
      "test": "$(pwd)/$TEST_FILE",
      "autoLaunch": true,
      "timeout": 60
    }
  }
}
EOF
)

echo "Running test: $TEST_FILE"
echo ""

# Send request to MCP server
cd "$(dirname "$0")"
echo "$REQUEST" | dotnet run --project src/Menace.Modkit.Mcp 2>&1
