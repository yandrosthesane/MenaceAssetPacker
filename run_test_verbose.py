#!/usr/bin/env python3
"""
Verbose MCP client to see full test results
"""
import json
import subprocess
import sys
import os
import time
import select

def send_request(process, method, params=None, request_id=1):
    request = {
        "jsonrpc": "2.0",
        "id": request_id,
        "method": method
    }
    if params:
        request["params"] = params

    request_line = json.dumps(request) + "\n"
    process.stdin.write(request_line)
    process.stdin.flush()

def read_response(process, timeout=180):
    start_time = time.time()
    while time.time() - start_time < timeout:
        ready, _, _ = select.select([process.stdout], [], [], 1.0)
        if ready:
            line = process.stdout.readline()
            if line:
                # Skip log lines
                if line.startswith('['):
                    print(f"[LOG] {line.strip()}", file=sys.stderr)
                    continue

                try:
                    return json.loads(line)
                except json.JSONDecodeError:
                    continue

        if process.poll() is not None:
            return None

    return None

def main():
    if len(sys.argv) < 2:
        print("Usage: python run_test_verbose.py <test_file>")
        sys.exit(1)

    test_file = os.path.abspath(sys.argv[1])

    if not os.path.exists(test_file):
        print(f"Error: Test file not found: {test_file}")
        sys.exit(1)

    mcp_server = "/home/poss/Documents/Code/Menace/MenaceAssetPacker/src/Menace.Modkit.Mcp/bin/Release/net10.0/Menace.Modkit.Mcp"

    print(f"Running test: {os.path.basename(test_file)}")
    print("=" * 80)

    process = subprocess.Popen(
        [mcp_server],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        bufsize=1
    )

    try:
        # Initialize
        send_request(process, "initialize", {
            "protocolVersion": "2024-11-05",
            "capabilities": {},
            "clientInfo": {"name": "test-runner", "version": "1.0.0"}
        }, 1)

        response = read_response(process, timeout=10)
        if not response or "error" in response:
            print(f"Initialization failed")
            sys.exit(1)

        # Call test_run
        send_request(process, "tools/call", {
            "name": "test_run",
            "arguments": {
                "test": test_file,
                "autoLaunch": True,
                "timeout": 60
            }
        }, 2)

        response = read_response(process, timeout=180)

        if not response or "error" in response:
            print(f"Test failed: {response.get('error') if response else 'No response'}")
            sys.exit(1)

        result_data = response["result"]

        # Handle MCP format
        if isinstance(result_data, dict) and "content" in result_data:
            content = result_data["content"]
            if isinstance(content, list) and len(content) > 0:
                result_text = content[0].get("text", "")
            else:
                result_text = json.dumps(content)
        else:
            result_text = json.dumps(result_data) if not isinstance(result_data, str) else result_data

        test_result = json.loads(result_text)

        # Full JSON output
        print("\nFULL TEST RESULT JSON:")
        print(json.dumps(test_result, indent=2))

    finally:
        process.terminate()
        try:
            process.wait(timeout=5)
        except:
            process.kill()

if __name__ == "__main__":
    main()
