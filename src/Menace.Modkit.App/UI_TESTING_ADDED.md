# UI Testing Capability Added

## Summary

Added HTTP API endpoints and test infrastructure to automate testing of the modkit UI, specifically for testing EventHandler editing functionality.

## Changes Made

### 1. UIHttpServer.cs - New Endpoints

Added two new HTTP endpoints:

#### `/ui/get-property` (POST)
Gets the current value of a property from the selected template.

**Request:**
```json
{
  "property": "EventHandlers"
}
```

**Response:**
```json
{
  "success": true,
  "property": "EventHandlers",
  "value": "[{...}]",
  "source": "modified"  // or "vanilla"
}
```

#### `/ui/set-complex-property` (POST)
Sets a complex property (like EventHandlers) directly without needing to open the dialog.

**Request:**
```json
{
  "property": "EventHandlers",
  "value": "[{\"_type\":\"AttackEventHandler\",\"Damage\":100}]"
}
```

**Response:**
```json
{
  "success": true,
  "property": "EventHandlers",
  "value": "[{\"_type\":\"AttackEventHandler\",\"Damage\":100}]"
}
```

### 2. TestTools.cs - New Test Step Types

Added 5 new test step types for UI automation:

#### `ui_navigate`
Navigate to a section/subsection of the modkit.

```json
{
  "type": "ui_navigate",
  "name": "Navigate to Data section",
  "section": "moddingtools",
  "subSection": "data"
}
```

#### `ui_select`
Select a modpack, template type, or template.

```json
{
  "type": "ui_select",
  "name": "Select SkillTemplate",
  "target": "templatetype",
  "value": "SkillTemplate"
}
```

Supported targets:
- `modpack` - Select a modpack
- `templatetype` - Select a template type category
- `template` - Select a specific template

#### `ui_set_field`
Set a simple field value.

```json
{
  "type": "ui_set_field",
  "name": "Set Name field",
  "field": "Name",
  "value": "New Name"
}
```

#### `ui_get_property`
Get a property value and optionally check if it contains an expected value.

```json
{
  "type": "ui_get_property",
  "name": "Verify EventHandlers persisted",
  "property": "EventHandlers",
  "expected": "AttackEventHandler"
}
```

#### `ui_set_complex_property`
Set a complex property with JSON value.

```json
{
  "type": "ui_set_complex_property",
  "name": "Set EventHandlers",
  "property": "EventHandlers",
  "value": "[{\"_type\":\"AttackEventHandler\",\"Damage\":100}]"
}
```

### 3. Test File Created

**File:** `tests/eventhandler_persistence.json`

This test verifies that EventHandlers can be:
1. Set directly via API
2. Retrieved and verified
3. Modified and re-verified

The test doesn't exercise the dialog UI itself (since dialogs are modal and block), but it verifies the underlying data persistence mechanism works correctly.

## How to Run the Test

### Option 1: Via MCP Tool (from Claude Code)
```
Use the test_run tool with:
test = "tests/eventhandler_persistence.json"
```

### Option 2: Via HTTP API (manual)
1. Start the modkit (the UIHttpServer runs on port 21421)
2. Send HTTP POST requests to the endpoints as shown in the test JSON
3. Verify responses contain `"success": true`

### Option 3: Via REPL (if we add it)
```
test.run eventhandler_persistence
```

## Test Results Format

When the test runs, it returns JSON with:
```json
{
  "test": "EventHandler Persistence Test",
  "passed": true,
  "totalSteps": 13,
  "steps": [
    {
      "step": "Navigate to Data section",
      "type": "ui_navigate",
      "status": "pass",
      "section": "moddingtools",
      "subSection": "data",
      "result": "{...}"
    },
    ...
  ]
}
```

## What This Tests

✅ Setting complex properties directly via API
✅ Property persistence after being set
✅ Property modification and re-verification
✅ UI state management (navigation, selection)

❌ Does NOT test: Dialog UI interactions (opening, clicking buttons, selecting from dropdowns)

## Why Dialog UI Testing Is Separate

The EventHandlerEditorDialog is modal and blocks the UI thread until closed. HTTP endpoints can't interact with modal dialogs while they're open because:
1. The HTTP handler runs on the UI thread
2. `ShowDialog()` blocks the UI thread
3. No HTTP requests can be processed while blocked

To test the actual dialog UI, we would need:
- Avalonia's UI testing framework
- Or a separate test mode that runs the dialog in a non-modal way
- Or automated UI testing tools that can interact with native windows

## Build Status

✅ Compiled successfully (0 errors, 0 warnings)

## Files Modified

1. `src/Menace.Modkit.App/Services/UIHttpServer.cs` - Added endpoints
2. `src/Menace.Modkit.Mcp/Tools/TestTools.cs` - Added test step types
3. `tests/eventhandler_persistence.json` - New test file
