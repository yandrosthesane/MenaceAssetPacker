#!/usr/bin/env python3
"""
Direct test runner for SkillTemplate validation
Executes test steps via game HTTP API
"""

import json
import requests
import time
import sys

GAME_URL = "http://127.0.0.1:7655"
TEST_FILE = "tests/template-validation/validate_SkillTemplate.json"

def check_game_status():
    """Check if game is running"""
    try:
        response = requests.get(f"{GAME_URL}/status", timeout=2)
        return response.json() if response.status_code == 200 else None
    except:
        return None

def game_repl(code):
    """Execute C# code in game REPL"""
    try:
        response = requests.post(
            f"{GAME_URL}/repl",
            json={"code": code},
            timeout=15
        )
        return response.json() if response.status_code == 200 else None
    except Exception as e:
        return {"success": False, "error": str(e)}

def game_cmd(command):
    """Execute console command in game"""
    try:
        response = requests.post(
            f"{GAME_URL}/cmd",
            json={"cmd": command},
            timeout=15
        )
        return response.json() if response.status_code == 200 else None
    except Exception as e:
        return {"success": False, "error": str(e)}

def test_field(template_type, template_name, field_name):
    """Test if a field can be read from a template"""
    code = f'Menace.SDK.Templates.GetProperty("{template_type}", "{template_name}", "{field_name}") != null'
    result = game_repl(code)

    if result and result.get("success"):
        value = result.get("value", "").lower()
        return value == "true"
    return False

def run_test():
    """Run the SkillTemplate validation test"""
    print("=" * 60)
    print("SkillTemplate Validation Test")
    print("=" * 60)
    print()

    # Check game status
    print("Checking game status...")
    status = check_game_status()
    if not status:
        print("ERROR: Game is not running or ModpackLoader not installed")
        print("Please start the game with MelonLoader first")
        return False

    print(f"✓ Game is running: {status.get('scene', 'unknown scene')}")
    print()

    # Skip navigation - templates should be available in any scene
    print("Skipping navigation - will test in current scene")
    print()

    # Verify templates load
    print("Verifying SkillTemplate templates load...")
    result = game_repl('Menace.SDK.Templates.FindAll("SkillTemplate").Length > 0')
    if result and result.get("value", "").lower() == "true":
        print("✓ SkillTemplate templates loaded")
    else:
        print("✗ Failed to load SkillTemplate templates")
        return False
    print()

    # Test templates and fields
    templates = [
        "active.change_plates",
        "active.deploy_explosive_charge",
        "active.detonate_explosive_charge"
    ]

    fields = [
        "name", "Type", "Order", "ActionPointCost", "IsLimitedUses",
        "Uses", "IsActive", "HideApCosts", "KeyBind", "ExecutingElement",
        "AnimationType", "AimingType", "IsOverrideAimSlot", "OverrideAimSlot",
        "IsTargeted", "TargetingCursor", "TargetsAllowed",
        "KeepSelectedIfStillUsable", "IsLineOfFireNeeded", "IsAttack", "IsAlwaysHitting"
    ]

    results = {"passed": [], "failed": []}

    for template_name in templates:
        print(f"Testing template: {template_name}")
        print("-" * 60)

        # Verify template exists
        exists_code = f'!Menace.SDK.Templates.Find("SkillTemplate", "{template_name}").IsNull'
        result = game_repl(exists_code)

        if not (result and result.get("value", "").lower() == "true"):
            print(f"✗ Template not found: {template_name}")
            for field in fields:
                results["failed"].append(f"{template_name}.{field}")
            print()
            continue

        print(f"✓ Template exists: {template_name}")

        # Test each field
        for field_name in fields:
            passed = test_field("SkillTemplate", template_name, field_name)
            field_label = f"{template_name}.{field_name}"

            if passed:
                results["passed"].append(field_label)
                print(f"  ✓ {field_name}")
            else:
                results["failed"].append(field_label)
                print(f"  ✗ {field_name}")

        print()

    # Print summary
    print("=" * 60)
    print("Test Summary")
    print("=" * 60)
    total = len(results["passed"]) + len(results["failed"])
    print(f"Total fields tested: {total}")
    print(f"Passed: {len(results['passed'])}")
    print(f"Failed: {len(results['failed'])}")
    print()

    if results["failed"]:
        print("Failed fields:")
        for field in results["failed"]:
            print(f"  - {field}")
        print()

    return len(results["failed"]) == 0

if __name__ == "__main__":
    success = run_test()
    sys.exit(0 if success else 1)
