#!/usr/bin/env python3
"""
Debug MCP client to see what's happening
"""
import json
import subprocess
import sys
import os

def main():
    test_file = sys.argv[1] if len(sys.argv) > 1 else "tests/template-validation/validate_WeaponTemplate.json"
    test_file = os.path.abspath(test_file)

    mcp_server = "/home/poss/Documents/Code/Menace/MenaceAssetPacker/src/Menace.Modkit.Mcp/bin/Release/net10.0/Menace.Modkit.Mcp"

    print(f"Starting MCP server: {mcp_server}")

    process = subprocess.Popen(
        [mcp_server],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        bufsize=1
    )

    # Send initialize request
    init_request = json.dumps({
        "jsonrpc": "2.0",
        "id": 1,
        "method": "initialize",
        "params": {
            "protocolVersion": "2024-11-05",
            "capabilities": {},
            "clientInfo": {"name": "test-runner", "version": "1.0.0"}
        }
    }) + "\n"

    print(f"\nSending: {init_request.strip()}")
    process.stdin.write(init_request)
    process.stdin.flush()

    # Read response
    print("\nReading response...")
    response_line = process.stdout.readline()
    print(f"Raw response: {repr(response_line)}")

    if response_line:
        try:
            response = json.loads(response_line)
            print(f"Parsed response: {json.dumps(response, indent=2)}")
        except json.JSONDecodeError as e:
            print(f"JSON decode error: {e}")
            print(f"Response text: {response_line}")

    # Check stderr
    process.poll()
    if process.returncode is None:
        print("\nProcess still running, trying to read more...")
        import time
        time.sleep(1)

    process.terminate()
    stderr = process.stderr.read()
    if stderr:
        print(f"\nStderr output:\n{stderr}")

if __name__ == "__main__":
    main()
