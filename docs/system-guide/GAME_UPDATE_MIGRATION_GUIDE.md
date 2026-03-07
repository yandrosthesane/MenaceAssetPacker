# Game Update Migration Guide

**Purpose:** When Menace receives game updates, we need to regenerate schemas and re-apply our knowledge.

---

## Overview

Game updates change:
- **IL2CPP dump** - New class definitions, methods, offsets
- **Ghidra database** - Needs fresh decompilation
- **Extracted game data** - New templates, changed values

Our knowledge to preserve:
- **EventHandler field descriptions** - Stored in `eventhandler_knowledge.json`
- **Ghidra comments** - Inline documentation (need to re-apply)
- **Template patterns** - Field usage insights

---

## Migration Process

### 1. Extract Updated Game Assembly

```bash
# Extract new game files (method depends on platform)
# For Unity IL2CPP games:
./path/to/il2cppdumper GameAssembly.dll global-metadata.dat output_dir/

# This produces new dump.cs
cp output_dir/dump.cs il2cpp_dump/dump.cs
```

### 2. Regenerate Base Schema

```bash
# Generate fresh schema from new dump
python3 tools/generate_schema.py il2cpp_dump/dump.cs generated/schema.json

# Copy to working locations
cp generated/schema.json src/Menace.Modkit.App/bin/Debug/net10.0/schema.json
cp generated/schema.json dist/gui-linux-x64/schema.json
# ... other locations
```

### 3. Extract EventHandler Definitions

```bash
# Extract EventHandler classes with improved classification
python3 extract_eventhandlers.py

# This updates effect_handlers section in schema.json
```

### 4. Re-apply Saved Knowledge

```bash
# Use saved knowledge base to restore descriptions
python3 scripts/apply_eventhandler_knowledge.py eventhandler_knowledge.json src/Menace.Modkit.App/bin/Debug/net10.0/schema.json
```

This script (to be created) matches fields by:
1. **Exact offset match** - Best case, field unchanged
2. **Name + type match** - Offset shifted but semantics same
3. **Fuzzy name match** - Field renamed slightly

### 5. Import to Fresh Ghidra Project

```bash
# Create new Ghidra project for updated game
# Import GameAssembly.dll
# Auto-analyze

# Then apply saved knowledge as comments
python3 scripts/apply_knowledge_to_ghidra.py eventhandler_knowledge.json
```

### 6. Analyze New/Changed EventHandlers

```bash
# For any fields that didn't match, re-analyze
python3 scripts/document_eventhandlers_in_ghidra.py

# This will:
# - Find new EventHandler types
# - Analyze changed offsets
# - Update knowledge base
```

### 7. Update Other Schema Locations

```bash
# Sync to all distribution locations
for path in dist/*/schema.json; do
  cp src/Menace.Modkit.App/bin/Debug/net10.0/schema.json "$path"
done
```

---

## Knowledge Base Format

`eventhandler_knowledge.json` structure:

```json
{
  "version": "1.0",
  "generated": "2026-03-06T...",
  "updated": "2026-03-06T...",
  "game_version": "v32.0.6",
  "handlers": {
    "AddSkill": {
      "Event": {
        "offset": "0x58",
        "type": "AddSkill.AddEvent",
        "category": "enum",
        "description": "Timing: When effect triggers",
        "confidence": 0.8,
        "source": "code_analysis",
        "enum_values": {
          "0": "OnStart",
          "1": "OnEnd",
          "2": "OnHit"
        }
      },
      ...
    }
  }
}
```

### Field Matching Strategy

When migrating to updated game:

**1. Exact Offset Match (Confidence: 100%)**
```python
if new_field.offset == old_knowledge.offset and
   new_field.type == old_knowledge.type:
    # Perfect match - apply description directly
```

**2. Name + Type Match (Confidence: 90%)**
```python
if new_field.name == old_knowledge.name and
   new_field.type == old_knowledge.type:
    # Field moved but unchanged - apply description
    # Log offset change for review
```

**3. Fuzzy Name Match (Confidence: 60%)**
```python
if similarity(new_field.name, old_knowledge.name) > 0.8 and
   new_field.type == old_knowledge.type:
    # Field possibly renamed - apply with warning
    # Mark for manual review
```

**4. No Match (Confidence: 0%)**
```python
# New field or significantly changed
# Re-analyze with Ghidra
# Add to review list
```

---

## Automated Migration Script

Create `scripts/migrate_to_new_game_version.sh`:

```bash
#!/bin/bash
set -e

GAME_VERSION="$1"
DUMP_PATH="$2"

if [ -z "$GAME_VERSION" ] || [ -z "$DUMP_PATH" ]; then
  echo "Usage: ./migrate_to_new_game_version.sh <version> <dump_path>"
  exit 1
fi

echo "=== Migrating to Game Version $GAME_VERSION ==="

# 1. Backup current knowledge
cp eventhandler_knowledge.json "eventhandler_knowledge_${GAME_VERSION}_backup.json"

# 2. Copy new dump
cp "$DUMP_PATH" il2cpp_dump/dump.cs

# 3. Regenerate base schema
python3 tools/generate_schema.py il2cpp_dump/dump.cs generated/schema.json

# 4. Extract EventHandlers
python3 extract_eventhandlers.py

# 5. Apply saved knowledge
python3 scripts/apply_eventhandler_knowledge.py \
  eventhandler_knowledge.json \
  src/Menace.Modkit.App/bin/Debug/net10.0/schema.json

# 6. Re-analyze any unmatched fields
python3 scripts/document_eventhandlers_in_ghidra.py

# 7. Generate migration report
python3 scripts/generate_migration_report.py \
  "eventhandler_knowledge_${GAME_VERSION}_backup.json" \
  eventhandler_knowledge.json \
  > "migration_report_${GAME_VERSION}.md"

echo "Migration complete! Review migration_report_${GAME_VERSION}.md"
```

---

## Manual Review Checklist

After automated migration:

- [ ] Check migration report for high-confidence matches
- [ ] Review fields marked as "fuzzy match" - verify descriptions still accurate
- [ ] Test new EventHandler types in modkit UI
- [ ] Verify enum values still map correctly
- [ ] Update descriptions for significantly changed fields
- [ ] Test modpack loading with new schema
- [ ] Run automated tests against new schema

---

## Rollback Procedure

If migration fails or produces bad results:

```bash
# Restore from backup
cp eventhandler_knowledge_v32.0.6_backup.json eventhandler_knowledge.json
cp schema_backup.json src/Menace.Modkit.App/bin/Debug/net10.0/schema.json

# Rebuild modkit with old schema
dotnet build src/Menace.Modkit.App/Menace.Modkit.App.csproj -c Debug
```

---

## Future Improvements

### Version Diffing
Track schema changes between game versions:
```json
{
  "from_version": "v32.0.5",
  "to_version": "v32.0.6",
  "changes": {
    "AddSkill": {
      "Event": {
        "offset_changed": "0x58 → 0x60",
        "reason": "New field added before"
      }
    }
  }
}
```

### Automated Offset Correction
If fields shift uniformly (e.g., new field added at 0x58, all later fields +8):
```python
def detect_uniform_shift(old_fields, new_fields):
    shifts = []
    for old, new in zip(old_fields, new_fields):
        if old.name == new.name and old.type == new.type:
            shift = new.offset - old.offset
            shifts.append(shift)

    if all(s == shifts[0] for s in shifts):
        return shifts[0]  # Uniform shift detected
    return None
```

### Ghidra Script Integration
Create Ghidra scripts that:
1. Export comments to JSON
2. Import comments from JSON
3. Compare two Ghidra databases
4. Highlight changed function signatures

---

## File Locations

- **Knowledge Base:** `eventhandler_knowledge.json`
- **Schema Files:** `src/Menace.Modkit.App/bin/*/schema.json`, `dist/*/schema.json`
- **IL2CPP Dump:** `il2cpp_dump/dump.cs`
- **Ghidra Project:** `ghidra_projects/menace_v{version}/`
- **Migration Reports:** `migration_report_v{version}.md`

---

## Contact / Troubleshooting

If migration produces errors or bad matches:
1. Check game version compatibility
2. Verify IL2CPP dump was successful
3. Review migration report for clues
4. Re-analyze suspicious fields with Ghidra
5. Document any manual fixes in knowledge base
