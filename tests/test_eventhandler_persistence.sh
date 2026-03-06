#!/bin/bash
# Automated EventHandler Persistence Test
# Tests the complete flow: navigate, select template, set EventHandlers, verify persistence

set -e

BASE_URL="http://127.0.0.1:21421"
LOGFILE="$HOME/Documents/MenaceModkit/modkit.log"

echo "=== EventHandler Persistence Test ==="
echo ""

# Step 1: Check server
echo "Step 1: Checking UI server is running..."
if ! curl -s --max-time 2 "$BASE_URL/" | grep -q "running"; then
  echo "✗ FAIL: UI server not responding"
  echo "  Please start the modkit first"
  exit 1
fi
echo "✓ PASS: UI server is running"
echo ""

# Step 2: Navigate to Data section
echo "Step 2: Navigating to Data section..."
RESPONSE=$(curl -s -X POST "$BASE_URL/ui/navigate" \
  -H "Content-Type: application/json" \
  -d '{"section":"moddingtools","subSection":"data"}')

if echo "$RESPONSE" | grep -q '"success": true'; then
  echo "✓ PASS: Navigated to Data section"
else
  echo "✗ FAIL: Navigation failed"
  echo "  Response: $RESPONSE"
  exit 1
fi
sleep 1
echo ""

# Step 3: Get first SkillTemplate
echo "Step 3: Discovering available templates..."
FIRST_SKILL=$(curl -s "$BASE_URL/ui/templates" | jq -r '.templates.SkillTemplate[0]')

if [ -z "$FIRST_SKILL" ] || [ "$FIRST_SKILL" == "null" ]; then
  echo "✗ FAIL: No SkillTemplates found"
  exit 1
fi
echo "✓ PASS: Found template: $FIRST_SKILL"
echo ""

# Step 4: Select SkillTemplate category
echo "Step 4: Selecting SkillTemplate category..."
RESPONSE=$(curl -s -X POST "$BASE_URL/ui/select" \
  -H "Content-Type: application/json" \
  -d '{"target":"templatetype","value":"SkillTemplate"}')

if echo "$RESPONSE" | grep -q '"success": true'; then
  echo "✓ PASS: Selected SkillTemplate category"
else
  echo "✗ FAIL: Failed to select category"
  echo "  Response: $RESPONSE"
  exit 1
fi
sleep 0.5
echo ""

# Step 5: Select specific template
echo "Step 5: Selecting template '$FIRST_SKILL'..."
RESPONSE=$(curl -s -X POST "$BASE_URL/ui/select" \
  -H "Content-Type: application/json" \
  -d "{\"target\":\"template\",\"value\":\"$FIRST_SKILL\"}")

if echo "$RESPONSE" | grep -q '"success": true'; then
  echo "✓ PASS: Selected template"
else
  echo "✗ FAIL: Failed to select template"
  echo "  Response: $RESPONSE"
  exit 1
fi
sleep 0.5
echo ""

# Step 6: Set EventHandlers
echo "Step 6: Setting EventHandlers..."
RESPONSE=$(curl -s -X POST "$BASE_URL/ui/set-complex-property" \
  -H "Content-Type: application/json" \
  -d '{"property":"EventHandlers","value":"[{\"_type\":\"TestHandler1\",\"TestField\":123},{\"_type\":\"TestHandler2\",\"AnotherField\":\"test\"}]"}')

if echo "$RESPONSE" | grep -q '"success": true'; then
  echo "✓ PASS: EventHandlers set successfully"
else
  echo "✗ FAIL: Failed to set EventHandlers"
  echo "  Response: $RESPONSE"
  exit 1
fi
sleep 0.5
echo ""

# Step 7: Verify EventHandlers persisted
echo "Step 7: Verifying EventHandlers persisted..."
RESPONSE=$(curl -s -X POST "$BASE_URL/ui/get-property" \
  -H "Content-Type: application/json" \
  -d '{"property":"EventHandlers"}')

if echo "$RESPONSE" | grep -q '"success": true'; then
  if echo "$RESPONSE" | grep -q 'TestHandler1'; then
    echo "✓ PASS: EventHandlers persisted correctly"
    echo "  Contains: TestHandler1"
  else
    echo "✗ FAIL: EventHandlers missing expected data"
    echo "  Response: $RESPONSE"
    exit 1
  fi
else
  echo "✗ FAIL: Failed to get EventHandlers"
  echo "  Response: $RESPONSE"
  exit 1
fi
echo ""

# Step 8: Modify EventHandlers
echo "Step 8: Modifying EventHandlers..."
RESPONSE=$(curl -s -X POST "$BASE_URL/ui/set-complex-property" \
  -H "Content-Type: application/json" \
  -d '{"property":"EventHandlers","value":"[{\"_type\":\"TestHandler1\",\"TestField\":999},{\"_type\":\"TestHandler2\",\"AnotherField\":\"modified\"}]"}')

if echo "$RESPONSE" | grep -q '"success": true'; then
  echo "✓ PASS: EventHandlers modified successfully"
else
  echo "✗ FAIL: Failed to modify EventHandlers"
  echo "  Response: $RESPONSE"
  exit 1
fi
sleep 0.5
echo ""

# Step 9: Verify modification persisted
echo "Step 9: Verifying modification persisted..."
RESPONSE=$(curl -s -X POST "$BASE_URL/ui/get-property" \
  -H "Content-Type: application/json" \
  -d '{"property":"EventHandlers"}')

if echo "$RESPONSE" | grep -q '"success": true'; then
  if echo "$RESPONSE" | grep -q '999' && echo "$RESPONSE" | grep -q 'modified'; then
    echo "✓ PASS: Modification persisted correctly"
    echo "  Contains: 999 and 'modified'"
  else
    echo "✗ FAIL: Modification not persisted"
    echo "  Response: $RESPONSE"
    exit 1
  fi
else
  echo "✗ FAIL: Failed to get EventHandlers after modification"
  echo "  Response: $RESPONSE"
  exit 1
fi
echo ""

# Step 10: Check logs
echo "Step 10: Checking logs for UpdateComplexArrayProperty calls..."
if [ -f "$LOGFILE" ]; then
  LOG_ENTRIES=$(tail -100 "$LOGFILE" | grep "UpdateComplexArrayProperty" | tail -5)
  if [ -n "$LOG_ENTRIES" ]; then
    echo "✓ PASS: Found UpdateComplexArrayProperty log entries:"
    echo "$LOG_ENTRIES" | sed 's/^/  /'
  else
    echo "⚠ WARNING: No UpdateComplexArrayProperty entries in recent logs"
  fi
else
  echo "⚠ WARNING: Log file not found at $LOGFILE"
fi
echo ""

echo "=== ALL TESTS PASSED ==="
echo ""
echo "Summary:"
echo "  ✓ Navigation works"
echo "  ✓ Template selection works"
echo "  ✓ EventHandlers can be set"
echo "  ✓ EventHandlers persist correctly"
echo "  ✓ EventHandlers can be modified"
echo "  ✓ Modifications persist"
echo ""
echo "The core persistence mechanism is working!"
echo "This confirms the fix to UpdateComplexArrayProperty."
