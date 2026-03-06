# Automated Test Harness

## Overview

The Menace Modkit includes a comprehensive automated testing infrastructure that enables AI agents (and humans) to:
- Create and deploy mods programmatically
- Launch the game and control its state
- Execute console commands and verify results
- Run automated test suites
- Capture results and screenshots

This eliminates the need for manual testing of every feature and parameter.

## Architecture

```
Test Agent (Claude via MCP)
    ↓
Modkit MCP Server (test_run tool)
    ↓
Game MCP HTTP Server (test.* commands)
    ↓
Game Runtime (ModpackLoader + TestHarness)
```

## Components

### 1. In-Game Test Commands (GameMcpServer)

Available via console or `/cmd` endpoint:

| Command | Description | Example |
|---------|-------------|---------|
| `test.status` | Get test harness status | `test.status` |
| `test.scene [name]` | Get/set current scene | `test.scene TacticalCombat` |
| `test.goto_main` | Navigate to main menu | `test.goto_main` |
| `test.goto_strategy` | Navigate to strategy map | `test.goto_strategy` |
| `test.start_mission <seed> <diff> [template]` | Queue test mission | `test.start_mission 12345 1` |
| `test.wait <cond\|ms> [timeout]` | Wait for condition or duration | `test.wait mission_ready 30000` |
| `test.wait_result` | Get last wait result | `test.wait_result` |
| `test.assert <expr> <expected>` | Assert expression equals value | `test.assert mission.Seed 12345` |
| `test.assert_contains <expr> <substring>` | Assert expression contains substring | `test.assert_contains logs SEED_OVERRIDE` |
| `test.screenshot <filename>` | Capture screenshot | `test.screenshot test1.png` |
| `test.eval <expression>` | Evaluate C# expression | `test.eval GameState.CurrentScene` |
| `test.inspect <path>` | Inspect game value | `test.inspect mission.Seed` |

**Wait conditions:**
- `scene_ready` - Scene is loaded
- `tactical_ready` - In tactical scene
- `mission_ready` - Mission is running
- `strategy_ready` - In strategy map
- `<number>` - Wait N milliseconds

### 2. MCP Tools (Modkit.Mcp)

Available to AI agents:

| Tool | Description |
|------|-------------|
| `game_launch` | Launch game and wait for ready |
| `game_cmd` | Execute console command |
| `game_status` | Check game status |
| `game_repl` | Execute C# code in-game |
| `test_run` | Run automated test suite |

### 3. Test Runner (`test_run`)

Orchestrates full test workflows:

```python
# Agent usage
result = await test_run(
    test="tests/my_test.json",     # Test spec file
    modpack="MyTestMod",            # Optional modpack to deploy
    autoLaunch=True,                # Auto-launch game if not running
    timeout=30                      # Timeout for operations
)
```

## Test Specification Format

Tests are defined in JSON files:

```json
{
  "name": "Test Name",
  "modpack": "ModpackName",           // Optional
  "continueOnFailure": false,         // Stop on first failure (default)
  "steps": [
    {
      "type": "command",
      "name": "Step description",
      "command": "test.start_mission 12345 1"
    },
    {
      "type": "wait",
      "name": "Wait for load",
      "durationMs": 5000
    },
    {
      "type": "assert",
      "name": "Verify seed",
      "expression": "TacticalController.GetMission().Seed",
      "expected": "12345"
    }
  ]
}
```

### Step Types

#### `command` - Execute console command
```json
{
  "type": "command",
  "name": "Start mission",
  "command": "test.start_mission 99999 1"
}
```

#### `assert` - Assert exact value
```json
{
  "type": "assert",
  "name": "Check seed",
  "expression": "mission.Seed",
  "expected": "99999"
}
```

#### `assert_contains` - Assert substring
```json
{
  "type": "assert_contains",
  "name": "Check logs",
  "expression": "File.ReadAllText(\"Latest.log\")",
  "expected": "SEED_OVERRIDE"
}
```

#### `wait` - Wait duration
```json
{
  "type": "wait",
  "name": "Wait for scene",
  "durationMs": 5000
}
```

#### `screenshot` - Capture screenshot
```json
{
  "type": "screenshot",
  "name": "Capture state",
  "filename": "test_result.png"
}
```

#### `repl` / `eval` - Execute C# code
```json
{
  "type": "repl",
  "name": "Get current scene",
  "code": "GameState.CurrentScene"
}
```

## Example Workflows

### Manual Testing (via MCP)

```python
# 1. Create test modpack
await modpack_create("TestMod")
await source_write("TestMod", "Main.cs", patch_code)

# 2. Deploy
await deploy_modpack("TestMod")

# 3. Launch game
await game_launch(timeout=60)

# 4. Execute test commands
await game_cmd("test.start_mission 12345 1")
await game_cmd("test.wait mission_ready 30000")
result = await game_cmd("test.assert mission.Seed 12345")

# 5. Check result
if "PASSED" in result:
    print("✓ Test passed!")
```

### Automated Test Suite

```python
# Run complete test from spec file
result = await test_run(
    test="tests/seed_override_test.json",
    modpack="SeedOverrideTest",
    autoLaunch=True
)

# Parse results
import json
data = json.loads(result)
if data["passed"]:
    print(f"✓ All {data['totalSteps']} steps passed!")
else:
    print(f"✗ Test failed:")
    for step in data["steps"]:
        if step["status"] == "fail":
            print(f"  - {step['name']}: {step.get('error')}")
```

## Writing Tests

### Test Template

```json
{
  "name": "Feature X Test",
  "modpack": "FeatureXTest",
  "steps": [
    {
      "type": "command",
      "name": "Setup - Navigate to test area",
      "command": "test.goto_strategy"
    },
    {
      "type": "wait",
      "name": "Wait for scene load",
      "durationMs": 3000
    },
    {
      "type": "assert",
      "name": "Verify feature X is enabled",
      "expression": "FeatureX.IsEnabled",
      "expected": "True"
    },
    {
      "type": "repl",
      "name": "Trigger feature X",
      "code": "FeatureX.DoSomething()"
    },
    {
      "type": "assert",
      "name": "Verify result",
      "expression": "FeatureX.LastResult",
      "expected": "Success"
    }
  ]
}
```

### Best Practices

1. **Start simple** - Test basic functionality first
2. **Use wait steps** - Give game time to load/process
3. **Assert incrementally** - Verify state after each operation
4. **Capture screenshots** - Visual verification of final state
5. **Name steps clearly** - Makes debugging easier
6. **Use continueOnFailure** sparingly - Usually want to stop on first failure

## Debugging Tests

### Check test harness status
```python
result = await game_cmd("test.status")
```

### Inspect game state
```python
result = await game_cmd("test.inspect TacticalController.GetMission().Seed")
```

### Check logs
```python
logs = await game_logs(lines=100, filter="ERROR")
```

### Use REPL for interactive debugging
```python
result = await game_repl("TacticalController.GetMission()?.Seed")
```

## Integration with Custom Maps

When custom maps feature is implemented, test harness enables:

```json
{
  "name": "Map Size Override Test",
  "modpack": "MapSizeTest",
  "steps": [
    {
      "type": "command",
      "name": "Start mission with custom size",
      "command": "test.start_mission 12345 1 custom_large_map"
    },
    {
      "type": "wait",
      "name": "Wait for map generation",
      "durationMs": 10000
    },
    {
      "type": "assert",
      "name": "Verify map size is 60x60",
      "expression": "TileMap.GetMapInfo().Width",
      "expected": "60"
    },
    {
      "type": "assert",
      "name": "Verify height matches",
      "expression": "TileMap.GetMapInfo().Height",
      "expected": "60"
    },
    {
      "type": "repl",
      "name": "Check generator parameters",
      "code": "TacticalController.GetMission().Generators[0].spawnDensity"
    }
  ]
}
```

## Troubleshooting

### Game not responding
- Check `test.status` to verify test harness is loaded
- Verify MCP server is enabled in settings
- Check game logs for errors

### Assertions failing
- Use `test.inspect` to see actual value
- Check expression syntax (C# code)
- Verify game state is ready (use wait steps)

### Tests timing out
- Increase timeout parameter
- Add wait steps between operations
- Check if game is frozen (logs, UI)

## Future Enhancements

- [ ] Visual test recorder (record manual actions as test spec)
- [ ] Test coverage reporting
- [ ] Parallel test execution
- [ ] Test result database/history
- [ ] Integration with CI/CD
- [ ] Performance benchmarking

## Examples

See `tests/` directory for example test specs:
- `example_simple_sanity.json` - Basic SDK verification
- `example_seed_override.json` - Full feature test with modpack

## Reference

- **Test Commands**: `src/Menace.ModpackLoader/TestHarnessCommands.cs`
- **MCP Tools**: `src/Menace.Modkit.Mcp/Tools/TestTools.cs`, `GameTools.cs`
- **Game Server**: `src/Menace.ModpackLoader/Mcp/GameMcpServer.cs`
