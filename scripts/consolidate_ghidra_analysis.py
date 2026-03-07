#!/usr/bin/env python3
"""
Consolidate Ghidra analysis results from multiple agents and update schema.

Reads JSON results from agent output files and:
1. Updates field descriptions in schema.json
2. Updates eventhandler_knowledge.json with high-confidence findings
3. Generates a summary report
"""

import json
import re
import sys
from pathlib import Path
from datetime import datetime

def extract_json_from_output(output_path):
    """Extract JSON object from agent output file."""
    try:
        with open(output_path, 'r') as f:
            content = f.read()

        # Look for JSON object in the output
        # Agents should output a JSON block
        json_pattern = r'\{[^{}]*"(?:handler|interface)"[^{}]*\}'

        # Try to find complete JSON objects
        matches = []
        brace_count = 0
        start_idx = None

        for i, char in enumerate(content):
            if char == '{':
                if brace_count == 0:
                    start_idx = i
                brace_count += 1
            elif char == '}':
                brace_count -= 1
                if brace_count == 0 and start_idx is not None:
                    try:
                        json_str = content[start_idx:i+1]
                        obj = json.loads(json_str)
                        if 'handler' in obj or 'interface' in obj or 'fields_analyzed' in obj:
                            matches.append(obj)
                    except:
                        pass
                    start_idx = None

        return matches
    except Exception as e:
        print(f"Error reading {output_path}: {e}")
        return []

def update_schema_with_findings(schema, findings):
    """Update schema.json with analysis findings."""
    updates = 0

    for finding in findings:
        handler_name = finding.get('handler')
        if not handler_name:
            continue

        if handler_name not in schema.get('effect_handlers', {}):
            print(f"  Warning: Handler {handler_name} not in schema")
            continue

        handler_data = schema['effect_handlers'][handler_name]
        fields_by_name = {f['name']: f for f in handler_data.get('fields', [])}

        for field_analysis in finding.get('fields_analyzed', []):
            field_name = field_analysis.get('name')
            new_desc = field_analysis.get('description', '')
            confidence = field_analysis.get('confidence', 0)

            if field_name not in fields_by_name:
                continue

            field = fields_by_name[field_name]
            old_desc = field.get('description', '')

            # Update if new description is better (higher confidence or more detailed)
            if new_desc and (confidence > 0.5 or len(new_desc) > len(old_desc)):
                field['description'] = new_desc
                updates += 1
                print(f"  Updated {handler_name}.{field_name}: {new_desc[:60]}...")

    return updates

def update_knowledge_base(kb, findings):
    """Update eventhandler_knowledge.json with findings."""
    updates = 0

    for finding in findings:
        handler_name = finding.get('handler')
        if not handler_name:
            continue

        if handler_name not in kb['handlers']:
            kb['handlers'][handler_name] = {}

        for field_analysis in finding.get('fields_analyzed', []):
            field_name = field_analysis.get('name')
            if not field_name:
                continue

            confidence = field_analysis.get('confidence', 0.6)

            kb['handlers'][handler_name][field_name] = {
                'description': field_analysis.get('description', ''),
                'confidence': confidence,
                'source': 'ghidra_analysis',
                'code_evidence': field_analysis.get('code_evidence', ''),
                'analyzed_at': datetime.now().isoformat()
            }

            # Preserve offset/type if present
            if 'offset' in field_analysis:
                kb['handlers'][handler_name][field_name]['offset'] = field_analysis['offset']

            updates += 1

    return updates

def generate_report(findings):
    """Generate a markdown summary report."""
    lines = [
        "# Ghidra Analysis Report",
        f"\n**Generated:** {datetime.now().isoformat()}",
        f"\n**Handlers Analyzed:** {len(findings)}",
        "\n---\n"
    ]

    total_fields = 0
    total_comments = 0

    for finding in findings:
        handler = finding.get('handler', finding.get('interface', 'Unknown'))
        fields = finding.get('fields_analyzed', [])
        comments = finding.get('ghidra_comments_added', [])

        lines.append(f"\n## {handler}")
        lines.append(f"\n**Fields analyzed:** {len(fields)}")
        lines.append(f"**Ghidra comments added:** {len(comments)}")

        if fields:
            lines.append("\n### Fields\n")
            for field in fields:
                name = field.get('name', 'Unknown')
                desc = field.get('description', 'No description')
                conf = field.get('confidence', 0)
                lines.append(f"- **{name}** ({conf:.0%}): {desc}")

        total_fields += len(fields)
        total_comments += len(comments)

    lines.insert(4, f"**Total fields documented:** {total_fields}")
    lines.insert(5, f"**Total Ghidra comments:** {total_comments}")

    return '\n'.join(lines)

def main():
    # Paths
    task_dir = Path('/tmp/claude/-home-poss-Documents-Code-Menace-MenaceAssetPacker/tasks')
    schema_path = Path('src/Menace.Modkit.App/bin/Debug/net10.0/schema.json')
    kb_path = Path('eventhandler_knowledge.json')
    report_path = Path('ghidra_analysis_report.md')

    # Allow override
    if len(sys.argv) > 1:
        task_dir = Path(sys.argv[1])

    if not task_dir.exists():
        print(f"Task directory not found: {task_dir}")
        sys.exit(1)

    # Collect all findings from agent outputs
    print("Reading agent outputs...")
    all_findings = []

    for output_file in task_dir.glob('*.output'):
        findings = extract_json_from_output(output_file)
        if findings:
            print(f"  Found {len(findings)} results in {output_file.name}")
            all_findings.extend(findings)

    if not all_findings:
        print("No analysis results found. Agents may still be running.")
        sys.exit(0)

    print(f"\nTotal findings: {len(all_findings)}")

    # Load schema
    print(f"\nLoading schema from {schema_path}...")
    with open(schema_path, 'r') as f:
        schema = json.load(f)

    # Load knowledge base
    print(f"Loading knowledge base from {kb_path}...")
    if kb_path.exists():
        with open(kb_path, 'r') as f:
            kb = json.load(f)
    else:
        kb = {'version': '1.0', 'handlers': {}}

    # Update schema
    print("\nUpdating schema with findings...")
    schema_updates = update_schema_with_findings(schema, all_findings)

    # Update knowledge base
    print("\nUpdating knowledge base...")
    kb_updates = update_knowledge_base(kb, all_findings)

    # Generate report
    print("\nGenerating report...")
    report = generate_report(all_findings)

    # Save everything
    if schema_updates > 0:
        print(f"\nSaving schema ({schema_updates} updates)...")
        with open(schema_path, 'w') as f:
            json.dump(schema, f, indent=2, ensure_ascii=False)

    if kb_updates > 0:
        kb['updated'] = datetime.now().isoformat()
        print(f"Saving knowledge base ({kb_updates} updates)...")
        with open(kb_path, 'w') as f:
            json.dump(kb, f, indent=2, ensure_ascii=False)

    print(f"Saving report to {report_path}...")
    with open(report_path, 'w') as f:
        f.write(report)

    print("\n" + "="*60)
    print("SUMMARY")
    print("="*60)
    print(f"Handlers analyzed: {len(all_findings)}")
    print(f"Schema fields updated: {schema_updates}")
    print(f"Knowledge base entries: {kb_updates}")
    print(f"Report: {report_path}")

if __name__ == '__main__':
    main()
