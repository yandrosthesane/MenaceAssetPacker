# UI Control Tree Inspection Guide

## Overview

The Menace Modkit now has a comprehensive UI inspection system that allows programmatic testing and debugging of the UI without manual interaction.

## HTTP Endpoint

### GET `/ui/controls`

Returns the complete hierarchical control tree of the current UI.

**Example:**
```bash
curl http://127.0.0.1:21421/ui/controls | jq '.'
```

**Response Structure:**
```json
{
  "timestamp": "2026-03-05T00:10:27Z",
  "tree": {
    "type": "MainWindow",
    "name": null,
    "isVisible": true,
    "isEnabled": true,
    "depth": 0,
    "children": [
      {
        "type": "Grid",
        "isVisible": true,
        "isEnabled": true,
        "depth": 1,
        "children": [...]
      }
    ]
  }
}
```

## Control Node Properties

Each control node includes:

- **type**: Control type name (e.g., "Button", "TextBlock", "ComboBox")
- **name**: Control name (if set)
- **isVisible**: Whether the control is currently visible
- **isEnabled**: Whether the control is currently enabled
- **depth**: Nesting depth in the visual tree

### Type-Specific Properties

**TextBlock:**
- `text`: The displayed text
- `fontSize`: Font size
- `fontWeight`: Font weight (e.g., "DemiBold")

**Button:**
- `content`: Button content (usually text)

**TextBox:**
- `text`: Current text value
- `watermark`: Placeholder text

**ComboBox:**
- `selectedItem`: Currently selected item
- `itemCount`: Number of items in dropdown

**CheckBox:**
- `content`: Label text
- `isChecked`: true/false/null (for tri-state)

**ItemsControl:**
- `itemCount`: Number of items

## MCP Tool Usage

Once the MCP server is restarted, you can use the `modkit_inspect_controls` tool:

```typescript
// In Claude Code MCP client
const result = await mcp.callTool("modkit_inspect_controls");
console.log(result);
```

## Use Cases

### 1. Verify Button Exists
```bash
curl -s http://127.0.0.1:21421/ui/controls | \
  jq '.. | objects | select(.type == "Button" and (.content // "" | contains("EventHandlers")))'
```

### 2. Find All Visible TextBlocks
```bash
curl -s http://127.0.0.1:21421/ui/controls | \
  jq -r '.. | objects | select(.type == "TextBlock" and .isVisible == true) | .text' | \
  grep -v null
```

### 3. Check Field Controls in Stats Editor
```bash
# Navigate to Stats Editor
curl -s -X POST http://127.0.0.1:21421/ui/click \
  -H "Content-Type: application/json" \
  -d '{"button": "data"}'

# Select a template
curl -s -X POST http://127.0.0.1:21421/ui/select \
  -H "Content-Type: application/json" \
  -d '{"target": "template", "value": "some_skill_name"}'

# Inspect controls
curl -s http://127.0.0.1:21421/ui/controls > /tmp/stats_editor_controls.json

# Find EventHandlers button
jq '.. | objects | select(.content and (.content | contains("EventHandlers")))' \
  /tmp/stats_editor_controls.json
```

### 4. Trace Control Hierarchy
```bash
# Save control tree
curl -s http://127.0.0.1:21421/ui/controls > /tmp/control_tree.json

# Find path to a specific control
jq -r '.. | objects | select(.content == "Edit EventHandlers...") | {type, depth, content}' \
  /tmp/control_tree.json
```

## Next Steps for EventHandlers Debugging

To diagnose why the EventHandlers button doesn't appear:

1. Navigate to Stats Editor and select a template with EventHandlers:
   ```bash
   curl -X POST http://127.0.0.1:21421/ui/click \
     -H "Content-Type: application/json" \
     -d '{"button": "data"}'
   ```

2. Get the control tree:
   ```bash
   curl -s http://127.0.0.1:21421/ui/controls > /tmp/tree.json
   ```

3. Search for "EventHandlers" in any form:
   ```bash
   jq -r '.. | strings | select(contains("EventHandlers") or contains("eventhandlers"))' /tmp/tree.json
   ```

4. Check if the field is being rendered at all:
   ```bash
   # Look for field labels/names in the tree
   jq -r '.. | objects | select(.type == "TextBlock") | .text' /tmp/tree.json | grep -i event
   ```

5. Compare vanilla array rendering vs special EventHandlers rendering:
   - Find other array fields being rendered
   - Check their control structure
   - See if EventHandlers follows the same pattern

## Benefits

- **No Manual Testing**: Query UI state programmatically
- **Automated Verification**: Write test scripts to verify UI behavior
- **Debugging**: Inspect actual rendered controls vs expected
- **CI/CD**: Integrate UI testing into build pipeline
- **Documentation**: Generate UI screenshots/structure automatically

## Implementation Details

- **UIStateService.cs**: `GetControlTree()` method builds hierarchical tree
- **UIHttpServer.cs**: `/ui/controls` endpoint exposes tree via HTTP
- **UITools.cs**: `modkit_inspect_controls` MCP tool for Claude Code integration
- **Update Frequency**: On-demand (HTTP GET), not periodic like `/ui/state`
- **Performance**: Fast recursive traversal with max depth limit (20 levels)
