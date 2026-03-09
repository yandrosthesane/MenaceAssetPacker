using System;
using System.Collections.Generic;
using System.Linq;

namespace Menace.Modkit.App.Services;

/// <summary>
/// Groups template fields into logical categories for UI organization.
/// Fields are grouped by keyword patterns in their names.
/// </summary>
public static class FieldGroupingService
{
    /// <summary>
    /// Defines a field group with display name and matching keywords.
    /// </summary>
    public class FieldGroup
    {
        public string Name { get; init; } = "";
        public string[] Keywords { get; init; } = Array.Empty<string>();
        public int Priority { get; init; } // Lower = appears first among groups
    }

    // Group definitions - order matters for priority when a field matches multiple groups
    private static readonly FieldGroup[] _groups = new[]
    {
        new FieldGroup
        {
            Name = "Range",
            Keywords = new[] { "range", "minrange", "maxrange", "idealrange", "distance" },
            Priority = 1
        },
        new FieldGroup
        {
            Name = "Area of Effect",
            Keywords = new[] { "aoe", "area", "radius", "blast", "explosion" },
            Priority = 2
        },
        new FieldGroup
        {
            Name = "Projectile & Impact",
            Keywords = new[] { "projectile", "muzzle", "impact", "scatter", "spread", "ricochet", "trajectory" },
            Priority = 3
        },
        new FieldGroup
        {
            Name = "Sounds",
            Keywords = new[] { "sound", "audio", "sfx" },
            Priority = 4
        },
        new FieldGroup
        {
            Name = "Animation & Visual",
            Keywords = new[] { "animation", "anim", "visual", "effect", "camera", "decal", "particle" },
            Priority = 5
        },
        new FieldGroup
        {
            Name = "Icons & UI",
            Keywords = new[] { "icon", "sprite", "image", "thumbnail", "viewelement" },
            Priority = 6
        },
        new FieldGroup
        {
            Name = "Cost & Requirements",
            Keywords = new[] { "cost", "actionpoint", "apcost", "require", "cooldown", "ammo", "charge" },
            Priority = 7
        },
    };

    // Template types that should use field grouping (long templates)
    private static readonly HashSet<string> _groupableTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        "SkillTemplate",
        "WeaponTemplate",
        "EquipmentTemplate",
        "AbilityTemplate",
        "TacticalAbilityTemplate",
        "PerkTemplate",
        "StatusEffectTemplate",
        "TileEffectTemplate",
        "EntityTemplate",
        "VehicleTemplate",
        "ModularVehicleWeaponTemplate",
    };

    // Minimum number of fields in a group to make it worth collapsing
    private const int MinGroupSize = 3;

    // Minimum total fields before we start grouping
    private const int MinFieldsForGrouping = 15;

    /// <summary>
    /// Check if a template type should use field grouping.
    /// </summary>
    public static bool ShouldGroupFields(string templateTypeName)
    {
        return _groupableTemplates.Contains(templateTypeName);
    }

    /// <summary>
    /// Group fields by category.
    /// Returns (ungrouped fields, grouped fields by category name).
    /// </summary>
    public static (List<KeyValuePair<string, object?>> Ungrouped,
                   Dictionary<string, List<KeyValuePair<string, object?>>> Grouped)
        GroupFields(Dictionary<string, object?> fields, string templateTypeName)
    {
        var ungrouped = new List<KeyValuePair<string, object?>>();
        var grouped = new Dictionary<string, List<KeyValuePair<string, object?>>>();

        // Don't group if template type isn't groupable or too few fields
        if (!ShouldGroupFields(templateTypeName) || fields.Count < MinFieldsForGrouping)
        {
            return (fields.ToList(), grouped);
        }

        // First pass: categorize all fields
        var fieldCategories = new Dictionary<string, string>(); // fieldName -> groupName
        foreach (var kvp in fields)
        {
            var fieldName = kvp.Key;
            // Skip dotted paths (nested object subfields) - they stay with their parent
            if (fieldName.Contains('.'))
            {
                continue;
            }

            var group = FindGroupForField(fieldName);
            if (group != null)
            {
                fieldCategories[fieldName] = group.Name;
            }
        }

        // Count fields per group
        var groupCounts = fieldCategories.Values
            .GroupBy(g => g)
            .ToDictionary(g => g.Key, g => g.Count());

        // Second pass: assign to ungrouped or grouped based on group size
        foreach (var kvp in fields)
        {
            var fieldName = kvp.Key;

            // For dotted paths, check if parent is in a viable group
            var baseName = fieldName.Contains('.') ? fieldName[..fieldName.IndexOf('.')] : fieldName;

            if (fieldCategories.TryGetValue(baseName, out var groupName) &&
                groupCounts.TryGetValue(groupName, out var count) &&
                count >= MinGroupSize)
            {
                // Add to group
                if (!grouped.TryGetValue(groupName, out var list))
                {
                    list = new List<KeyValuePair<string, object?>>();
                    grouped[groupName] = list;
                }
                list.Add(kvp);
            }
            else
            {
                ungrouped.Add(kvp);
            }
        }

        return (ungrouped, grouped);
    }

    /// <summary>
    /// Find which group a field belongs to based on its name.
    /// </summary>
    private static FieldGroup? FindGroupForField(string fieldName)
    {
        var nameLower = fieldName.ToLowerInvariant();

        foreach (var group in _groups)
        {
            foreach (var keyword in group.Keywords)
            {
                if (nameLower.Contains(keyword))
                {
                    return group;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Get the sort priority for a group name.
    /// </summary>
    public static int GetGroupPriority(string groupName)
    {
        var group = _groups.FirstOrDefault(g => g.Name == groupName);
        return group?.Priority ?? 99;
    }
}
