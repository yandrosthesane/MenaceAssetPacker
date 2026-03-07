# EventHandler Field Types - Improvements Summary

**Date:** 2026-03-06
**Status:** Schema extraction improved with name-based reference inference

---

## What Was Fixed

### 1. Proper Type Classification from IL2CPP Dump

**Previous Issue:** `extract_eventhandlers.py` used simple heuristics to guess field types:
- Checked if type ends with "Type", "Kind", or "Mode" to identify enums
- Marked anything with "Template" in name as reference
- Everything else defaulted to "primitive"

**Fix:** Updated script to use actual type definitions from schema.json:
- Loads known enums, structs, and templates from existing schema
- Uses same `classify_field` logic as `generate_schema.py`
- Properly categorizes all field types based on C# definitions

**Result:**
- Enum fields correctly identified (66 → 58, as some were actually ID structs)
- Reference fields increased (42 → 50)
- Interface types now properly categorized (19 fields)

### 2. Name-Based Reference Inference

**Problem:** Many fields use generic types (`String`, `ID`, `int`) but semantically represent references to other templates.

**Examples:**
- `SoundToPlay` (type `ID`) → Sound reference
- Fields with "skill" in name but type `String` → SkillTemplate reference
- Fields with "tospawn" in name → Entity/Effect template references

**Fix:** Added `infer_reference_from_name()` function that detects reference fields based on naming patterns:

| Pattern | Inferred Type | Confidence |
|---------|---------------|------------|
| "sound", "audio" | SoundReference | 90% |
| "tospawn" + "entity" | EntityTemplate | 90% |
| "tospawn" + "tile" | TileEffectTemplate | 90% |
| "skill", "ability" | SkillTemplate | 80% |
| "perk" | PerkTemplate | 80% |
| "item" + spawn keywords | ItemTemplate | 80% |
| "tag" + String/ID | TagTemplate | 70% |
| "animation", "anim" | AnimationTemplate | 70% |

**Result:** Sound fields like `SoundToPlay`, `SoundWhenSuppressed`, `SoundWhenPinnedDown` now correctly marked as references with `element_type: "SoundReference"`.

### 3. ID Type Handling

**Problem:** `ID` exists as both an enum and a struct in the IL2CPP dump:
- `enum ID` - Empty enum, rarely used
- `struct ID` - FMOD sound bank identifier (bankId, itemId)

Fields typed as `ID` were being categorized as "enum" incorrectly.

**Fix:** Check for `ID` struct BEFORE checking enums, and apply name-based inference to determine if it's a sound reference.

**Result:** Fields like `SoundToPlay` now show:
```json
{
  "name": "SoundToPlay",
  "type": "ID",
  "category": "reference",
  "element_type": "SoundReference"
}
```

### 4. Description Field Added

**Added:** Every field now has a `"description": ""` field that can be filled in manually over time to document what each field does.

**Purpose:** As we learn what each EventHandler field does, we can add descriptions to help modders understand them.

---

## Current Field Distribution

After improvements:

| Category | Count | Percentage | Description |
|----------|-------|------------|-------------|
| primitive | 215 | 56.4% | Ints, floats, bools, strings |
| enum | 58 | 15.2% | Enum types from game code |
| reference | 50 | 13.1% | References to templates/assets |
| collection | 22 | 5.8% | Arrays and Lists |
| interface | 19 | 5.0% | Interface types (ITacticalCondition, IItemFilter, etc.) |
| unity_asset | 13 | 3.4% | Unity asset references (Sprite, GameObject, etc.) |
| struct | 4 | 1.0% | Struct types (Vector3, etc.) |

---

## What Still Needs Work

### 1. Remaining Primitive Fields

**Current:** 215 fields (56.4%) are still categorized as "primitive"

**Many are legitimately primitive:**
- Damage values (float)
- Duration values (int)
- Percentages (float)
- Boolean flags

**But some may be references that naming patterns didn't catch.**

**Action Needed:** Manual review of primitive String/ID fields to identify additional reference patterns.

### 2. Interface Type Handling in UI

**Current State:** 19 fields typed as interfaces:
- `ITacticalCondition` (conditions for when effects apply)
- `IItemFilter` (filters for which items are affected)
- `ISkillFilter` (filters for which skills are affected)
- `IValueProvider` (dynamic value calculation)

**Problem:** Interface fields can't show a dropdown of template instances since they're not concrete types.

**Solution Options:**
1. Allow JSON editing for interface fields
2. Show a list of implementing classes
3. Mark as "advanced" and require manual editing

**Status:** UI currently treats them as reference fields, which may not work correctly.

### 3. Description Content

**Current:** All descriptions are empty strings `""`

**Needed:** Populate descriptions for commonly-used EventHandler fields.

**Approach:**
1. Focus on top 10 most common EventHandler types (Attack, ChangeProperty, AddSkill, etc.)
2. Document fields for each one
3. Use actual game data to understand what each field does

### 4. Element Type for Collections

**Current:** Arrays like `TargetRequiresOneOfTheseTags` correctly show `element_type: "TagTemplate"`

**Needed:** Verify all collection fields have correct element_type values.

**Action:** Check collections that don't have element_type set.

---

## How to Use

### Regenerate EventHandler Schema

After updating IL2CPP dump or making changes to field classification logic:

```bash
python3 extract_eventhandlers.py
```

This updates `src/Menace.Modkit.App/bin/Debug/net10.0/schema.json`

### Update All Schema Locations

```bash
python3 extract_eventhandlers.py il2cpp_dump/dump.cs dist/gui-linux-x64/schema.json
python3 extract_eventhandlers.py il2cpp_dump/dump.cs dist/mcp-linux-x64/schema.json
python3 extract_eventhandlers.py il2cpp_dump/dump.cs src/Menace.Modkit.App/bin/Release/net10.0/schema.json
```

### Query EventHandler Info

```bash
# Show details for a specific type
python3 scripts/query_eventhandler_schema.py Attack

# List all types
python3 scripts/query_eventhandler_schema.py --list

# Search by name
python3 scripts/query_eventhandler_schema.py --search Damage
```

### Rebuild Modkit

```bash
dotnet build src/Menace.Modkit.App/Menace.Modkit.App.csproj -c Debug
```

---

## Field Category Reference

### primitive
- Basic C# types: int, float, bool, string
- Direct values like damage amounts, durations, flags
- **UI Rendering:** TextBox with appropriate validation

### enum
- C# enum types from game code
- Limited set of valid values (often 0-5)
- **UI Rendering:** Dropdown with enum value labels

### reference
- References to template instances (SkillTemplate, EntityTemplate, etc.)
- References to other game objects
- **UI Rendering:** Autocomplete with instance names

### collection
- Arrays (`Type[]`) or Lists (`List<Type>`)
- Contains multiple values of `element_type`
- **UI Rendering:** ListBox with add/remove buttons

### interface
- Interface types that can have multiple implementations
- ITacticalCondition, IItemFilter, ISkillFilter, IValueProvider
- **UI Rendering:** Currently treated as reference (may need special handling)

### unity_asset
- Unity engine asset references
- Sprite, GameObject, AudioClip, Material, etc.
- **UI Rendering:** Asset picker (if available) or text input

### struct
- C# struct types
- Vector3, Quaternion, etc.
- **UI Rendering:** Structured input (X/Y/Z for Vector3)

---

## Examples of Improved Classification

### Before Fix

```json
{
  "name": "SoundToPlay",
  "type": "ID",
  "category": "enum"  ← WRONG
}
```

### After Fix

```json
{
  "name": "SoundToPlay",
  "type": "ID",
  "category": "reference",
  "element_type": "SoundReference",
  "description": ""
}
```

---

## Next Steps

1. **Manual Review:** Check remaining primitive String/ID fields for missed references
2. **UI Testing:** Test EventHandler editor with new schema classifications
3. **Documentation:** Fill in descriptions for commonly-used fields
4. **Interface Handling:** Decide how to handle interface-typed fields in UI
5. **Validation:** Create tests to verify field classification correctness

---

## Files Modified

- `/extract_eventhandlers.py` - Improved field classification logic
- All `schema.json` files - Regenerated with better field types
- This document - Created to track improvements

## Related Documents

- `/working-docs/EVENTHANDLER_SCHEMA_STATUS.md` - Overall status
- `/src/Menace.Modkit.App/eventhandler-schema-README.md` - Usage guide
- `/src/Menace.Modkit.App/eventhandler-schema-report.md` - Analysis report
