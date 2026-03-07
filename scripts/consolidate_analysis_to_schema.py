#!/usr/bin/env python3
"""
Consolidate Ghidra analysis JSON files into schema.json and eventhandler_knowledge.json.

Reads the rich analysis JSON files from /docs/ and:
1. Extracts handler field descriptions
2. Updates schema.json effect_handlers with descriptions
3. Updates eventhandler_knowledge.json with high-confidence findings
"""

import json
import os
import re
import sys
from pathlib import Path
from datetime import datetime

def normalize_handler_name(name):
    """Convert various handler name formats to schema format."""
    # Remove 'Handler' suffix
    name = re.sub(r'Handler$', '', name)
    # Remove namespace prefixes
    if '.' in name:
        name = name.split('.')[-1]
    return name

def extract_fields_from_analysis(data, handler_name=None):
    """Extract field information from various analysis JSON formats."""
    fields = {}

    def safe_items(obj):
        """Safely iterate over dict items, returning empty if not a dict."""
        if isinstance(obj, dict):
            return obj.items()
        return []

    def add_field(handler, offset, info):
        """Add a field to the results."""
        # Normalize offset format
        if isinstance(offset, str):
            if offset.startswith('+'):
                offset = offset[1:]
            if offset.startswith('offset_'):
                offset = offset.replace('offset_', '')
        if isinstance(info, dict):
            fields.setdefault(handler, {})[offset] = info
        elif isinstance(info, str):
            fields.setdefault(handler, {})[offset] = {'description': info}

    # Format 1: primary_handlers.HandlerName.fields.effect_data_layout
    if 'primary_handlers' in data and isinstance(data['primary_handlers'], dict):
        for h_name, h_data in data['primary_handlers'].items():
            if not isinstance(h_data, dict):
                continue
            norm_name = normalize_handler_name(h_name)
            if 'fields' in h_data and isinstance(h_data['fields'], dict):
                if 'effect_data_layout' in h_data['fields']:
                    for offset, field_info in safe_items(h_data['fields']['effect_data_layout']):
                        add_field(norm_name, offset, field_info)

    # Format 2: related_handlers.HandlerName.fields
    if 'related_handlers' in data and isinstance(data['related_handlers'], dict):
        for h_name, h_data in data['related_handlers'].items():
            if not isinstance(h_data, dict):
                continue
            norm_name = normalize_handler_name(h_name)
            if 'fields' in h_data and isinstance(h_data['fields'], dict):
                for layout_name, layout_data in h_data['fields'].items():
                    if not isinstance(layout_data, dict):
                        continue
                    if 'effect_data' in layout_name.lower() or 'template' in layout_name.lower():
                        for offset, field_info in layout_data.items():
                            add_field(norm_name, offset, field_info)

    # Format 3: handlers dict at top level (NOT list)
    if 'handlers' in data and isinstance(data['handlers'], dict):
        for h_name, h_data in data['handlers'].items():
            norm_name = normalize_handler_name(h_name)
            if isinstance(h_data, dict):
                if 'fields' in h_data and isinstance(h_data['fields'], dict):
                    for offset, field_info in h_data['fields'].items():
                        add_field(norm_name, offset, field_info)
                # Check for effect_data_layout directly
                if 'effect_data_layout' in h_data and isinstance(h_data['effect_data_layout'], dict):
                    for offset, field_info in h_data['effect_data_layout'].items():
                        add_field(norm_name, offset, field_info)

    # Format 4: Direct handler analysis with fields at 0x58+
    if 'effect_fields' in data or 'field_layout' in data:
        layout = data.get('effect_fields') or data.get('field_layout', {})
        if isinstance(layout, dict):
            h_name = data.get('handler_name', data.get('class_name', handler_name or 'Unknown'))
            norm_name = normalize_handler_name(h_name)
            for offset, field_info in layout.items():
                add_field(norm_name, offset, field_info)

    # Format 5: Table-style with handler as key directly containing fields
    for key, value in data.items():
        if key.endswith('Handler') and isinstance(value, dict):
            norm_name = normalize_handler_name(key)
            if 'fields' in value and isinstance(value['fields'], dict):
                field_data = value['fields']
                # Could be nested or direct
                for k, v in field_data.items():
                    if k.startswith('0x'):
                        add_field(norm_name, k, v)
                    elif isinstance(v, dict):
                        # Nested layout
                        for offset, field_info in v.items():
                            if offset.startswith('0x'):
                                add_field(norm_name, offset, field_info)

    # Format 6: Analyzed handlers list
    if 'analyzed_handlers' in data and isinstance(data['analyzed_handlers'], list):
        for h_entry in data['analyzed_handlers']:
            if isinstance(h_entry, dict):
                h_name = h_entry.get('name', h_entry.get('handler_name', ''))
                norm_name = normalize_handler_name(h_name)
                if 'fields' in h_entry and isinstance(h_entry['fields'], dict):
                    for offset, field_info in h_entry['fields'].items():
                        add_field(norm_name, offset, field_info)

    # Format 7: handlers array with effect_def_fields (common format from overnight agents)
    if 'handlers' in data and isinstance(data['handlers'], list):
        for h_entry in data['handlers']:
            if not isinstance(h_entry, dict):
                continue
            h_name = h_entry.get('name', h_entry.get('class_name', ''))
            if not h_name:
                continue
            norm_name = normalize_handler_name(h_name)

            # Check various field key formats
            field_keys = ['effect_def_fields', 'effect_data_fields', 'template_fields',
                         'data_fields', 'fields', 'effect_fields', 'handler_fields']
            for field_key in field_keys:
                if field_key not in h_entry:
                    continue
                field_data = h_entry[field_key]

                # Handle dict format: {"0x58": {...}}
                if isinstance(field_data, dict):
                    for offset, field_info in field_data.items():
                        if offset.startswith('0x'):
                            add_field(norm_name, offset, field_info)

                # Handle list format: [{"offset": "0x58", "name": "X", ...}]
                elif isinstance(field_data, list):
                    for field_info in field_data:
                        if isinstance(field_info, dict) and 'offset' in field_info:
                            offset = field_info['offset']
                            if isinstance(offset, str) and offset.startswith('0x'):
                                # Build a clean field info dict
                                clean_info = {k: v for k, v in field_info.items() if k != 'offset'}
                                add_field(norm_name, offset, clean_info)

    def process_handler_entry(h_entry, fields_result):
        """Process a handler entry from any array format."""
        if not isinstance(h_entry, dict):
            return
        h_name = h_entry.get('name', h_entry.get('class_name', ''))
        if not h_name:
            return
        norm_name = normalize_handler_name(h_name)

        field_keys = ['effect_def_fields', 'effect_data_fields', 'template_fields',
                     'data_fields', 'fields', 'effect_fields', 'handler_fields']
        for field_key in field_keys:
            if field_key not in h_entry:
                continue
            field_data = h_entry[field_key]

            # Handle dict format
            if isinstance(field_data, dict):
                for offset, field_info in field_data.items():
                    if isinstance(offset, str) and offset.startswith('0x'):
                        add_field(norm_name, offset, field_info)

            # Handle list format
            elif isinstance(field_data, list):
                for field_info in field_data:
                    if isinstance(field_info, dict) and 'offset' in field_info:
                        offset = field_info['offset']
                        if isinstance(offset, str) and offset.startswith('0x'):
                            clean_info = {k: v for k, v in field_info.items() if k != 'offset'}
                            add_field(norm_name, offset, clean_info)

    # Format 8: analyzed_classes array
    if 'analyzed_classes' in data and isinstance(data['analyzed_classes'], list):
        for h_entry in data['analyzed_classes']:
            process_handler_entry(h_entry, fields)

    # Format 9: Classes array (another common format)
    if 'classes' in data and isinstance(data['classes'], list):
        for h_entry in data['classes']:
            process_handler_entry(h_entry, fields)

    # Format 10: analyzed_classes as dict (key is handler name)
    if 'analyzed_classes' in data and isinstance(data['analyzed_classes'], dict):
        for h_name, h_data in data['analyzed_classes'].items():
            if not isinstance(h_data, dict):
                continue
            norm_name = normalize_handler_name(h_name)

            # Check for fields dict with offset_0xNN keys
            if 'fields' in h_data and isinstance(h_data['fields'], dict):
                for key, field_info in h_data['fields'].items():
                    # Handle "offset_0x58" format
                    if key.startswith('offset_'):
                        offset = key.replace('offset_', '')
                        if isinstance(field_info, dict):
                            add_field(norm_name, offset, field_info)
                    # Handle "0x58" format
                    elif key.startswith('0x'):
                        if isinstance(field_info, dict):
                            add_field(norm_name, key, field_info)

    # Format 11: Top-level dict where keys are handler names (common simple format)
    for key, value in data.items():
        if key in ('title', 'description', 'analysis_summary', 'handlers', 'analyzed_classes',
                   'classes', 'primary_handlers', 'related_handlers', 'modding_notes',
                   'implementation_patterns', 'entity_fields', 'ghidra_comments_added',
                   'tactical_manager_events', 'analysis_date', 'date', 'source', 'tool', 'binary',
                   'version'):
            continue
        if isinstance(value, dict) and 'fields' in value:
            norm_name = normalize_handler_name(key)
            field_data = value['fields']
            if isinstance(field_data, dict):
                for fkey, field_info in field_data.items():
                    if fkey.startswith('offset_'):
                        offset = fkey.replace('offset_', '')
                        add_field(norm_name, offset, field_info)
                    elif fkey.startswith('0x') or fkey.startswith('+0x'):
                        add_field(norm_name, fkey, field_info)
            elif isinstance(field_data, list):
                for field_info in field_data:
                    if isinstance(field_info, dict) and 'offset' in field_info:
                        offset = field_info['offset']
                        if isinstance(offset, str):
                            clean_info = {k: v for k, v in field_info.items() if k != 'offset'}
                            add_field(norm_name, offset, clean_info)

    # Format 12: 'classes' dict with handler_fields/config_fields (another common format)
    for class_key in ['classes', 'key_classes', 'analyzed']:
        if class_key not in data or not isinstance(data[class_key], dict):
            continue
        for h_name, h_data in data[class_key].items():
            if not isinstance(h_data, dict):
                continue
            # Extract handler name from fully qualified name
            norm_name = normalize_handler_name(h_name.split('.')[-1])

            # Check handler_fields and config_fields
            for field_key in ['handler_fields', 'config_fields', 'effect_fields', 'fields',
                             'template_fields', 'data_fields', 'effect_def_fields']:
                if field_key not in h_data or not isinstance(h_data[field_key], dict):
                    continue
                for offset, field_info in h_data[field_key].items():
                    if isinstance(offset, str) and ('0x' in offset or offset.startswith('+')):
                        add_field(norm_name, offset, field_info)

    # Format 13: Single handler at top level with handler_name field
    if 'handler_name' in data:
        h_name = data['handler_name']
        norm_name = normalize_handler_name(h_name.split('.')[-1])

        # Check various field key patterns
        field_keys = ['handler_fields', 'config_fields', 'effect_fields', 'fields',
                     'template_fields', 'data_fields', 'effect_def_fields',
                     'config_fields_starting_0x58', 'fields_analyzed']
        for field_key in field_keys:
            if field_key not in data:
                continue
            field_data = data[field_key]
            if isinstance(field_data, dict):
                for offset, field_info in field_data.items():
                    if isinstance(offset, str) and ('0x' in offset or offset.startswith('+')):
                        add_field(norm_name, offset, field_info)
            elif isinstance(field_data, list):
                for item in field_data:
                    if isinstance(item, dict):
                        offset = item.get('offset', '')
                        if isinstance(offset, str) and '0x' in offset:
                            clean_info = {k: v for k, v in item.items() if k != 'offset'}
                            add_field(norm_name, offset, clean_info)

    # Format 14: analyzed_handlers list (from skill_trigger analysis)
    if 'analyzed_handlers' in data and isinstance(data['analyzed_handlers'], list):
        for h_entry in data['analyzed_handlers']:
            if not isinstance(h_entry, dict):
                continue
            h_name = h_entry.get('name', h_entry.get('handler_name', ''))
            if not h_name:
                continue
            norm_name = normalize_handler_name(h_name.split('.')[-1])

            field_keys = ['effect_def_fields', 'effect_data_fields', 'template_fields',
                         'data_fields', 'fields', 'effect_fields', 'handler_fields']
            for field_key in field_keys:
                if field_key not in h_entry:
                    continue
                field_data = h_entry[field_key]
                if isinstance(field_data, dict):
                    for offset, field_info in field_data.items():
                        if isinstance(offset, str) and '0x' in offset:
                            add_field(norm_name, offset, field_info)
                elif isinstance(field_data, list):
                    for item in field_data:
                        if isinstance(item, dict) and 'offset' in item:
                            offset = item['offset']
                            if isinstance(offset, str) and '0x' in offset:
                                clean_info = {k: v for k, v in item.items() if k != 'offset'}
                                add_field(norm_name, offset, clean_info)

    return fields

def normalize_offset(offset):
    """Normalize offset to consistent format (0xNN uppercase)."""
    if isinstance(offset, str):
        offset = offset.strip()
        # Handle "+0x58" format
        if offset.startswith('+'):
            offset = offset[1:]
        offset = offset.lower()
        if offset.startswith('0x'):
            # Parse and reformat
            try:
                val = int(offset, 16)
                return f"0x{val:02X}"
            except:
                return offset.upper()
    return offset

def update_schema(schema, all_fields):
    """Update schema effect_handlers with extracted field descriptions."""
    updates = 0
    handlers_updated = set()

    if 'effect_handlers' not in schema:
        print("Warning: No effect_handlers section in schema")
        return 0

    for handler_name, handler_data in schema['effect_handlers'].items():
        if 'fields' not in handler_data:
            continue

        # Try to find matching analysis data
        matching_analysis = None
        for analysis_name in all_fields.keys():
            if analysis_name.lower() == handler_name.lower():
                matching_analysis = all_fields[analysis_name]
                break
            # Try without common prefixes/suffixes
            if normalize_handler_name(analysis_name).lower() == handler_name.lower():
                matching_analysis = all_fields[analysis_name]
                break

        if not matching_analysis:
            continue

        for field in handler_data['fields']:
            field_offset = normalize_offset(field.get('offset', ''))
            field_name = field.get('name', '')

            # Try to match by offset
            for analysis_offset, analysis_field in matching_analysis.items():
                norm_analysis_offset = normalize_offset(analysis_offset)

                if norm_analysis_offset == field_offset:
                    # Found a match by offset
                    new_desc = None
                    if isinstance(analysis_field, dict):
                        new_desc = analysis_field.get('description', '')
                    elif isinstance(analysis_field, str):
                        new_desc = analysis_field

                    if new_desc:
                        old_desc = field.get('description', '')
                        # Update if new description is better
                        if not old_desc or (len(new_desc) > len(old_desc) and 'ghidra' not in old_desc.lower()):
                            field['description'] = new_desc
                            updates += 1
                            handlers_updated.add(handler_name)
                    break

            # Also try to match by name if offset didn't match
            for analysis_offset, analysis_field in matching_analysis.items():
                if isinstance(analysis_field, dict):
                    analysis_name = analysis_field.get('name', '')
                    if analysis_name.lower() == field_name.lower():
                        new_desc = analysis_field.get('description', '')
                        if new_desc:
                            old_desc = field.get('description', '')
                            if not old_desc or len(new_desc) > len(old_desc):
                                field['description'] = new_desc
                                updates += 1
                                handlers_updated.add(handler_name)
                        break

    print(f"  Updated {len(handlers_updated)} handlers: {', '.join(sorted(handlers_updated)[:10])}{'...' if len(handlers_updated) > 10 else ''}")
    return updates

def parse_confidence(val):
    """Parse confidence value from various formats."""
    if isinstance(val, (int, float)):
        return float(val)
    if isinstance(val, str):
        val_lower = val.lower().strip()
        # Handle text confidence levels
        if val_lower in ('high', 'verified', 'confirmed'):
            return 0.95
        if val_lower in ('medium', 'likely'):
            return 0.75
        if val_lower in ('low', 'uncertain'):
            return 0.5
        # Try to parse as number
        try:
            return float(val)
        except ValueError:
            return 0.85
    return 0.85


def update_knowledge_base(kb, all_fields):
    """Update eventhandler_knowledge.json with analysis findings.

    MERGE strategy: Only add NEW fields or upgrade LOW confidence entries.
    Preserves existing high-confidence entries and their extra metadata.
    """
    updates = 0
    added = 0
    upgraded = 0

    if 'handlers' not in kb:
        kb['handlers'] = {}

    for handler_name, fields in all_fields.items():
        if handler_name not in kb['handlers']:
            kb['handlers'][handler_name] = {}

        for offset, field_info in fields.items():
            if isinstance(field_info, dict):
                field_name = field_info.get('name', f'field_{offset}')
                new_description = field_info.get('description', '')
                new_confidence = field_info.get('confidence', 0.85)

                if not new_description:
                    continue

                existing = kb['handlers'][handler_name].get(field_name)

                if existing is None:
                    # New field - add it
                    new_conf_val = parse_confidence(new_confidence)
                    kb['handlers'][handler_name][field_name] = {
                        'offset': normalize_offset(offset),
                        'description': new_description,
                        'confidence': new_conf_val,
                        'source': 'ghidra_analysis',
                    }
                    if 'type' in field_info:
                        kb['handlers'][handler_name][field_name]['type'] = field_info['type']
                    # Copy extra fields like enum_values, formula, verified_in, etc.
                    for extra_key in ['enum_values', 'formula', 'verified_in', 'maps_to_entityprops',
                                     'value_range', 'default', 'note', 'category']:
                        if extra_key in field_info:
                            kb['handlers'][handler_name][field_name][extra_key] = field_info[extra_key]
                    added += 1
                    updates += 1
                else:
                    # Existing field - only upgrade if new data is better
                    existing_confidence = parse_confidence(existing.get('confidence', 0.3))
                    existing_source = existing.get('source', 'name_inference')
                    new_confidence_f = parse_confidence(new_confidence)

                    # Upgrade if: new confidence higher, OR existing is low-confidence inference
                    should_upgrade = (
                        (new_confidence_f > existing_confidence) or
                        (existing_source == 'name_inference' and new_confidence_f >= 0.8)
                    )

                    if should_upgrade:
                        # Merge: keep existing extra fields, update core fields
                        existing['description'] = new_description
                        existing['confidence'] = max(new_confidence_f, existing_confidence)
                        existing['source'] = 'ghidra_analysis'
                        if 'type' in field_info:
                            existing['type'] = field_info['type']
                        # Add any new extra fields without overwriting existing
                        for extra_key in ['enum_values', 'formula', 'verified_in', 'maps_to_entityprops',
                                         'value_range', 'default', 'note', 'category']:
                            if extra_key in field_info and extra_key not in existing:
                                existing[extra_key] = field_info[extra_key]
                        upgraded += 1
                        updates += 1

    print(f"  Added {added} new fields, upgraded {upgraded} existing fields")
    return updates

def extract_verified_fields(data):
    """Extract fields from VERIFIED_FIELDS.json format with high confidence."""
    fields = {}

    # Format: attack_effect_fields with nested field objects
    for section_key, section in data.items():
        if not isinstance(section, dict) or section_key in ['version', 'generated', 'source', 'description']:
            continue

        # Check if this is a fields section (has offset, type, confidence)
        for field_name, field_data in section.items():
            if not isinstance(field_data, dict):
                continue
            if 'offset' in field_data and 'confidence' in field_data:
                # This is a verified field entry
                handler_name = 'Attack'  # For ATTACK_HANDLER_VERIFIED_FIELDS.json
                if handler_name not in fields:
                    fields[handler_name] = {}
                fields[handler_name][field_data['offset']] = {
                    'name': field_name,
                    'description': field_data.get('description', ''),
                    'type': field_data.get('type', ''),
                    'confidence': field_data.get('confidence', 0.95),
                    'verified_in': field_data.get('verified_in', []),
                    'formula': field_data.get('formula', ''),
                    'maps_to_entityprops': field_data.get('maps_to_entityprops', ''),
                    'value_range': field_data.get('value_range', ''),
                    'default': field_data.get('default'),
                }

    return fields


def main():
    # Paths
    docs_dir = Path('/home/poss/Documents/Code/Menace/MenaceAssetPacker/docs')
    schema_path = Path('/home/poss/Documents/Code/Menace/MenaceAssetPacker/src/Menace.Modkit.App/bin/Debug/net10.0/schema.json')
    kb_path = Path('/home/poss/Documents/Code/Menace/MenaceAssetPacker/eventhandler_knowledge.json')

    print("=" * 60)
    print("Consolidating Ghidra Analysis to Schema")
    print("=" * 60)

    # Collect all field data from analysis files
    all_fields = {}
    files_processed = 0

    print(f"\nScanning {docs_dir} for analysis JSON files...")

    # FIRST: Process verified fields (highest priority, highest confidence)
    verified_files = list(docs_dir.glob('*VERIFIED*.json'))
    for vf in verified_files:
        try:
            with open(vf, 'r') as f:
                data = json.load(f)
            fields = extract_verified_fields(data)
            if fields:
                for h_name, h_fields in fields.items():
                    if h_name not in all_fields:
                        all_fields[h_name] = {}
                    all_fields[h_name].update(h_fields)
                print(f"  ★ {vf.name}: {sum(len(f) for f in fields.values())} verified fields (HIGH CONFIDENCE)")
                files_processed += 1
        except Exception as e:
            print(f"  ✗ {vf.name}: {e}")

    print(f"\nScanning regular analysis JSON files...")

    for json_file in docs_dir.rglob('*.json'):
        try:
            with open(json_file, 'r') as f:
                data = json.load(f)

            # Extract handler name from filename as fallback
            handler_name = json_file.stem.replace('_analysis', '').replace('_ANALYSIS', '')
            handler_name = handler_name.replace('EVENTHANDLERS', '').replace('HANDLERS', '')
            handler_name = handler_name.replace('_', ' ').title().replace(' ', '')

            fields = extract_fields_from_analysis(data, handler_name)

            if fields:
                for h_name, h_fields in fields.items():
                    if h_name not in all_fields:
                        all_fields[h_name] = {}
                    all_fields[h_name].update(h_fields)
                files_processed += 1
                print(f"  ✓ {json_file.name}: {len(fields)} handlers")
        except json.JSONDecodeError as e:
            print(f"  ✗ {json_file.name}: JSON parse error - {e}")
        except Exception as e:
            print(f"  ✗ {json_file.name}: {e}")

    print(f"\nProcessed {files_processed} files, found {len(all_fields)} unique handlers")

    if not all_fields:
        print("No handler data extracted. Check JSON file formats.")
        return 1

    # Show what we found
    print("\nExtracted handlers:")
    for h_name, h_fields in sorted(all_fields.items())[:20]:
        print(f"  {h_name}: {len(h_fields)} fields")
    if len(all_fields) > 20:
        print(f"  ... and {len(all_fields) - 20} more")

    # Load and update schema
    print(f"\nLoading schema from {schema_path}...")
    with open(schema_path, 'r') as f:
        schema = json.load(f)

    print("Updating schema with analysis findings...")
    schema_updates = update_schema(schema, all_fields)

    # Load and update knowledge base
    print(f"\nLoading knowledge base from {kb_path}...")
    if kb_path.exists():
        with open(kb_path, 'r') as f:
            kb = json.load(f)
    else:
        kb = {'version': '1.0', 'handlers': {}}

    print("Updating knowledge base...")
    kb_updates = update_knowledge_base(kb, all_fields)

    # Save updates
    if schema_updates > 0:
        print(f"\nSaving schema ({schema_updates} field descriptions updated)...")
        with open(schema_path, 'w') as f:
            json.dump(schema, f, indent=2, ensure_ascii=False)

        # Also sync to other schema locations
        sync_paths = [
            Path('/home/poss/Documents/Code/Menace/MenaceAssetPacker/schema.json'),
            Path('/home/poss/Documents/Code/Menace/MenaceAssetPacker/generated/schema.json'),
        ]
        for sync_path in sync_paths:
            if sync_path.parent.exists():
                with open(sync_path, 'w') as f:
                    json.dump(schema, f, indent=2, ensure_ascii=False)
                print(f"  Synced to {sync_path}")

    if kb_updates > 0:
        kb['updated'] = datetime.now().isoformat()
        print(f"Saving knowledge base ({kb_updates} entries updated)...")
        with open(kb_path, 'w') as f:
            json.dump(kb, f, indent=2, ensure_ascii=False)

    # Summary
    print("\n" + "=" * 60)
    print("SUMMARY")
    print("=" * 60)
    print(f"Analysis files processed: {files_processed}")
    print(f"Handlers found: {len(all_fields)}")
    print(f"Schema descriptions updated: {schema_updates}")
    print(f"Knowledge base entries updated: {kb_updates}")

    return 0

if __name__ == '__main__':
    sys.exit(main())
