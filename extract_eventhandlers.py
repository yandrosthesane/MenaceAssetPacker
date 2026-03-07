#!/usr/bin/env python3
"""
Extract EventHandler class definitions from IL2CPP dump.cs and add to schema.json

This script parses EventHandler classes from the IL2CPP dump and properly categorizes
their fields using the existing enum/struct/template definitions from schema.json.
"""

import json
import re
import sys
from pathlib import Path

# Type classification constants (same as generate_schema.py)
PRIMITIVE_TYPES = {
    "int", "Int32", "float", "Single", "bool", "Boolean",
    "byte", "Byte", "short", "Int16", "long", "Int64",
    "double", "Double",
}

UNITY_ASSET_TYPES = {
    "Sprite", "Texture2D", "Material", "Mesh", "AudioClip",
    "AnimationClip", "GameObject", "RuntimeAnimatorController",
    "VolumeProfile",
}

LOCALIZATION_TYPES = {"LocalizedLine", "LocalizedMultiLine"}

def infer_reference_from_name(field_name, field_type):
    """
    Infer reference type from field naming patterns.
    Returns (inferred_reference_type, confidence) or (None, 0)
    """
    field_lower = field_name.lower()

    # Skill references
    if any(pattern in field_lower for pattern in ['skill', 'ability']):
        if 'template' not in field_lower:  # Avoid SkillTemplate type (already correct)
            return ("SkillTemplate", 0.8)

    # Entity/Actor references
    if any(pattern in field_lower for pattern in ['entity', 'actor', 'unit']):
        if 'tospawn' in field_lower or 'spawn' in field_lower:
            return ("EntityTemplate", 0.9)

    # Effect/TileEffect references
    if 'tospawn' in field_lower or 'effecttospawn' in field_lower:
        if 'tile' in field_lower or field_type in ('String', 'ID'):
            return ("TileEffectTemplate", 0.9)

    # Sound references
    if any(pattern in field_lower for pattern in ['sound', 'audio']):
        # Fields like SoundToPlay, SoundWhenSuppressed, etc.
        return ("SoundReference", 0.9)

    # Perk references
    if 'perk' in field_lower and 'template' not in field_lower:
        return ("PerkTemplate", 0.8)

    # Tag references (if it's a string/ID and has 'tag' in name)
    if 'tag' in field_lower and field_type in ('String', 'ID'):
        if 'list' not in field_lower:  # Collections handled elsewhere
            return ("TagTemplate", 0.7)

    # Item references
    if 'item' in field_lower and any(x in field_lower for x in ['tospawn', 'give', 'grant']):
        return ("ItemTemplate", 0.8)

    # Animation references
    if 'animation' in field_lower or 'anim' in field_lower:
        if field_type in ('String', 'ID'):
            return ("AnimationTemplate", 0.7)

    return (None, 0)


def classify_field(field_name, field_type, known_enums, known_structs, known_templates):
    """
    Classify a field type into a category.
    Uses actual type definitions and field naming patterns.
    """
    # Handle array types - extract base type first
    is_array = field_type.endswith("[]")
    base = field_type.rstrip("[]")

    # Check for List<T>
    list_match = re.match(r"List<(\w+)>", field_type)
    if list_match:
        element_type = list_match.group(1)
        return "collection", element_type

    # Array of something
    if is_array:
        return "collection", base

    # Primitives
    if base in PRIMITIVE_TYPES:
        return "primitive", None

    # String - check if it might be a reference based on field name
    if base in ("string", "String"):
        inferred_ref, confidence = infer_reference_from_name(field_name, base)
        if inferred_ref and confidence >= 0.7:
            return "reference", inferred_ref
        return "primitive", None

    # ID struct - check this BEFORE checking enums since ID is both enum and struct
    # The struct version (FMOD sound ID) is more commonly used in EventHandlers
    if base == "ID" and base in known_structs:
        inferred_ref, confidence = infer_reference_from_name(field_name, base)
        if inferred_ref and confidence >= 0.8:
            return "reference", inferred_ref
        # ID struct is a special type for sound/asset references
        return "sound_id", None

    # Enums (actual enum types from schema)
    if base in known_enums:
        return "enum", None

    # Structs
    if base in known_structs:
        return "struct", None

    # Localization
    if base in LOCALIZATION_TYPES:
        return "localization", None

    # Unity assets
    if base in UNITY_ASSET_TYPES:
        return "unity_asset", None

    # Template references
    if base.endswith("Template") and base in known_templates:
        return "reference", None

    # Interfaces and other reference types
    if base.startswith("I") and len(base) > 1 and base[1].isupper():
        # Likely an interface (ITacticalCondition, IItemFilter, etc.)
        return "interface", None

    # Unknown uppercase types - treat as reference
    if base[0:1].isupper():
        return "reference", None

    # Fallback
    return "unknown", None


def parse_eventhandler_classes(dump_path, known_enums, known_structs, known_templates):
    """Parse dump.cs to extract EventHandler classes and their fields"""
    handlers = {}

    with open(dump_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()

    # Find all classes that inherit from SkillEventHandlerTemplate or PerkEventHandlerTemplate
    # Pattern: public class ClassName : BaseClass
    class_pattern = r'public class (\w+)\s*:\s*(?:SkillEventHandlerTemplate|PerkEventHandlerTemplate)'

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
        field_pattern = r'public\s+([\w<>\[\]\.]+)\s+(\w+);\s*//\s*0x([0-9A-Fa-f]+)'
        fields = []

        for field_match in re.finditer(field_pattern, class_body):
            field_type = field_match.group(1)
            field_name = field_match.group(2)
            offset = field_match.group(3)

            # Skip certain fields
            if field_name in ['k__BackingField', 'NativeFieldInfoPtr', 'NativeClassPtr', 'Il2CppClass']:
                continue

            # Determine field category using proper classification
            category, element_type = classify_field(
                field_name, field_type, known_enums, known_structs, known_templates)

            field_info = {
                "name": field_name,
                "type": field_type,
                "category": category,
                "offset": f"0x{offset}",
                "description": ""  # To be filled in manually over time
            }

            # Add element_type for collections or inferred references
            if element_type:
                field_info["element_type"] = element_type

            fields.append(field_info)

        if fields:
            handlers[class_name] = {
                "fields": fields
            }

    return handlers


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
    dump_path = Path('il2cpp_dump/dump.cs')
    schema_path = Path('src/Menace.Modkit.App/bin/Debug/net10.0/schema.json')

    # Allow override from command line
    if len(sys.argv) > 1:
        dump_path = Path(sys.argv[1])
    if len(sys.argv) > 2:
        schema_path = Path(sys.argv[2])

    if not dump_path.exists():
        print(f"Error: dump.cs not found at {dump_path}")
        sys.exit(1)

    if not schema_path.exists():
        print(f"Error: schema.json not found at {schema_path}")
        sys.exit(1)

    # Load existing schema to get known types
    print(f"Loading existing schema from {schema_path}...")
    with open(schema_path, 'r') as f:
        schema = json.load(f)

    known_enums = set(schema.get('enums', {}).keys())
    known_structs = set(schema.get('structs', {}).keys())
    known_templates = set(schema.get('templates', {}).keys())

    print(f"  Loaded {len(known_enums)} enums, {len(known_structs)} structs, {len(known_templates)} templates")

    print(f"\nParsing EventHandlers from {dump_path}...")
    handlers = parse_eventhandler_classes(
        str(dump_path), known_enums, known_structs, known_templates)

    print(f"Found {len(handlers)} EventHandler types")

    # Count field categories
    category_counts = {}
    total_fields = 0
    for handler_name, handler_data in handlers.items():
        for field in handler_data['fields']:
            category = field['category']
            category_counts[category] = category_counts.get(category, 0) + 1
            total_fields += 1

    print(f"Total fields: {total_fields}")
    print(f"Field categories:")
    for category, count in sorted(category_counts.items(), key=lambda x: x[1], reverse=True):
        pct = (count / total_fields) * 100
        print(f"  {category:15s}: {count:4d} ({pct:5.1f}%)")

    print(f"\nUpdating {schema_path}...")
    update_schema(str(schema_path), handlers)

    print("\n✓ Schema updated successfully!")

    # List other locations that might need updating
    other_paths = [
        'dist/gui-linux-x64/schema.json',
        'dist/gui-win-x64/schema.json',
        'dist/mcp-linux-x64/schema.json',
        'dist/mcp-win-x64/schema.json',
        'src/Menace.Modkit.App/bin/Release/net10.0/schema.json',
    ]

    existing_other = [p for p in other_paths if Path(p).exists()]
    if existing_other:
        print(f"\nNote: The following schema files may also need updating:")
        for path in existing_other:
            print(f"  - {path}")
        print(f"\nTo update all, run:")
        for path in existing_other:
            print(f"  python extract_eventhandlers.py il2cpp_dump/dump.cs {path}")


if __name__ == '__main__':
    main()
