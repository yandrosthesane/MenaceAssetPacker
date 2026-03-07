#!/usr/bin/env python3
"""
Review EventHandler fields that might need better classification.

Helps identify String/ID fields that could be references based on naming patterns.
"""

import json
import sys
from pathlib import Path
from collections import defaultdict

def load_schema(schema_path):
    """Load schema.json"""
    with open(schema_path, 'r') as f:
        return json.load(f)

def analyze_primitive_fields(schema):
    """Find primitive fields that might be references"""
    suspicious = []

    for handler_name, handler_data in schema['effect_handlers'].items():
        for field in handler_data.get('fields', []):
            if field['category'] != 'primitive':
                continue

            field_type = field['type']
            field_name = field['name']
            field_lower = field_name.lower()

            # Look for naming patterns that suggest references
            patterns = {
                'Skill/Ability': ['skill', 'ability'],
                'Entity/Actor': ['entity', 'actor', 'unit'],
                'Item': ['item'],
                'Effect': ['effect'],
                'Tag': ['tag'],
                'Sound': ['sound', 'audio'],
                'Animation': ['animation', 'anim'],
                'Perk': ['perk'],
                'Spawn': ['spawn', 'tospawn'],
                'Condition': ['condition'],
                'Filter': ['filter'],
            }

            matches = []
            for pattern_name, keywords in patterns.items():
                if any(kw in field_lower for kw in keywords):
                    matches.append(pattern_name)

            if matches and field_type in ('String', 'string', 'int', 'Int32'):
                suspicious.append({
                    'handler': handler_name,
                    'field': field_name,
                    'type': field_type,
                    'patterns': matches
                })

    return suspicious

def analyze_interface_fields(schema):
    """List all interface-typed fields"""
    interfaces = []

    for handler_name, handler_data in schema['effect_handlers'].items():
        for field in handler_data.get('fields', []):
            if field['category'] == 'interface':
                interfaces.append({
                    'handler': handler_name,
                    'field': field['name'],
                    'type': field['type']
                })

    return interfaces

def analyze_collections_without_element_type(schema):
    """Find collection fields missing element_type"""
    missing = []

    for handler_name, handler_data in schema['effect_handlers'].items():
        for field in handler_data.get('fields', []):
            if field['category'] == 'collection' and 'element_type' not in field:
                missing.append({
                    'handler': handler_name,
                    'field': field['name'],
                    'type': field['type']
                })

    return missing

def group_by_field_name(items):
    """Group items by field name to find common patterns"""
    grouped = defaultdict(list)
    for item in items:
        grouped[item['field']].append(item)
    return grouped

def main():
    schema_path = Path('src/Menace.Modkit.App/bin/Debug/net10.0/schema.json')

    if len(sys.argv) > 1:
        schema_path = Path(sys.argv[1])

    if not schema_path.exists():
        print(f"Error: Schema not found at {schema_path}")
        sys.exit(1)

    print(f"Loading schema from {schema_path}...")
    schema = load_schema(schema_path)

    print("\n" + "=" * 80)
    print("PRIMITIVE FIELDS THAT MIGHT BE REFERENCES")
    print("=" * 80)

    suspicious = analyze_primitive_fields(schema)

    if suspicious:
        grouped = group_by_field_name(suspicious)

        # Sort by frequency
        sorted_fields = sorted(grouped.items(), key=lambda x: len(x[1]), reverse=True)

        for field_name, instances in sorted_fields[:20]:  # Top 20
            count = len(instances)
            sample = instances[0]
            patterns = ', '.join(sample['patterns'])
            print(f"\n{field_name} ({sample['type']}) - {count} occurrences")
            print(f"  Matches patterns: {patterns}")
            if count <= 3:
                print(f"  Found in: {', '.join(i['handler'] for i in instances)}")
    else:
        print("No suspicious primitive fields found!")

    print("\n" + "=" * 80)
    print("INTERFACE-TYPED FIELDS")
    print("=" * 80)

    interfaces = analyze_interface_fields(schema)

    if interfaces:
        interface_types = defaultdict(int)
        for item in interfaces:
            interface_types[item['type']] += 1

        for itype, count in sorted(interface_types.items(), key=lambda x: x[1], reverse=True):
            print(f"\n{itype}: {count} fields")
            samples = [i for i in interfaces if i['type'] == itype][:5]
            for sample in samples:
                print(f"  - {sample['handler']}.{sample['field']}")
    else:
        print("No interface fields found!")

    print("\n" + "=" * 80)
    print("COLLECTIONS WITHOUT ELEMENT_TYPE")
    print("=" * 80)

    missing = analyze_collections_without_element_type(schema)

    if missing:
        for item in missing:
            print(f"  {item['handler']}.{item['field']} ({item['type']})")
    else:
        print("All collections have element_type defined!")

    print("\n" + "=" * 80)
    print("SUMMARY")
    print("=" * 80)
    print(f"Primitive fields that might be references: {len(suspicious)}")
    print(f"Interface-typed fields needing special handling: {len(interfaces)}")
    print(f"Collections missing element_type: {len(missing)}")

if __name__ == '__main__':
    main()
