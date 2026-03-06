#!/usr/bin/env python3
"""
Quick lookup tool for EventHandler schema.
Usage:
  python3 query_eventhandler_schema.py [type_name]
  python3 query_eventhandler_schema.py --list
  python3 query_eventhandler_schema.py --stats
  python3 query_eventhandler_schema.py --search [pattern]
"""

import json
import sys
import re
from pathlib import Path

SCHEMA_PATH = Path(__file__).parent.parent / "src/Menace.Modkit.App/eventhandler-schema.json"

def load_schema():
    """Load the EventHandler schema."""
    with open(SCHEMA_PATH, 'r') as f:
        return json.load(f)

def list_types(schema):
    """List all EventHandler types."""
    print("All EventHandler Types:")
    print("=" * 80)

    types_sorted = sorted(
        schema['eventHandlerTypes'].items(),
        key=lambda x: x[1]['instanceCount'],
        reverse=True
    )

    for type_name, type_data in types_sorted:
        count = type_data['instanceCount']
        field_count = len(type_data['fields'])
        print(f"  {type_name:40s} ({count:4d} instances, {field_count:2d} fields)")

def show_stats(schema):
    """Show schema statistics."""
    total_types = len(schema['eventHandlerTypes'])
    total_instances = sum(t['instanceCount'] for t in schema['eventHandlerTypes'].values())
    total_fields = sum(len(t['fields']) for t in schema['eventHandlerTypes'].values())

    print("EventHandler Schema Statistics")
    print("=" * 80)
    print(f"Total types: {total_types}")
    print(f"Total instances: {total_instances}")
    print(f"Total fields: {total_fields}")
    print(f"Average fields per type: {total_fields / total_types:.1f}")

    # Count field types
    type_counts = {}
    for type_name, type_data in schema['eventHandlerTypes'].items():
        for field_name, field_info in type_data['fields'].items():
            field_type = field_info.get('type', 'unknown')
            type_counts[field_type] = type_counts.get(field_type, 0) + 1

    print("\nField type distribution:")
    for ftype, count in sorted(type_counts.items(), key=lambda x: x[1], reverse=True):
        pct = (count / total_fields) * 100
        print(f"  {ftype:15s}: {count:4d} ({pct:5.1f}%)")

def show_type(schema, type_name):
    """Show details for a specific type."""
    if type_name not in schema['eventHandlerTypes']:
        print(f"Error: Type '{type_name}' not found in schema.")
        print(f"\nDid you mean one of these?")
        # Fuzzy search
        matches = []
        pattern = type_name.lower()
        for name in schema['eventHandlerTypes'].keys():
            if pattern in name.lower():
                matches.append(name)

        if matches:
            for match in matches[:5]:
                print(f"  - {match}")
        return

    type_data = schema['eventHandlerTypes'][type_name]

    print(f"EventHandler Type: {type_name}")
    print("=" * 80)
    print(f"Instances in game data: {type_data['instanceCount']}")
    print(f"Number of fields: {len(type_data['fields'])}")
    print()

    print("Fields:")
    print("-" * 80)

    for field_name, field_info in sorted(type_data['fields'].items()):
        field_type = field_info.get('type', 'unknown')

        # Build type display
        type_display = field_type
        if field_info.get('nullable'):
            type_display += '?'
        if field_type == 'array':
            elem_type = field_info.get('elementType', 'unknown')
            type_display = f'array<{elem_type}>'

        print(f"\n  {field_name}: {type_display}")

        # Show enum values
        if field_type == 'enum' and 'values' in field_info:
            values = field_info['values']
            if len(values) <= 10:
                print(f"    Possible values: {values}")
            else:
                print(f"    Possible values: {values[:5]} ... {values[-5:]} ({len(values)} total)")

        # Show common string values
        if 'commonValues' in field_info:
            print(f"    Common values: {field_info['commonValues']}")

        # Show sample
        if 'sample' in field_info:
            sample = field_info['sample']
            if isinstance(sample, str):
                if len(sample) > 60:
                    print(f"    Sample: {sample[:60]}...")
                else:
                    print(f"    Sample: {sample}")
            else:
                print(f"    Sample: {sample}")

def search_types(schema, pattern):
    """Search for types matching a pattern."""
    print(f"Searching for types matching '{pattern}':")
    print("=" * 80)

    regex = re.compile(pattern, re.IGNORECASE)
    matches = []

    for type_name, type_data in schema['eventHandlerTypes'].items():
        if regex.search(type_name):
            matches.append((type_name, type_data['instanceCount']))

    if matches:
        matches.sort(key=lambda x: x[1], reverse=True)
        for type_name, count in matches:
            print(f"  {type_name:40s} ({count:4d} instances)")
    else:
        print("  No matches found.")

def main():
    if not SCHEMA_PATH.exists():
        print(f"Error: Schema file not found at {SCHEMA_PATH}")
        print("Please run analyze_eventhandlers.py first.")
        sys.exit(1)

    schema = load_schema()

    if len(sys.argv) < 2:
        print("EventHandler Schema Query Tool")
        print("=" * 80)
        print("Usage:")
        print("  python3 query_eventhandler_schema.py [type_name]     # Show details for a type")
        print("  python3 query_eventhandler_schema.py --list          # List all types")
        print("  python3 query_eventhandler_schema.py --stats         # Show statistics")
        print("  python3 query_eventhandler_schema.py --search PATTERN  # Search for types")
        print()
        print("Examples:")
        print("  python3 query_eventhandler_schema.py Attack")
        print("  python3 query_eventhandler_schema.py --search Damage")
        sys.exit(0)

    arg = sys.argv[1]

    if arg == '--list':
        list_types(schema)
    elif arg == '--stats':
        show_stats(schema)
    elif arg == '--search':
        if len(sys.argv) < 3:
            print("Error: --search requires a pattern argument")
            sys.exit(1)
        search_types(schema, sys.argv[2])
    else:
        show_type(schema, arg)

if __name__ == "__main__":
    main()
