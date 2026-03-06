#!/usr/bin/env python3
"""
Extract EventHandler class definitions from IL2CPP dump.cs and add to schema.json
"""

import json
import re
import sys
from pathlib import Path

def parse_eventhandler_classes(dump_path):
    """Parse dump.cs to extract EventHandler classes and their fields"""
    handlers = {}

    with open(dump_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()

    # Find all classes that inherit from SkillEventHandlerTemplate or PerkEventHandlerTemplate
    # Pattern: public class ClassName : BaseClass
    class_pattern = r'public class (\w+)\s*:\s*(?:SkillEventHandlerTemplate|PerkEventHandlerTemplate|(\w*EventHandler\w*))'

    classes = re.finditer(class_pattern, content)

    for match in classes:
        class_name = match.group(1)

        # Skip if it's a base class or abstract
        if 'Template' in class_name or 'Base' in class_name or 'Abstract' in class_name:
            continue

        # Find the class body
        class_start = match.end()
        brace_count = 0
        in_class = False
        class_body_start = -1
        class_body_end = -1

        for i in range(class_start, len(content)):
            if content[i] == '{':
                if not in_class:
                    in_class = True
                    class_body_start = i + 1
                brace_count += 1
            elif content[i] == '}':
                brace_count -= 1
                if brace_count == 0 and in_class:
                    class_body_end = i
                    break

        if class_body_start == -1 or class_body_end == -1:
            continue

        class_body = content[class_body_start:class_body_end]

        # Extract fields (public fields only, non-static)
        field_pattern = r'public\s+(\w+(?:<[^>]+>)?)\s+(\w+);\s*//\s*0x([0-9A-Fa-f]+)'
        fields = []

        for field_match in re.finditer(field_pattern, class_body):
            field_type = field_match.group(1)
            field_name = field_match.group(2)
            offset = field_match.group(3)

            # Skip certain fields
            if field_name in ['k__BackingField', 'NativeFieldInfoPtr', 'NativeClassPtr']:
                continue

            # Determine field category
            category = categorize_field(field_type)

            field_info = {
                "name": field_name,
                "offset": f"0x{offset}",
                "type": simplify_type(field_type),
                "category": category
            }

            # Add element_type for collections
            if category == "collection":
                element_type = extract_element_type(field_type)
                if element_type:
                    field_info["element_type"] = element_type

            fields.append(field_info)

        if fields:
            handlers[class_name] = {
                "fields": fields
            }

    return handlers

def categorize_field(field_type):
    """Determine the category of a field"""
    field_type_lower = field_type.lower()

    if 'list<' in field_type_lower or 'array<' in field_type_lower or field_type.endswith('[]'):
        return "collection"
    elif field_type in ['int', 'Int32', 'float', 'Single', 'double', 'Double', 'bool', 'Boolean', 'string', 'String']:
        return "primitive"
    elif 'template' in field_type_lower:
        return "reference"
    elif field_type.endswith('Type') or field_type.endswith('Kind') or field_type.endswith('Mode'):
        return "enum"
    else:
        return "primitive"

def simplify_type(type_str):
    """Simplify type names"""
    # Remove Il2Cpp prefixes
    type_str = type_str.replace('Il2Cpp', '')
    type_str = type_str.replace('Il2CppSystem.', '')

    # Simplify generic types
    if '<' in type_str:
        base_type = type_str.split('<')[0]
        return base_type

    return type_str

def extract_element_type(type_str):
    """Extract element type from collection types"""
    match = re.search(r'<([^>]+)>', type_str)
    if match:
        return simplify_type(match.group(1))
    return None

def update_schema(schema_path, handlers):
    """Update schema.json with extracted handlers"""
    with open(schema_path, 'r') as f:
        schema = json.load(f)

    # Update or create effect_handlers section
    schema['effect_handlers'] = handlers

    # Write back with pretty printing
    with open(schema_path, 'w') as f:
        json.dump(schema, f, indent=2, ensure_ascii=False)

    print(f"✓ Updated schema.json with {len(handlers)} EventHandler types")

def main():
    dump_path = Path('/home/poss/Documents/Code/Menace/MenaceAssetPacker/il2cpp_dump/dump.cs')
    schema_path = Path('/home/poss/Documents/Code/Menace/MenaceAssetPacker/dist/gui-linux-x64/schema.json')

    if not dump_path.exists():
        print(f"Error: dump.cs not found at {dump_path}")
        sys.exit(1)

    if not schema_path.exists():
        print(f"Error: schema.json not found at {schema_path}")
        sys.exit(1)

    print(f"Parsing EventHandlers from {dump_path}...")
    handlers = parse_eventhandler_classes(str(dump_path))

    print(f"Found {len(handlers)} EventHandler types")
    print(f"Sample handlers: {list(handlers.keys())[:10]}")

    print(f"\nUpdating {schema_path}...")
    update_schema(str(schema_path), handlers)

    print("\n✓ Schema updated successfully!")
    print(f"\nNext steps:")
    print(f"1. Copy updated schema to other locations:")
    print(f"   cp {schema_path} dist/gui-win-x64/schema.json")
    print(f"   cp {schema_path} src/Menace.Modkit.App/bin/Debug/net10.0/schema.json")
    print(f"2. Restart the Modkit app")

if __name__ == '__main__':
    main()
