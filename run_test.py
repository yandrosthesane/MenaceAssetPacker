#!/usr/bin/env python3
"""
Simple MCP client to run tests via the Menace Modkit MCP server
"""
import json
import subprocess
import sys
import os

def send_mcp_request(process, method, params=None, request_id=1):
    """Send an MCP request and read response"""
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

    # Read response
    response_line = process.stdout.readline()
    if not response_line:
        return None

    return json.loads(response_line)

def main():
    if len(sys.argv) < 2:
        print("Usage: python run_test.py <test_file>")
        sys.exit(1)

    test_file = sys.argv[1]

    if not os.path.exists(test_file):
        print(f"Error: Test file not found: {test_file}")
        sys.exit(1)

    # Make test file path absolute
    test_file = os.path.abspath(test_file)

    # Path to MCP server
    mcp_server = "/home/poss/Documents/Code/Menace/MenaceAssetPacker/src/Menace.Modkit.Mcp/bin/Release/net10.0/Menace.Modkit.Mcp"

    if not os.path.exists(mcp_server):
        print(f"MCP server not found, building...")
        subprocess.run([
            "dotnet", "build",
            "/home/poss/Documents/Code/Menace/MenaceAssetPacker/src/Menace.Modkit.Mcp/Menace.Modkit.Mcp.csproj",
            "-c", "Release"
        ], check=True)

    print(f"Starting MCP server and running test: {test_file}")
    print("-" * 80)

    # Start MCP server
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
        response = send_mcp_request(process, "initialize", {
            "protocolVersion": "2024-11-05",
            "capabilities": {},
            "clientInfo": {
                "name": "test-runner",
                "version": "1.0.0"
            }
        }, request_id=1)

        if not response or "error" in response:
            print(f"Initialization failed: {response}")
            sys.exit(1)

        print("✓ MCP server initialized")

        # Call test_run tool
        print(f"\nRunning test from: {test_file}")
        response = send_mcp_request(process, "tools/call", {
            "name": "test_run",
            "arguments": {
                "test": test_file,
                "autoLaunch": True,
                "timeout": 60
            }
        }, request_id=2)

        if not response:
            print("No response from test_run")
            sys.exit(1)

        if "error" in response:
            print(f"\n❌ Test execution error:")
            print(json.dumps(response["error"], indent=2))
            sys.exit(1)

        # Parse result
        if "result" in response:
            result_data = response["result"]

            # The result might be nested in a content array (MCP protocol)
            if isinstance(result_data, dict) and "content" in result_data:
                content = result_data["content"]
                if isinstance(content, list) and len(content) > 0:
                    result_text = content[0].get("text", "")
                else:
                    result_text = str(content)
            else:
                result_text = str(result_data)

            # Try to parse as JSON
            try:
                test_result = json.loads(result_text)
                print("\n" + "=" * 80)
                print(f"TEST RESULTS: {test_result.get('test', 'Unknown')}")
                print("=" * 80)
                print(f"Status: {'✓ PASSED' if test_result.get('passed') else '❌ FAILED'}")
                print(f"Total Steps: {test_result.get('totalSteps', len(test_result.get('steps', [])))}")

                if "error" in test_result:
                    print(f"\nError: {test_result['error']}")

                print("\nStep Results:")
                print("-" * 80)

                for i, step in enumerate(test_result.get("steps", []), 1):
                    status_icon = "✓" if step.get("status") == "pass" else ("❌" if step.get("status") == "fail" else "ℹ")
                    print(f"{i:3d}. {status_icon} [{step.get('status', 'unknown'):8s}] {step.get('name', step.get('step', 'Unknown'))}")

                    if step.get("status") == "fail" and "error" in step:
                        print(f"      Error: {step['error']}")
                    elif "result" in step and step["status"] != "pass":
                        print(f"      Result: {step['result']}")

                print("\n" + "=" * 80)

                # Return exit code based on test result
                sys.exit(0 if test_result.get("passed") else 1)

            except json.JSONDecodeError:
                print(f"\nRaw result (not JSON):")
                print(result_text)
        else:
            print(f"\nUnexpected response format:")
            print(json.dumps(response, indent=2))

    except KeyboardInterrupt:
        print("\n\nTest interrupted by user")
        sys.exit(130)

    finally:
        process.terminate()
        process.wait(timeout=5)

if __name__ == "__main__":
    main()
