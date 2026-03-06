# SDK Testing Guide

A practical guide for testing and debugging the Menace SDK, including diagnostic commands, safe API usage, and troubleshooting workflows.

## Running Diagnostics (In-Game)

Open the dev console and run:

```bash
# Test all template types
debug.test_all_templates

# Check scene navigation
debug.scene_info
debug.list_scenes
debug.test_scene_load MainMenu

# Test SDK methods
debug.test_sdk_methods
debug.test_tilemap
debug.test_templates
```

Check results in `Logs/template_diagnostic.log`.

## Using New Safe APIs

### Template Property Access
```csharp
// ✅ Safe - handles IL2CPP casting
var damage = Templates.GetProperty<int>("WeaponTemplate", "Rifle", "Damage");
var name = Templates.GetProperty<string>("WeaponTemplate", "Rifle", "DisplayName");

// Get multiple properties at once
var props = Templates.GetProperties("WeaponTemplate", "Rifle",
    "Damage", "Range", "DisplayName");
```

### Mode-Aware Execution
```csharp
// ✅ Safe - validates mode before executing
var result = ModeAware.Execute(
    GameMode.Tactical,
    () => TileMap.GetMapInfo()?.Width.ToString(),
    "TileMap.GetMapInfo"
);
// Returns error message if wrong mode instead of crashing
```

### Querying Modpacks
```csharp
// ✅ Public API - no reflection needed
var loaded = Modpacks.IsModpackLoaded("MyMod");
var info = Modpacks.GetModpack("MyMod");
var all = Modpacks.GetAllModpacks();
```

## Console Commands

### Modpack Info
```bash
modpacks.list              # Show all loaded modpacks
modpacks.info MyMod        # Show details for specific modpack
modpacks.count             # Count loaded modpacks
```

### Template Diagnostics
```bash
debug.test_all_templates   # Test 26 representative template types (of 77 total)
debug.template_log         # Show recent loading attempts
debug.clear_template_log   # Clear log
```

### Scene Diagnostics
```bash
debug.scene_info          # Current scene details
debug.list_scenes         # All scenes in build
debug.test_scene_load MainMenu  # Test loading scene
debug.last_scene_attempt  # Last load attempt
```

### SDK Safety Tests
```bash
debug.test_tilemap        # Test TileMap methods
debug.test_templates      # Test Templates API
debug.test_sdk_methods    # Comprehensive test
```

## Build & Deploy

```bash
# Build (DLL auto-syncs to bundled directory)
dotnet build src/Menace.ModpackLoader/Menace.ModpackLoader.csproj -c Release

# Verify sync
ls -lh third_party/bundled/ModpackLoader/Menace.ModpackLoader.dll
```

## Applying Template Path Fixes

After running diagnostics, update:

**File:** `src/Menace.ModpackLoader/TemplateLoading/TemplateLoadingFixes.cs`

```csharp
private static readonly Dictionary<string, string> KnownPathFixes = new()
{
    // Add entries here if you discover types with broken paths via debug.test_all_templates
    // As of 2026-03-04: All 77 template types work correctly - no fixes needed!
};
```

Then rebuild and redeploy.

## Test Generation

```python
# Generate tests (uses new safe APIs)
await test_generate("MyMod", overwrite=True)

# Run tests
await test_run_modpack("MyMod")
```

## Related Documentation

- **[template-system.md](template-system.md)** - Template system architecture and troubleshooting
- **[12-auto-generated-tests.md](../../modding-guides/12-auto-generated-tests.md)** - Automatic test generation for modpacks
- **[test-harness.md](test-harness.md)** - Test harness command reference
- **[../../maintainers/sdk-issues-tracking.md](../../maintainers/sdk-issues-tracking.md)** - Known issues and implementation status

## Common Workflows

### Workflow 1: Diagnosing Template Loading Issues

1. Run comprehensive test:
   ```bash
   debug.test_all_templates
   ```

2. Check the generated report:
   ```bash
   cat Logs/template_diagnostic.log
   ```

3. Identify failures (shows which types have null paths or return 0 instances)

4. Add fixes to `TemplateLoadingFixes.KnownPathFixes`:
   ```csharp
   { "PerkTree", "Data/Perks/Trees" }
   ```

5. Rebuild and re-test

### Workflow 2: Testing Scene Navigation

1. Check current state:
   ```bash
   debug.scene_info
   ```

2. List available scenes:
   ```bash
   debug.list_scenes
   ```

3. Test scene load:
   ```bash
   debug.test_scene_load MainMenu
   ```

4. Check if scene actually changed:
   ```bash
   debug.scene_info
   ```

### Workflow 3: Validating SDK Method Safety

Run in each mode (MainMenu, Strategy, Tactical) to document requirements:

```bash
# In main menu
debug.test_sdk_methods

# Navigate to strategy
test.goto_strategy

# After scene loads
debug.test_sdk_methods

# Navigate to tactical (requires starting mission)
debug.test_sdk_methods
```

Compare results to identify which methods require which modes.
