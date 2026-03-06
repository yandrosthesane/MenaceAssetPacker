# Bootstrap Test Runner

## Purpose

This test validates the entire test infrastructure and discovers what APIs actually exist in the game.

## How to Run

### Option 1: Via MCP (Recommended)

```python
# In Claude Desktop or MCP client:
result = await test_run(
    test="tests/bootstrap.json",
    autoLaunch=True
)

# Parse and display results
import json
data = json.loads(result)

print(f"Test Results: {data['passed']}")
print(f"Steps: {len(data['steps'])}")

for step in data['steps']:
    status = "✓" if step['status'] == 'pass' else "✗"
    print(f"{status} {step['step']}")
```

### Option 2: Manual (if MCP not working)

1. **Launch the game**
2. **Open console** (~)
3. **Run each test command manually:**
   ```
   test.status
   test.scene
   test.eval 1 + 1
   test.goto_main
   ... etc
   ```

## What It Tests

### Phase 1: Basic Infrastructure (Tests 1-10)
- ✓ Test harness loaded
- ✓ REPL works
- ✓ SDK accessible
- ✓ Assertions work
- ✓ MCP server running

### Phase 2: Navigation (Tests 11-14)
- ✓ Wait command works
- ✓ Scene navigation works
- ✓ Scene changes detected

### Phase 3: API Discovery (Tests 15-23)
**This is the critical part - discovers what APIs exist:**
- Templates.Find API
- Templates.FindAll API
- AssetManager existence
- TileMap methods
- TacticalController methods

### Phase 4: Commands (Tests 24-28)
- ✓ test.inspect works
- ✓ test.eval works
- ✓ test.screenshot works
- ✓ test.assert_contains works

### Phase 5: Modpack Integration (Tests 29-30)
- Modpack detection
- Template counting

## Expected Output

```json
{
  "test": "Bootstrap Test - Validate Test Infrastructure",
  "passed": true/false,
  "totalSteps": 30,
  "steps": [
    {
      "step": "Test 1: Check test harness is loaded",
      "type": "command",
      "status": "pass",
      "result": "..."
    },
    ...
  ]
}
```

## What to Look For

### If Test 1-10 Fail
**Problem:** Basic infrastructure not working
**Fix:** Check if:
- ModpackLoader is installed
- TestHarnessCommands registered
- REPL (Roslyn) loaded correctly

### If Test 15-20 Fail
**Problem:** Templates API doesn't work as expected
**Fix:**
- Check actual Templates API signature
- Adjust auto-generated test templates
- Update test generation code

### If Test 21 Returns "NOT_FOUND"
**Problem:** No AssetManager in SDK
**Fix:**
- Find actual asset loading API
- Update auto-generated asset tests
- Or skip asset tests in generation

### If Test 27 Fails
**Problem:** Screenshot API broken
**Fix:**
- Check screenshot directory permissions
- Verify ScreenCapture API works

## After Running

### Success Case (All Pass)
```
✓ Infrastructure is solid
✓ Can proceed with auto-generated tests
✓ Template access works
✓ Asset loading works
✓ All commands functional
```

→ **Next step:** Run `test_generate` on a real modpack

### Partial Success
```
✓ Basic infrastructure works
✗ Some API discovery failed
```

→ **Next step:** Fix the failed APIs, update test generation

### Failure
```
✗ Basic commands don't work
```

→ **Next step:** Debug test harness installation

## Interpreting Results

### Test 15-17: Templates API
```json
{
  "step": "Test 17: Try finding a template",
  "code": "Templates.Find('EntityTemplate', 'enemy.pirate_grunt')",
  "result": "Il2CppSystem.Object (0x...)" // ✓ GOOD - template found
}
```
OR
```json
{
  "result": "null" // ✗ BAD - template not found or API wrong
}
```

### Test 21: Asset API Discovery
```json
{
  "result": "Menace.AssetBundles.AssetManager" // ✓ GOOD - found it
}
```
OR
```json
{
  "result": "NOT_FOUND" // ✗ Need to find correct API
}
```

## Recovery Actions

### If Templates API Fails
```python
# Manually discover the API
await game_repl("typeof(Menace.SDK.Templates).GetMethods().Select(m => m.Name + '(' + string.Join(',', m.GetParameters().Select(p => p.ParameterType.Name)) + ')').ToArray()")

# This shows actual method signatures
# Update TestGenerationTools.cs to match
```

### If Assert Formatting Wrong
```python
# Check actual return format
await game_repl("var t = Templates.Find('EntityTemplate', 'enemy.pirate_grunt'); t.MaxHealth")

# See if it returns "200" or "200.0" or "System.Int32: 200"
# Adjust assertion comparison logic
```

## Next Steps After Bootstrap

### If All Pass (Optimistic)
1. Create a simple test modpack
2. Generate tests with `test_generate`
3. Run tests with `test_run_modpack`
4. Verify auto-generation works

### If Some Fail (Realistic)
1. Document which APIs work
2. Fix TestGenerationTools to use working APIs
3. Re-run bootstrap
4. Iterate until stable

### If Most Fail (Pessimistic)
1. Start with manual tests only
2. Build up infrastructure piece by piece
3. Add auto-generation later

## Bootstrap Test Output Schema

The test will produce a JSON report. Save it for analysis:

```python
result = await test_run("tests/bootstrap.json", autoLaunch=True)

# Save to file
with open("bootstrap_results.json", "w") as f:
    f.write(result)

# Analyze
import json
data = json.loads(result)

# Group by status
passed = [s for s in data['steps'] if s['status'] == 'pass']
failed = [s for s in data['steps'] if s['status'] == 'fail']

print(f"Passed: {len(passed)}/{len(data['steps'])}")
print(f"Failed: {len(failed)}/{len(data['steps'])}")

# Show failures
for step in failed:
    print(f"FAILED: {step['step']}")
    if 'error' in step:
        print(f"  Error: {step['error']}")
    if 'result' in step:
        print(f"  Result: {step['result']}")
```

## Success Criteria

**Minimum for "Infrastructure Works":**
- Tests 1-10 pass (basic commands)
- Tests 12-14 pass (navigation)
- Tests 25-28 pass (utility commands)

**Minimum for "Auto-Generation Works":**
- Tests 15-20 pass (Templates API)
- Test 21 finds asset API
- Tests 29-30 pass (modpack detection)

**Full Success:**
- All 30 tests pass
- Can proceed with confidence

## Timeline

**Phase 1:** Run bootstrap test (5 minutes)
**Phase 2:** Analyze results (10 minutes)
**Phase 3:** Fix broken parts (30 minutes - 2 hours depending on issues)
**Phase 4:** Re-run bootstrap (5 minutes)
**Phase 5:** Proceed to real tests (if passing)

## Ready?

Run this command:

```python
result = await test_run("tests/bootstrap.json", autoLaunch=True)
```

Then share the output and we'll know exactly what needs fixing.
