#!/usr/bin/env python3
"""
Analyze EventHandler instances from PerkTemplate and SkillTemplate JSON files.
Generate a comprehensive schema for all EventHandler types.
"""

import json
import sys
from pathlib import Path
from typing import Dict, List, Set, Any, Optional
from collections import defaultdict

def infer_type(value: Any) -> str:
    """Infer the type from a value."""
    if value is None:
        return "null"
    elif isinstance(value, bool):
        return "bool"
    elif isinstance(value, int):
        return "int"
    elif isinstance(value, float):
        return "float"
    elif isinstance(value, str):
        return "string"
    elif isinstance(value, list):
        return "array"
    elif isinstance(value, dict):
        return "object"
    else:
        return "unknown"

def infer_array_element_type(values: List[Any]) -> str:
    """Infer the element type of an array."""
    if not values:
        return "unknown"

    types = set()
    for val in values:
        if isinstance(val, list):
            for item in val:
                types.add(infer_type(item))
        else:
            types.add(infer_type(val))

    if len(types) == 1:
        return types.pop()
    elif len(types) == 2 and "null" in types:
        # Nullable type
        types.remove("null")
        return f"{types.pop()}?"
    else:
        return "mixed"

def is_likely_enum(values: List[Any], type_name: str) -> tuple[bool, List[Any]]:
    """
    Check if a field is likely an enum based on:
    - Limited number of distinct values
    - Numeric values (int or float)
    """
    if type_name not in ["int", "float"]:
        return False, []

    # Filter out None values
    non_null_values = [v for v in values if v is not None]
    if not non_null_values:
        return False, []

    unique_values = sorted(set(non_null_values))

    # If there are 10 or fewer unique values, it's likely an enum
    # But only if there are at least 2 instances (to avoid false positives)
    if len(unique_values) <= 10 and len(non_null_values) >= 2:
        return True, unique_values

    return False, []

class EventHandlerAnalyzer:
    def __init__(self):
        self.type_fields: Dict[str, Dict[str, List[Any]]] = defaultdict(lambda: defaultdict(list))
        self.type_counts: Dict[str, int] = defaultdict(int)

    def analyze_eventhandler(self, eh: Dict[str, Any]):
        """Analyze a single EventHandler object."""
        if not isinstance(eh, dict):
            return

        eh_type = eh.get("_type")
        if not eh_type:
            return

        self.type_counts[eh_type] += 1

        # Collect all fields and their values
        for field_name, field_value in eh.items():
            if field_name == "_type":
                continue
            self.type_fields[eh_type][field_name].append(field_value)

    def process_template(self, template: Dict[str, Any]):
        """Process a template (Perk or Skill) and extract EventHandlers."""
        if not isinstance(template, dict):
            return

        # Check for EventHandlers field
        event_handlers = template.get("EventHandlers")
        if isinstance(event_handlers, list):
            for eh in event_handlers:
                self.analyze_eventhandler(eh)

        # Recursively search for EventHandlers in nested structures
        for key, value in template.items():
            if key == "EventHandlers" and isinstance(value, list):
                continue  # Already processed
            elif isinstance(value, list):
                for item in value:
                    if isinstance(item, dict):
                        self.process_template(item)
            elif isinstance(value, dict):
                self.process_template(value)

    def generate_schema(self) -> Dict[str, Any]:
        """Generate the final schema from collected data."""
        schema = {
            "eventHandlerTypes": {}
        }

        for eh_type, fields in sorted(self.type_fields.items()):
            type_schema = {
                "fields": {},
                "instanceCount": self.type_counts[eh_type]
            }

            for field_name, values in sorted(fields.items()):
                field_info = {}

                # Determine base type
                types_seen = set(infer_type(v) for v in values)

                # Check for nullable
                nullable = "null" in types_seen
                if nullable:
                    types_seen.discard("null")

                # Determine primary type
                if len(types_seen) == 0:
                    primary_type = "null"
                elif len(types_seen) == 1:
                    primary_type = types_seen.pop()
                else:
                    # Mixed types - choose most common
                    type_counts = defaultdict(int)
                    for v in values:
                        if v is not None:
                            type_counts[infer_type(v)] += 1
                    primary_type = max(type_counts.items(), key=lambda x: x[1])[0] if type_counts else "mixed"

                field_info["type"] = primary_type

                if nullable:
                    field_info["nullable"] = True

                # Check for enum
                is_enum, enum_values = is_likely_enum(values, primary_type)
                if is_enum:
                    field_info["type"] = "enum"
                    field_info["values"] = enum_values

                # For arrays, determine element type
                if primary_type == "array":
                    element_type = infer_array_element_type(values)
                    field_info["elementType"] = element_type

                # For strings, check for common patterns
                if primary_type == "string":
                    non_null_values = [v for v in values if v is not None]
                    if non_null_values:
                        unique_values = set(non_null_values)
                        if len(unique_values) <= 10:
                            field_info["commonValues"] = sorted(unique_values)

                # Add sample value (non-null if possible)
                non_null_values = [v for v in values if v is not None]
                if non_null_values:
                    sample = non_null_values[0]
                    # Truncate long strings or large objects
                    if isinstance(sample, str) and len(sample) > 100:
                        field_info["sample"] = sample[:100] + "..."
                    elif isinstance(sample, (list, dict)):
                        field_info["sample"] = str(sample)[:100] + "..."
                    else:
                        field_info["sample"] = sample

                type_schema["fields"][field_name] = field_info

            schema["eventHandlerTypes"][eh_type] = type_schema

        return schema

def main():
    data_dir = Path("/home/poss/.steam/debian-installation/steamapps/common/Menace/UserData/ExtractedData")

    perk_file = data_dir / "PerkTemplate.json"
    skill_file = data_dir / "SkillTemplate.json"

    print("Loading template files...")

    analyzer = EventHandlerAnalyzer()

    # Process PerkTemplate
    if perk_file.exists():
        print(f"Processing {perk_file}...")
        with open(perk_file, 'r', encoding='utf-8') as f:
            perk_data = json.load(f)
            if isinstance(perk_data, list):
                for template in perk_data:
                    analyzer.process_template(template)
            elif isinstance(perk_data, dict):
                analyzer.process_template(perk_data)
    else:
        print(f"Warning: {perk_file} not found")

    # Process SkillTemplate
    if skill_file.exists():
        print(f"Processing {skill_file}...")
        with open(skill_file, 'r', encoding='utf-8') as f:
            skill_data = json.load(f)
            if isinstance(skill_data, list):
                for template in skill_data:
                    analyzer.process_template(template)
            elif isinstance(skill_data, dict):
                analyzer.process_template(skill_data)
    else:
        print(f"Warning: {skill_file} not found")

    print("\nGenerating schema...")
    schema = analyzer.generate_schema()

    # Output location
    output_file = Path("/home/poss/Documents/Code/Menace/MenaceAssetPacker/src/Menace.Modkit.App/eventhandler-schema.json")
    output_file.parent.mkdir(parents=True, exist_ok=True)

    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(schema, f, indent=2)

    print(f"\nSchema written to: {output_file}")

    # Generate summary
    print("\n" + "="*80)
    print("SUMMARY")
    print("="*80)

    total_types = len(schema["eventHandlerTypes"])
    total_instances = sum(t["instanceCount"] for t in schema["eventHandlerTypes"].values())

    print(f"\nTotal EventHandler types found: {total_types}")
    print(f"Total EventHandler instances: {total_instances}")

    print(f"\nTop 10 most common types:")
    sorted_types = sorted(
        schema["eventHandlerTypes"].items(),
        key=lambda x: x[1]["instanceCount"],
        reverse=True
    )
    for i, (type_name, type_data) in enumerate(sorted_types[:10], 1):
        print(f"  {i:2d}. {type_name:40s} ({type_data['instanceCount']:4d} instances)")

    # Find enum fields
    print(f"\n" + "-"*80)
    print("Fields that appear to be enums:")
    print("-"*80)
    enum_fields = []
    for type_name, type_data in sorted(schema["eventHandlerTypes"].items()):
        for field_name, field_info in type_data["fields"].items():
            if field_info.get("type") == "enum":
                enum_fields.append((type_name, field_name, field_info["values"]))

    if enum_fields:
        for type_name, field_name, values in enum_fields:
            print(f"\n{type_name}.{field_name}:")
            print(f"  Values: {values}")
    else:
        print("  None found")

    # Find types with complex structures
    print(f"\n" + "-"*80)
    print("Types with object or array fields:")
    print("-"*80)
    complex_types = []
    for type_name, type_data in sorted(schema["eventHandlerTypes"].items()):
        complex_fields = []
        for field_name, field_info in type_data["fields"].items():
            if field_info.get("type") in ["object", "array"]:
                complex_fields.append((field_name, field_info.get("type")))
        if complex_fields:
            complex_types.append((type_name, complex_fields))

    if complex_types:
        for type_name, fields in complex_types:
            print(f"\n{type_name}:")
            for field_name, field_type in fields:
                print(f"  - {field_name} ({field_type})")
    else:
        print("  None found")

    # Find types with few instances (potentially problematic)
    print(f"\n" + "-"*80)
    print("Types with fewer than 3 instances (may need manual review):")
    print("-"*80)
    rare_types = [(name, data["instanceCount"])
                  for name, data in schema["eventHandlerTypes"].items()
                  if data["instanceCount"] < 3]
    rare_types.sort(key=lambda x: x[1])

    if rare_types:
        for type_name, count in rare_types:
            print(f"  - {type_name}: {count} instance(s)")
    else:
        print("  None found")

    print("\n" + "="*80)
    print(f"Analysis complete!")
    print("="*80)

if __name__ == "__main__":
    main()
