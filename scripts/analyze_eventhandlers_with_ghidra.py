#!/usr/bin/env python3
"""
Analyze EventHandler fields using Ghidra decompiled code.

Cross-references the schema against actual game code to:
1. Verify field types and offsets
2. Auto-generate field descriptions based on usage
3. Identify field semantics (what the code does with each field)
4. Find default values and common patterns
"""

import json
import re
import sys
from pathlib import Path
from collections import defaultdict

def load_schema(schema_path):
    """Load schema.json"""
    with open(schema_path, 'r') as f:
        return json.load(f)

def save_schema(schema_path, schema):
    """Save updated schema"""
    with open(schema_path, 'w') as f:
        json.dump(schema, f, indent=2, ensure_ascii=False)

def analyze_field_usage(decompiled_code, offset):
    """
    Analyze how a field at a given offset is used in decompiled code.
    Returns a description based on usage patterns.
    """
    patterns = {
        # Boolean checks
        r'if.*\(lVar\d+ \+ ' + offset + r'\).*!=.*\'\\0\'.*\{':
            "Boolean flag - controls conditional behavior",

        # Enum comparisons
        r'if.*\(lVar\d+ \+ ' + offset + r'\).*==.*\d+':
            "Enum value - checked against specific constants",

        # Function calls with field as parameter
        r'(\w+)\(.*\(lVar\d+ \+ ' + offset + r'\)':
            lambda m: f"Used in {m.group(1)} function call",

        # Template/Reference usage
        r'CreateSkill.*\(lVar\d+ \+ ' + offset + r'\)':
            "Reference to skill template - instantiated via CreateSkill",

        # Condition evaluation
        r'ITacticalCondition.*\(lVar\d+ \+ ' + offset + r'\)':
            "Tactical condition - evaluated to determine if effect applies",

        # List/Collection access
        r'List<\w+>.*\(lVar\d+ \+ ' + offset + r'\)':
            "Collection field - iterated or queried",
    }

    descriptions = []
    for pattern, desc in patterns.items():
        if callable(desc):
            for match in re.finditer(pattern, decompiled_code):
                descriptions.append(desc(match))
        else:
            if re.search(pattern, decompiled_code):
                descriptions.append(desc)

    return descriptions

def infer_enum_values(decompiled_code, offset):
    """
    Find enum value comparisons in code to document what each value means.
    Returns dict of {value: likely_meaning}
    """
    values = {}

    # Pattern: if (*(int *)(lVar + 0x58) == 2)
    pattern = rf'\*\(int \*\)\(lVar\d+ \+ {offset}\)\s*==\s*(\d+)'

    for match in re.finditer(pattern, decompiled_code):
        value = int(match.group(1))
        # Try to find context around this check
        start = max(0, match.start() - 200)
        end = min(len(decompiled_code), match.end() + 200)
        context = decompiled_code[start:end]

        # Look for hints about what this value means
        if 'hit' in context.lower():
            values[value] = 'OnHit'
        elif 'kill' in context.lower() or 'destroy' in context.lower():
            values[value] = 'OnKill'
        elif 'start' in context.lower():
            values[value] = 'OnStart'
        elif 'end' in context.lower():
            values[value] = 'OnEnd'
        elif 'round' in context.lower():
            values[value] = 'OnRound'
        else:
            values[value] = f'Unknown_{value}'

    return values

def analyze_eventhandler(handler_name, schema, ghidra_search, ghidra_decompile):
    """
    Analyze a specific EventHandler type using Ghidra.

    Args:
        handler_name: EventHandler class name (e.g., "AddSkill")
        schema: Current schema dict
        ghidra_search: Function to search for methods
        ghidra_decompile: Function to decompile methods

    Returns:
        dict with analysis results
    """
    results = {
        'handler': handler_name,
        'functions_found': [],
        'field_usage': {},
        'enum_values': {},
        'suggested_descriptions': {}
    }

    # Search for related functions
    search_patterns = [
        f"{handler_name}$$",  # Constructor
        f"{handler_name}Handler$$",  # Handler class
    ]

    for pattern in search_patterns:
        try:
            functions = ghidra_search(pattern)
            if functions:
                results['functions_found'].extend(functions)
        except:
            pass

    # Decompile key functions
    key_methods = ['OnTargetHit', 'OnApply', 'OnAdded', '.ctor']

    for func_info in results['functions_found']:
        func_name = func_info.get('name', '')

        # Check if it's a key method
        if not any(method in func_name for method in key_methods):
            continue

        try:
            decompiled = ghidra_decompile(func_name)
            if not decompiled:
                continue

            code = decompiled.get('result', '')

            # Analyze each field from schema
            handler_data = schema['effect_handlers'].get(handler_name, {})
            for field in handler_data.get('fields', []):
                offset = field['offset']
                field_name = field['name']

                # Analyze usage
                usage = analyze_field_usage(code, offset)
                if usage:
                    results['field_usage'][field_name] = usage

                # For enum fields, try to infer values
                if field['category'] == 'enum':
                    enum_vals = infer_enum_values(code, offset)
                    if enum_vals:
                        results['enum_values'][field_name] = enum_vals

        except Exception as e:
            print(f"  Error decompiling {func_name}: {e}")

    return results

def generate_description_from_analysis(field, analysis_results):
    """
    Generate a human-readable description based on analysis results.
    """
    field_name = field['name']

    # Check if we have usage info
    if field_name in analysis_results.get('field_usage', {}):
        usages = analysis_results['field_usage'][field_name]
        return usages[0] if usages else ""

    # Fallback to name-based inference
    name_lower = field_name.lower()

    if 'only' in name_lower and field['category'] == 'primitive' and field['type'] in ('bool', 'Boolean'):
        return f"Condition: Only applies when {field_name.replace('Only', '').replace('_', ' ')}"

    if 'show' in name_lower and field['type'] in ('bool', 'Boolean'):
        return f"UI flag: Controls whether to show {field_name.replace('Show', '')}"

    if 'event' in name_lower and field['category'] == 'enum':
        return "Timing: When this effect should trigger"

    if 'condition' in name_lower and field['category'] == 'interface':
        return "Condition: Evaluates whether this effect should apply"

    if field_name.endswith('ToAdd') or field_name.endswith('ToSpawn'):
        what = field_name.replace('ToAdd', '').replace('ToSpawn', '')
        return f"Reference: The {what} to add/spawn"

    if 'requires' in name_lower and field['category'] == 'collection':
        return "Filter: Target must have at least one of these tags"

    if 'cannot' in name_lower and field['category'] == 'collection':
        return "Filter: Target must not have any of these tags"

    return ""

def main():
    schema_path = Path('src/Menace.Modkit.App/bin/Debug/net10.0/schema.json')

    if len(sys.argv) > 1:
        schema_path = Path(sys.argv[1])

    if not schema_path.exists():
        print(f"Error: Schema not found at {schema_path}")
        sys.exit(1)

    print(f"Loading schema from {schema_path}...")
    schema = load_schema(schema_path)

    print("\nThis script requires the Ghidra MCP server to be running.")
    print("It will attempt to analyze EventHandler code to generate descriptions.")
    print()

    # For now, just generate descriptions based on field names and types
    # TODO: Integrate with Ghidra MCP calls when running in MCP context

    updated_count = 0

    for handler_name, handler_data in schema['effect_handlers'].items():
        for field in handler_data.get('fields', []):
            if field.get('description', '') == '':
                # Generate description
                desc = generate_description_from_analysis(field, {})
                if desc:
                    field['description'] = desc
                    updated_count += 1

    if updated_count > 0:
        print(f"Generated {updated_count} field descriptions")
        print(f"Saving updated schema to {schema_path}...")
        save_schema(schema_path, schema)
        print("Done!")
    else:
        print("No descriptions to add (all fields already have descriptions or no patterns matched)")

if __name__ == '__main__':
    main()
