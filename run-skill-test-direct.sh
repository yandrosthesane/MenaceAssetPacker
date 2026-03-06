#!/bin/bash
# Direct test runner - executes test steps via game HTTP API
# Requires the game to be running with ModpackLoader

GAME_URL="http://127.0.0.1:7655"
TEST_FILE="tests/template-validation/validate_SkillTemplate.json"

echo "==== SkillTemplate Validation Test ===="
echo ""

# Check if game is running
echo "Checking game status..."
STATUS=$(curl -s $GAME_URL/status 2>/dev/null)
if [ $? -ne 0 ]; then
    echo "ERROR: Game is not running or ModpackLoader not installed"
    echo "Please start the game with MelonLoader first"
    exit 1
fi

echo "Game is running: $STATUS"
echo ""

# Navigate to main menu
echo "Step 1: Navigating to main menu..."
RESULT=$(curl -s -X POST -H "Content-Type: application/json" \
    -d '{"cmd":"test.goto_main"}' \
    $GAME_URL/cmd)
echo "$RESULT"
echo ""

# Wait for scene load
echo "Step 2: Waiting 3 seconds for scene load..."
sleep 3
echo ""

# Test SkillTemplate loading
echo "Step 3: Verifying SkillTemplate templates load..."
RESULT=$(curl -s -X POST -H "Content-Type: application/json" \
    -d '{"code":"Menace.SDK.Templates.FindAll(\"SkillTemplate\").Length > 0"}' \
    $GAME_URL/repl)
echo "$RESULT"
echo ""

# Testing fields for active.change_plates
echo "===== Testing Template: active.change_plates ====="

echo "Verifying template exists..."
RESULT=$(curl -s -X POST -H "Content-Type: application/json" \
    -d '{"code":"!Menace.SDK.Templates.Find(\"SkillTemplate\", \"active.change_plates\").IsNull"}' \
    $GAME_URL/repl)
echo "$RESULT"
echo ""

# Test each field
FIELDS=("name" "Type" "Order" "ActionPointCost" "IsLimitedUses" "Uses" "IsActive" "HideApCosts" "KeyBind" "ExecutingElement" "AnimationType" "AimingType" "IsOverrideAimSlot" "OverrideAimSlot" "IsTargeted" "TargetingCursor" "TargetsAllowed" "KeepSelectedIfStillUsable" "IsLineOfFireNeeded" "IsAttack" "IsAlwaysHitting")

for FIELD in "${FIELDS[@]}"; do
    echo "Testing field: $FIELD"
    RESULT=$(curl -s -X POST -H "Content-Type: application/json" \
        -d "{\"code\":\"Menace.SDK.Templates.GetProperty(\\\"SkillTemplate\\\", \\\"active.change_plates\\\", \\\"$FIELD\\\") != null\"}" \
        $GAME_URL/repl)

    # Check if result contains "value": "true"
    if echo "$RESULT" | grep -q '"value":"true"' || echo "$RESULT" | grep -q '"value": "true"'; then
        echo "  ✓ PASS - $FIELD"
    else
        echo "  ✗ FAIL - $FIELD"
        echo "  Response: $RESULT"
    fi
    echo ""
done

echo "==== Test Complete ===="
