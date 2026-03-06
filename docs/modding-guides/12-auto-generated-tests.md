# Auto-Generated Tests Per Modpack

## Overview

Tests are **automatically generated** from modpack contents. Every time you create or modify a modpack, the system can generate comprehensive tests that verify:
- ✅ Template patches are applied correctly
- ✅ Clones exist with correct properties
- ✅ Assets load successfully
- ✅ Lua scripts and commands work
- ✅ No errors are logged

**No manual test authoring required.**

## Quick Start

```python
# Generate tests for a modpack
await test_generate("MyMod")

# Run all generated tests
await test_run_modpack("MyMod")

# Result: All features verified automatically
```

## How It Works

### Test Generation

The system analyzes your `modpack.json` and generates test files:

```
Mods/MyMod/
├── modpack.json          # Your modpack definition
├── sources/
├── assets/
├── lua/
└── tests/                # Auto-generated
    ├── sanity.json       # Basic mod loading
    ├── templates.json    # Template patch verification
    ├── clones.json       # Clone verification
    ├── assets.json       # Asset loading
    └── lua.json          # Lua command testing
```

### Generation Rules

#### 1. Template Patches → Assert Tests

**Your modpack.json:**
```json
{
  "patches": {
    "EntityTemplate": {
      "enemy.pirate_grunt": {
        "MaxHealth": 200,
        "MoveSpeed": 5.5
      }
    }
  }
}
```

**Auto-generated tests/templates.json:**
```json
{
  "name": "MyMod - Template Patches",
  "steps": [
    {
      "type": "command",
      "name": "Navigate to main menu to load templates",
      "command": "test.goto_main"
    },
    {
      "type": "wait",
      "name": "Wait for scene load",
      "durationMs": 3000
    },
    {
      "type": "repl",
      "name": "Verify EntityTemplate templates load",
      "code": "Templates.FindAll('EntityTemplate').Length > 0"
    },
    {
      "type": "repl",
      "name": "Verify EntityTemplate.enemy.pirate_grunt exists",
      "code": "!Templates.Find('EntityTemplate', 'enemy.pirate_grunt').IsNull"
    },
    {
      "type": "assert",
      "name": "Verify EntityTemplate.enemy.pirate_grunt.MaxHealth",
      "expression": "Templates.GetProperty('EntityTemplate', 'enemy.pirate_grunt', 'MaxHealth')?.ToString()",
      "expected": "200"
    },
    {
      "type": "assert",
      "name": "Verify EntityTemplate.enemy.pirate_grunt.MoveSpeed",
      "expression": "Templates.GetProperty('EntityTemplate', 'enemy.pirate_grunt', 'MoveSpeed')?.ToString()",
      "expected": "5.5"
    }
  ]
}
```

**Note:** Tests use `Templates.GetProperty()` API instead of direct property access to handle IL2CPP type casting automatically.

#### 2. Clones → Existence + Property Tests

**Your modpack.json:**
```json
{
  "clones": {
    "EntityTemplate": {
      "enemy.super_grunt": {
        "source": "enemy.pirate_grunt",
        "properties": {
          "DisplayName": "Super Grunt",
          "MaxHealth": 300
        }
      }
    }
  }
}
```

**Auto-generated tests/clones.json:**
```json
{
  "name": "MyMod - Clones",
  "steps": [
    {
      "type": "repl",
      "name": "Verify clone EntityTemplate.enemy.super_grunt exists",
      "code": "!Templates.Find('EntityTemplate', 'enemy.super_grunt').IsNull"
    },
    {
      "type": "assert",
      "name": "Verify enemy.super_grunt.DisplayName",
      "expression": "Templates.GetProperty('EntityTemplate', 'enemy.super_grunt', 'DisplayName')?.ToString()",
      "expected": "Super Grunt"
    },
    {
      "type": "assert",
      "name": "Verify enemy.super_grunt.MaxHealth",
      "expression": "Templates.GetProperty('EntityTemplate', 'enemy.super_grunt', 'MaxHealth')?.ToString()",
      "expected": "300"
    }
  ]
}
```

#### 3. Assets → Load Tests

**Your assets/ directory:**
```
assets/
├── weapons/custom_rifle.png
├── models/custom_tank.glb
└── textures/custom_camo.png
```

**Auto-generated tests/assets.json:**
```json
{
  "name": "MyMod - Assets",
  "steps": [
    {
      "type": "repl",
      "name": "Verify asset weapons/custom_rifle.png loads",
      "code": "AssetManager.LoadAsset('MyMod/weapons/custom_rifle.png') != null"
    },
    {
      "type": "repl",
      "name": "Verify asset models/custom_tank.glb loads",
      "code": "AssetManager.LoadAsset('MyMod/models/custom_tank.glb') != null"
    },
    {
      "type": "repl",
      "name": "Verify asset textures/custom_camo.png loads",
      "code": "AssetManager.LoadAsset('MyMod/textures/custom_camo.png') != null"
    }
  ]
}
```

#### 4. Lua Scripts → Command Tests

**Your lua/commands.lua:**
```lua
function spawn_custom_enemy()
    EntitySpawner.Spawn("enemy.super_grunt", 10, 10)
    return "Spawned super grunt at (10, 10)"
end

DevConsole.RegisterCommand("mymod.spawn", "", "Spawn custom enemy", spawn_custom_enemy)

function get_mod_version()
    return "MyMod v1.0.0"
end

DevConsole.RegisterCommand("mymod.version", "", "Get mod version", get_mod_version)
```

**Auto-generated tests/lua.json:**
```json
{
  "name": "MyMod - Lua Scripts",
  "steps": [
    {
      "type": "command",
      "name": "Test Lua command: mymod.spawn",
      "command": "mymod.spawn"
    },
    {
      "type": "command",
      "name": "Test Lua command: mymod.version",
      "command": "mymod.version"
    },
    {
      "type": "assert",
      "name": "Verify Lua engine is initialized",
      "expression": "LuaScriptEngine.Instance.IsInitialized",
      "expected": "True"
    }
  ]
}
```

#### 5. Sanity Check → Always Generated

**Auto-generated tests/sanity.json:**
```json
{
  "name": "MyMod - Sanity Check",
  "steps": [
    {
      "type": "command",
      "name": "Check test harness status",
      "command": "test.status"
    },
    {
      "type": "repl",
      "name": "Verify modpack is loaded",
      "code": "Modpacks.IsModpackLoaded('MyMod')"
    },
    {
      "type": "command",
      "name": "Check for mod errors",
      "command": "errors"
    }
  ]
}
```

## Workflow Integration

### During Development

```python
# 1. Create modpack
await modpack_create("MyMod")

# 2. Add content
await source_write("MyMod", "Main.cs", code)
await template_set_field("EntityTemplate", "enemy.pirate_grunt", "MaxHealth", 200)

# 3. Generate tests automatically
await test_generate("MyMod")

# 4. Deploy and test
await deploy_modpack("MyMod")
await test_run_modpack("MyMod")

# Result: Instant verification
```

### Continuous Testing

```python
# After making changes
await source_write("MyMod", "Main.cs", updated_code)

# Regenerate tests (picks up changes)
await test_generate("MyMod", overwrite=True)

# Re-run all tests
await test_run_modpack("MyMod")

# Catches regressions immediately
```

### Pre-Release Verification

```python
# Before publishing mod
result = await test_run_modpack("MyMod", continueOnFailure=True)

data = json.loads(result)
if data["allPassed"]:
    print(f"✓ All {data['totalTests']} tests passed - ready to publish!")
else:
    print(f"✗ {data['testsFailed']} tests failed:")
    for test in data["results"]:
        if not test["passed"]:
            print(f"  - {test['test']}")
```

## Custom Tests

You can add custom tests alongside auto-generated ones:

```
tests/
├── sanity.json          # Auto-generated
├── templates.json       # Auto-generated
├── clones.json          # Auto-generated
├── assets.json          # Auto-generated
├── lua.json             # Auto-generated
└── custom_integration.json  # Your custom test
```

Custom test example:
```json
{
  "name": "MyMod - Custom Integration Test",
  "steps": [
    {
      "type": "command",
      "name": "Start tactical mission",
      "command": "test.start_mission 12345 1"
    },
    {
      "type": "wait",
      "name": "Wait for mission load",
      "durationMs": 10000
    },
    {
      "type": "command",
      "name": "Spawn custom enemy",
      "command": "mymod.spawn"
    },
    {
      "type": "assert",
      "name": "Verify enemy spawned",
      "expression": "EntitySpawner.FindEntity('enemy.super_grunt') != null",
      "expected": "True"
    }
  ]
}
```

## Benefits

### For Developers
- ✅ **Zero test authoring** - Tests generated automatically
- ✅ **Instant verification** - Run tests after every change
- ✅ **Regression prevention** - Catches breaks immediately
- ✅ **Confidence** - Know your mod works before publishing

### For Users
- ✅ **Quality assurance** - Mods are tested before release
- ✅ **Fewer bugs** - Issues caught during development
- ✅ **Reliable mods** - Verified to work as intended

### For You
- ✅ **No manual testing** - Agent runs tests automatically
- ✅ **Less debugging** - Tests catch issues early
- ✅ **Faster iteration** - Deploy → test → fix cycle is seconds

## Advanced Usage

### Selective Test Generation

```python
# Only regenerate template tests
await test_generate("MyMod", overwrite=True)
# Then manually delete tests you don't want regenerated
```

### Test First Development

```python
# 1. Write a custom test for new feature
# 2. Run test (it fails - feature doesn't exist)
# 3. Implement feature
# 4. Run test (it passes!)
# 5. Generate tests for other aspects
```

### CI/CD Integration

```bash
# In GitHub Actions / CI pipeline
claude-code test_run_modpack("MyMod") || exit 1
```

## Example: Complete Modpack Test Coverage

**Modpack with everything:**
```
Mods/CompleteMod/
├── modpack.json (patches, clones, lua commands)
├── sources/Main.cs (Harmony patches)
├── assets/custom_texture.png
├── lua/commands.lua (console commands)
└── tests/
    ├── sanity.json (3 tests - mod loads, no errors)
    ├── templates.json (5 tests - all patches verified)
    ├── clones.json (8 tests - clones exist + properties)
    ├── assets.json (1 test - texture loads)
    └── lua.json (3 tests - commands work + engine init)
```

**Total: 20 auto-generated tests**

**Running all tests:**
```python
result = await test_run_modpack("CompleteMod")

# Output:
{
  "success": true,
  "allPassed": true,
  "totalTests": 5,  # 5 test files
  "testsRun": 5,
  "testsPassed": 5,
  "testsFailed": 0
}
```

## Recent Improvements (January 2025)

### ✅ Safe Template Property Access
- **Problem:** Direct property access (`template.Damage`) crashed due to IL2CPP casting requirements
- **Solution:** Added `Templates.GetProperty()` API that handles casting internally
- **Impact:** All generated tests now use safe property access

### ✅ Template Type Validation
- **Added:** Tests now verify template types load before testing instances
- **Benefit:** Clearer error messages when template types don't load

### ✅ Modpack Query API
- **Added:** `Modpacks.IsModpackLoaded()` SDK for checking mod status
- **Updated:** Sanity tests use new API instead of reflection

### ✅ Comprehensive Diagnostics
- **Added:** `debug.test_all_templates` command for discovering template loading issues
- **Added:** `debug.test_sdk_methods` for validating SDK safety
- **Benefit:** Can identify and fix broken template types before testing

## Future Enhancements

- [ ] Test generation on file save (auto-regenerate when modpack changes)
- [ ] Performance tests (measure load times, FPS impact)
- [ ] Compatibility tests (test with other mods)
- [ ] Visual regression tests (compare screenshots)
- [ ] Coverage reports (% of mod features tested)
- [ ] Mode-aware tests (tactical vs strategy context validation)

## API Reference

### `test_generate(modpack, overwrite=False)`

Generate test files from modpack contents.

**Parameters:**
- `modpack` - Modpack name
- `overwrite` - Overwrite existing test files (default: False)

**Returns:**
```json
{
  "success": true,
  "modpack": "MyMod",
  "testsGenerated": 5,
  "tests": ["sanity.json", "templates.json", "clones.json", "assets.json", "lua.json"],
  "testsDirectory": "Mods/MyMod/tests"
}
```

### `test_run_modpack(modpack, autoLaunch=True, continueOnFailure=False)`

Run all tests for a modpack.

**Parameters:**
- `modpack` - Modpack name
- `autoLaunch` - Auto-launch game if not running (default: True)
- `continueOnFailure` - Continue running tests after failure (default: False)

**Returns:**
```json
{
  "success": true,
  "modpack": "MyMod",
  "allPassed": true,
  "totalTests": 5,
  "testsRun": 5,
  "testsPassed": 5,
  "testsFailed": 0,
  "results": [...]
}
```

## Conclusion

**You never need to manually test a mod again.**

When you ask me to implement a feature:
1. I create/modify the modpack
2. I run `test_generate` to create tests
3. I run `test_run_modpack` to verify
4. I give you results showing it works

**Zero manual testing. Maximum confidence.**
