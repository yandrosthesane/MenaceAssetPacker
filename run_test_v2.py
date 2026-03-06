#!/usr/bin/env python3
"""
MCP client to run tests via the Menace Modkit MCP server
"""
import json
import subprocess
import sys
import os
import time

def send_request(process, method, params=None, request_id=1):
    """Send MCP request"""
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

def read_response(process, timeout=120):
    """Read MCP response with timeout"""
    import select

    start_time = time.time()
    while time.time() - start_time < timeout:
        # Check if data is available
        ready, _, _ = select.select([process.stdout], [], [], 1.0)
        if ready:
            line = process.stdout.readline()
            if line:
                try:
                    return json.loads(line)
                except json.JSONDecodeError as e:
                    print(f"Warning: Failed to parse response: {e}")
                    print(f"Raw line: {repr(line)}")
                    continue

        # Check if process died
        if process.poll() is not None:
            print("Process terminated unexpectedly")
            return None

    print("Timeout waiting for response")
    return None

def main():
    if len(sys.argv) < 2:
        print("Usage: python run_test_v2.py <test_file>")
        sys.exit(1)

    test_file = os.path.abspath(sys.argv[1])

    if not os.path.exists(test_file):
        print(f"Error: Test file not found: {test_file}")
        sys.exit(1)

    mcp_server = "/home/poss/Documents/Code/Menace/MenaceAssetPacker/src/Menace.Modkit.Mcp/bin/Release/net10.0/Menace.Modkit.Mcp"

    if not os.path.exists(mcp_server):
        print(f"Building MCP server...")
        subprocess.run([
            "dotnet", "build",
            "/home/poss/Documents/Code/Menace/MenaceAssetPacker/src/Menace.Modkit.Mcp/Menace.Modkit.Mcp.csproj",
            "-c", "Release"
        ], check=True)

    print(f"Starting MCP server...")
    print(f"Test file: {test_file}")
    print("-" * 80)

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
            print(f"Initialization failed: {response}")
            sys.exit(1)

        print("✓ MCP server initialized\n")

        # Call test_run tool
        print(f"Calling test_run tool...")
        send_request(process, "tools/call", {
            "name": "test_run",
            "arguments": {
                "test": test_file,
                "autoLaunch": True,
                "timeout": 60
            }
        }, 2)

        print("Waiting for test result (this may take a while)...\n")
        response = read_response(process, timeout=180)

        if not response:
            print("❌ No response from test_run")
            sys.exit(1)

        if "error" in response:
            print(f"❌ Test execution error:")
            print(json.dumps(response["error"], indent=2))
            sys.exit(1)

        # Parse result
        if "result" not in response:
            print(f"❌ Unexpected response format:")
            print(json.dumps(response, indent=2))
            sys.exit(1)

        result_data = response["result"]

        # Handle MCP tool response format
        if isinstance(result_data, dict) and "content" in result_data:
            content = result_data["content"]
            if isinstance(content, list) and len(content) > 0:
                result_text = content[0].get("text", "")
            else:
                result_text = json.dumps(content)
        else:
            result_text = json.dumps(result_data) if not isinstance(result_data, str) else result_data

        # Parse test result JSON
        try:
            test_result = json.loads(result_text)
        except json.JSONDecodeError:
            print("❌ Failed to parse test result as JSON:")
            print(result_text)
            sys.exit(1)

        # Display results
        print("\n" + "=" * 80)
        print(f"TEST: {test_result.get('test', 'Unknown')}")
        print("=" * 80)
        print(f"Status: {'✓ PASSED' if test_result.get('passed') else '❌ FAILED'}")
        print(f"Total Steps: {test_result.get('totalSteps', len(test_result.get('steps', [])))}")

        if "error" in test_result:
            print(f"\nOverall Error: {test_result['error']}")

        print("\nStep-by-Step Results:")
        print("-" * 80)

        passed_count = 0
        failed_count = 0

        for i, step in enumerate(test_result.get("steps", []), 1):
            status = step.get("status", "unknown")
            status_icon = "✓" if status == "pass" else ("❌" if status == "fail" else "ℹ")

            if status == "pass":
                passed_count += 1
            elif status == "fail":
                failed_count += 1

            step_name = step.get("name", step.get("step", "Unknown"))
            print(f"{i:3d}. {status_icon} [{status:8s}] {step_name}")

            if status == "fail":
                if "error" in step:
                    print(f"      Error: {step['error']}")
                if "result" in step:
                    result_preview = str(step['result'])[:200]
                    print(f"      Result: {result_preview}")

        print("\n" + "=" * 80)
        print(f"Summary: {passed_count} passed, {failed_count} failed")
        print("=" * 80)

        sys.exit(0 if test_result.get("passed") else 1)

    except KeyboardInterrupt:
        print("\n\n❌ Test interrupted by user")
        sys.exit(130)
    except Exception as e:
        print(f"\n\n❌ Unexpected error: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
    finally:
        process.terminate()
        try:
            process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            process.kill()

if __name__ == "__main__":
    main()
