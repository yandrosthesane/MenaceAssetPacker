#!/usr/bin/env python3
"""
Document EventHandler fields using Ghidra analysis and write findings back to Ghidra.

This script:
1. Analyzes EventHandler code in Ghidra
2. Generates field descriptions based on usage
3. Writes comments to Ghidra database for future reference
4. Saves findings to JSON for migration to newer game versions

Migration Strategy:
- Field knowledge stored in eventhandler_knowledge.json
- Can be re-applied to updated game assemblies by offset/name matching
- Ghidra comments serve as inline documentation
"""

import json
import re
import sys
from pathlib import Path
from datetime import datetime

# This will be populated with MCP tool functions when running
# For standalone use, we'll use name-based inference
GHIDRA_AVAILABLE = False

try:
    # When run via MCP, these will be available
    from mcp_tools import ghidra_search, ghidra_decompile, ghidra_set_comment
    GHIDRA_AVAILABLE = True
except:
    pass

def load_schema(schema_path):
    """Load schema.json"""
    with open(schema_path, 'r') as f:
        return json.load(f)

def save_schema(schema_path, schema):
    """Save updated schema"""
    with open(schema_path, 'w') as f:
        json.dump(schema, f, indent=2, ensure_ascii=False)

def load_knowledge_base(kb_path):
    """Load existing EventHandler knowledge base"""
    if kb_path.exists():
        with open(kb_path, 'r') as f:
            return json.load(f)
    return {
        'version': '1.0',
        'generated': datetime.now().isoformat(),
        'handlers': {}
    }

def save_knowledge_base(kb_path, kb):
    """Save EventHandler knowledge base"""
    kb['updated'] = datetime.now().isoformat()
    with open(kb_path, 'w') as f:
        json.dump(kb, f, indent=2, ensure_ascii=False)

def analyze_field_usage_from_code(code, offset, field_name, field_type):
    """
    Analyze decompiled code to understand what a field does.
    Returns (description, confidence_score)
    """
    descriptions = []
    confidence = 0

    # Convert offset to hex without 0x prefix for matching
    offset_hex = offset.replace('0x', '')

    # Pattern 1: Boolean flag checks
    bool_pattern = rf'\*\((?:char|byte) \*\)\(.*\+ 0x{offset_hex}\)\s*(?:==|!=)\s*[\'"]?\\0[\'"]?'
    if re.search(bool_pattern, code, re.IGNORECASE):
        descriptions.append("Boolean flag checked in code")
        confidence += 0.3

    # Pattern 2: Null checks (reference fields)
    null_pattern = rf'\*\(.*\*\)\(.*\+ 0x{offset_hex}\)\s*(?:==|!=)\s*0'
    if re.search(null_pattern, code):
        descriptions.append("Nullable reference")
        confidence += 0.2

    # Pattern 3: Enum value comparisons
    enum_pattern = rf'\*\(int \*\)\(.*\+ 0x{offset_hex}\)\s*==\s*(\d+)'
    enum_matches = re.findall(enum_pattern, code)
    if enum_matches:
        values = ', '.join(set(enum_matches))
        descriptions.append(f"Enum compared to values: {values}")
        confidence += 0.4

    # Pattern 4: Function calls with field
    call_pattern = rf'(\w+)\([^)]*\*\(.*\*\)\(.*\+ 0x{offset_hex}\)[^)]*\)'
    call_matches = re.findall(call_pattern, code)
    if call_matches:
        functions = list(set(call_matches))[:3]  # First 3 unique
        descriptions.append(f"Passed to: {', '.join(functions)}")
        confidence += 0.5

    # Specific patterns based on field name
    name_lower = field_name.lower()

    if 'skill' in name_lower:
        if 'CreateSkill' in code or 'GetSkill' in code:
            descriptions.append("Skill template reference - instantiated in code")
            confidence += 0.6

    if 'condition' in name_lower:
        if 'ITacticalCondition' in code:
            descriptions.append("Tactical condition - evaluated to determine if effect applies")
            confidence += 0.7

    if 'sound' in name_lower:
        if 'PlaySound' in code or 'Audio' in code:
            descriptions.append("Sound reference - played during effect")
            confidence += 0.6

    if 'show' in name_lower or 'hide' in name_lower:
        if 'HUD' in code or 'UI' in code:
            descriptions.append("UI control flag - affects display")
            confidence += 0.5

    # Target/filter patterns
    if 'target' in name_lower:
        if 'requires' in name_lower:
            descriptions.append("Filter: Target must match this requirement")
            confidence += 0.6
        elif 'cannot' in name_lower:
            descriptions.append("Filter: Target must not match this requirement")
            confidence += 0.6

    return '; '.join(descriptions) if descriptions else "", min(confidence, 1.0)

def infer_description_from_name_and_type(field_name, field_type, field_category):
    """
    Fallback: Infer description from field name and type when code analysis unavailable.
    """
    name_lower = field_name.lower()

    # Boolean flags
    if field_type in ('bool', 'Boolean'):
        if name_lower.startswith('only'):
            condition = field_name.replace('Only', '').replace('_', ' ')
            return f"Condition: Only applies when {condition}"
        if name_lower.startswith('show'):
            what = field_name.replace('Show', '').replace('_', ' ')
            return f"UI: Controls whether to show {what}"
        if name_lower.startswith('hide'):
            what = field_name.replace('Hide', '').replace('_', ' ')
            return f"UI: Controls whether to hide {what}"
        if name_lower.startswith('is'):
            state = field_name.replace('Is', '').replace('_', ' ')
            return f"State: True if {state}"

    # Event enums
    if 'event' in name_lower and field_category == 'enum':
        return "Timing: Specifies when this effect triggers (OnHit, OnKill, OnStart, etc.)"

    # Conditions
    if 'condition' in name_lower and field_category == 'interface':
        return "Condition: Determines if effect applies based on game state"

    # Filters
    if 'filter' in name_lower and field_category == 'interface':
        return "Filter: Specifies which targets/items are affected"

    # References
    if field_name.endswith('ToAdd'):
        what = field_name.replace('ToAdd', '')
        return f"Reference: The {what} to add when effect triggers"

    if field_name.endswith('ToSpawn'):
        what = field_name.replace('ToSpawn', '')
        return f"Reference: The {what} to spawn when effect triggers"

    if field_name.endswith('ToPlay'):
        what = field_name.replace('ToPlay', '')
        return f"Reference: The {what} to play when effect triggers"

    # Collections
    if field_category == 'collection':
        if 'requires' in name_lower:
            return "Filter: Must have at least one of these"
        if 'cannot' in name_lower:
            return "Filter: Must not have any of these"

    # Numeric values
    if field_type in ('int', 'Int32', 'float', 'Single'):
        if 'damage' in name_lower:
            return "Damage value or modifier"
        if 'percent' in name_lower or 'pct' in name_lower:
            return "Percentage value (0-100)"
        if 'chance' in name_lower:
            return "Probability (0-100)"
        if 'duration' in name_lower or 'time' in name_lower:
            return "Duration or time value"
        if 'amount' in name_lower or 'count' in name_lower:
            return "Quantity or count value"

    return ""

def analyze_eventhandler_with_ghidra(handler_name, schema, kb):
    """
    Analyze a specific EventHandler using Ghidra decompiled code.
    Updates knowledge base with findings.
    """
    if not GHIDRA_AVAILABLE:
        print(f"  Ghidra not available, using name-based inference for {handler_name}")
        return analyze_eventhandler_without_ghidra(handler_name, schema, kb)

    print(f"\nAnalyzing {handler_name}...")

    handler_data = schema['effect_handlers'].get(handler_name, {})
    if not handler_data:
        return

    # Search for handler functions
    try:
        search_results = ghidra_search(f"{handler_name}Handler$$")
        if not search_results:
            print(f"  No handler functions found")
            return
    except Exception as e:
        print(f"  Error searching: {e}")
        return

    # Decompile key methods
    key_methods = ['OnTargetHit', 'OnApply', 'OnAdded', 'OnUse', '.ctor']
    analyzed_code = ""

    for func in search_results:
        func_name = func.get('name', '')
        if not any(method in func_name for method in key_methods):
            continue

        try:
            result = ghidra_decompile(func_name)
            code = result.get('result', '')
            if code:
                analyzed_code += code + "\n\n"
                print(f"  Decompiled {func_name}")
        except Exception as e:
            print(f"  Error decompiling {func_name}: {e}")

    if not analyzed_code:
        print(f"  No code decompiled, using fallback")
        return analyze_eventhandler_without_ghidra(handler_name, schema, kb)

    # Analyze each field
    for field in handler_data.get('fields', []):
        field_name = field['name']
        offset = field['offset']
        field_type = field['type']
        field_category = field['category']

        desc, confidence = analyze_field_usage_from_code(
            analyzed_code, offset, field_name, field_type)

        # Fallback to name-based if low confidence
        if confidence < 0.3:
            desc = infer_description_from_name_and_type(
                field_name, field_type, field_category)
            confidence = 0.2

        # Update field if we have a description
        if desc and not field.get('description'):
            field['description'] = desc
            print(f"    {field_name}: {desc} (confidence: {confidence:.0%})")

            # Store in knowledge base
            if handler_name not in kb['handlers']:
                kb['handlers'][handler_name] = {}
            kb['handlers'][handler_name][field_name] = {
                'offset': offset,
                'type': field_type,
                'category': field_category,
                'description': desc,
                'confidence': confidence,
                'source': 'code_analysis'
            }

def analyze_eventhandler_without_ghidra(handler_name, schema, kb):
    """
    Analyze EventHandler using only name/type inference (no Ghidra).
    """
    handler_data = schema['effect_handlers'].get(handler_name, {})
    if not handler_data:
        return

    for field in handler_data.get('fields', []):
        if field.get('description'):
            continue  # Already has description

        field_name = field['name']
        field_type = field['type']
        field_category = field['category']

        desc = infer_description_from_name_and_type(
            field_name, field_type, field_category)

        if desc:
            field['description'] = desc

            # Store in knowledge base
            if handler_name not in kb['handlers']:
                kb['handlers'][handler_name] = {}
            kb['handlers'][handler_name][field_name] = {
                'offset': field['offset'],
                'type': field_type,
                'category': field_category,
                'description': desc,
                'confidence': 0.3,
                'source': 'name_inference'
            }

def main():
    schema_path = Path('src/Menace.Modkit.App/bin/Debug/net10.0/schema.json')
    kb_path = Path('eventhandler_knowledge.json')

    if len(sys.argv) > 1:
        schema_path = Path(sys.argv[1])

    if not schema_path.exists():
        print(f"Error: Schema not found at {schema_path}")
        sys.exit(1)

    print(f"Loading schema from {schema_path}...")
    schema = load_schema(schema_path)

    print(f"Loading knowledge base from {kb_path}...")
    kb = load_knowledge_base(kb_path)

    total_handlers = len(schema['effect_handlers'])
    print(f"\nFound {total_handlers} EventHandler types")

    if GHIDRA_AVAILABLE:
        print("Ghidra MCP connection available - will analyze code")
    else:
        print("Ghidra not available - using name-based inference")

    # Analyze all handlers
    updated_fields = 0

    for handler_name in sorted(schema['effect_handlers'].keys()):
        handler_data = schema['effect_handlers'][handler_name]

        # Count fields without descriptions
        empty_desc_count = sum(
            1 for f in handler_data.get('fields', [])
            if not f.get('description')
        )

        if empty_desc_count == 0:
            continue  # All fields already documented

        if GHIDRA_AVAILABLE:
            analyze_eventhandler_with_ghidra(handler_name, schema, kb)
        else:
            analyze_eventhandler_without_ghidra(handler_name, schema, kb)

        # Count how many we filled in
        new_empty_count = sum(
            1 for f in handler_data.get('fields', [])
            if not f.get('description')
        )
        updated_fields += (empty_desc_count - new_empty_count)

    print(f"\n{'='*80}")
    print(f"SUMMARY")
    print(f"{'='*80}")
    print(f"Updated {updated_fields} field descriptions")

    if updated_fields > 0:
        print(f"\nSaving updated schema to {schema_path}...")
        save_schema(schema_path, schema)

        print(f"Saving knowledge base to {kb_path}...")
        save_knowledge_base(kb_path, kb)

        print("\nDone! Knowledge saved for future game version migrations.")
    else:
        print("\nNo updates needed (all fields already have descriptions)")

if __name__ == '__main__':
    main()
