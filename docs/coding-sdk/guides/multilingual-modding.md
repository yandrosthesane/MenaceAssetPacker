# Multilingual Modding Guide

**Status:** ✅ Implemented and available in ModpackLoader
**Purpose:** Create mods that support multiple languages without relying on defaults

---

## Available Languages (Important!)

**Base Game Ships With (10 languages):**
- English, German, French, Russian
- ChineseSimplified, ChineseTraditional
- Japanese, Korean, Polish, Turkish

**NOT Available in Base Game:**
- Spanish, Italian, Portuguese *(may be DLC/regional releases)*

⚠️ **Note:** Examples in this guide may reference Spanish/Italian/Portuguese for illustration, but these won't work in the base game. Use German, French, or Russian instead for testing.

---

## Overview

The Multi-Lingual Localization system allows you to:
- **View translations in ALL languages simultaneously** (no need to switch game language)
- **Edit specific language translations** (German, French, Russian, etc.)
- **Export/import CSV files** for translator workflows
- **Validate translations** (find missing keys, check completeness)
- **Support multilingual communities** (both modders and players speak many languages)

**Key Benefit:** Instead of creating English-only mods and hoping for fallbacks, you can provide complete translations for all supported languages.

---

## Quick Start

### View All Translations for a Key

```csharp
using Menace.SDK;

// Get weapon name in all languages
var translations = MultiLingualLocalization.GetAllTranslations("weapons", "weapons.assault_rifle.name");

foreach (var (language, text) in translations)
{
    Debug.Log($"{language}: {text}");
}

// Output:
// English: Assault Rifle
// Spanish: Fusil de Asalto
// German: Sturmgewehr
// French: Fusil d'Assaut
// Italian: Fucile d'Assalto
// Portuguese: Rifle de Assalto
// Russian: Штурмовая винтовка

// Available language constants:
// MultiLingualLocalization.Languages.English
// MultiLingualLocalization.Languages.Spanish
// MultiLingualLocalization.Languages.German
// etc.
```

### Set Translation for Specific Language

```csharp
using Menace.SDK;

// Update Spanish translation for custom weapon
MultiLingualLocalization.SetTranslation(
    MultiLingualLocalization.Languages.Spanish,
    "weapons",
    "weapons.my_custom_rifle.name",
    "Mi Rifle Personalizado"
);

// Update German translation
MultiLingualLocalization.SetTranslation(
    MultiLingualLocalization.Languages.German,
    "weapons",
    "weapons.my_custom_rifle.name",
    "Mein Benutzerdefiniertes Gewehr"
);
```

### Export Translations for Translators

```csharp
// Export Spanish translations to CSV file for translator
MultiLingualLocalization.ExportLanguage(
    MultiLingualLocalization.Languages.Spanish,
    "MyModpack/Localization/Spanish_ToTranslate.csv"
);

// Translator edits the CSV file...

// Import updated translations back
MultiLingualLocalization.ImportLanguage(
    MultiLingualLocalization.Languages.Spanish,
    "MyModpack/Localization/Spanish_Translated.csv"
);
```

---

## Architecture

### How Game Localization Works

**Single Language Loading (Default):**
1. Player selects language (or system default)
2. Game loads ONE CSV file: `English.csv`, `Spanish.csv`, etc.
3. Templates reference keys: `{ m_Key: "weapons.rifle.name", m_TableID: "weapons" }`
4. Game looks up key in loaded language
5. Falls back to English if translation missing

**Problem for Modders:**
- Can't see Spanish translation while game is in English
- Must switch language to verify each translation
- Difficult to provide complete translations

### Multi-Lingual System (New)

**Loads ALL Languages:**
1. On startup, load ALL language CSV files (English, Spanish, German, etc.)
2. Store in memory: `Language → Category → Key → Translation`
3. Provide API to access ANY language at ANY time
4. No need to switch game language

**Benefits:**
- View all translations side-by-side
- Edit Spanish while game runs in English
- Validate completeness (find missing keys)
- Export/import for translator workflows

---

## API Reference

### Core Methods

#### Get Translation (Single Language)

```csharp
string translation = MultiLingualLocalization.GetTranslation(
    MultiLingualLocalization.Languages.Spanish,
    "weapons",                              // Category
    "weapons.assault_rifle.name"           // Key
);
// Returns: "Fusil de Asalto"
```

#### Get All Translations (All Languages)

```csharp
Dictionary<LocaLanguage, string> translations =
    MultiLingualLocalization.GetAllTranslations(
        "weapons",
        "weapons.assault_rifle.name"
    );

// Returns dictionary with all available languages
```

#### Set Translation

```csharp
MultiLingualLocalization.SetTranslation(
    MultiLingualLocalization.Languages.French,
    "weapons",
    "weapons.my_gun.name",
    "Mon Fusil"
);
```

#### Check if Key Exists

```csharp
bool exists = MultiLingualLocalization.HasKey(
    MultiLingualLocalization.Languages.German,
    "weapons",
    "weapons.assault_rifle.name"
);
```

### Discovery Methods

#### List Available Languages

```csharp
LocaLanguage[] languages = MultiLingualLocalization.GetAvailableLanguages();
// Returns: [English, Spanish, German, French, Italian, Portuguese, Russian, ...]
```

#### List Categories in Language

```csharp
string[] categories = MultiLingualLocalization.GetCategories(MultiLingualLocalization.Languages.English);
// Returns: ["weapons", "skills", "perks", "items", "missions", ...]
```

#### List Keys in Category

```csharp
string[] keys = MultiLingualLocalization.GetKeys(
    MultiLingualLocalization.Languages.English,
    "weapons"
);
// Returns: ["weapons.assault_rifle.name", "weapons.pistol.name", ...]
```

### Import/Export Methods

#### Export Language to CSV

```csharp
MultiLingualLocalization.ExportLanguage(
    MultiLingualLocalization.Languages.Spanish,
    "Output/Spanish.csv"
);
```

#### Import Language from CSV

```csharp
MultiLingualLocalization.ImportLanguage(
    MultiLingualLocalization.Languages.Spanish,
    "Input/Spanish_Updated.csv"
);
```

### Statistics

#### Get System Statistics

```csharp
string stats = MultiLingualLocalization.GetStatistics();
Debug.Log(stats);

// Output:
// === Multi-Lingual Localization Statistics ===
// Languages loaded: 7
//
// English: 12 categories, 2341 keys
//   - weapons: 245 keys
//   - skills: 189 keys
//   - perks: 156 keys
// ...
```

---

## MCP Tools (For External Tools)

All functionality is exposed via MCP HTTP server for external tooling (Python scripts, web UIs, etc.).

### get_all_translations

**Get translations for a key in ALL languages.**

```bash
curl -X POST http://localhost:7655/get_all_translations \
  -H "Content-Type: application/json" \
  -d '{
    "category": "weapons",
    "key": "weapons.assault_rifle.name"
  }'
```

Response:
```json
{
  "success": true,
  "category": "weapons",
  "key": "weapons.assault_rifle.name",
  "translations": {
    "English": "Assault Rifle",
    "Spanish": "Fusil de Asalto",
    "German": "Sturmgewehr",
    "French": "Fusil d'Assaut"
  },
  "languageCount": 4
}
```

### get_translation

**Get translation for specific language.**

```bash
curl -X POST http://localhost:7655/get_translation \
  -d '{"language": "Spanish", "category": "weapons", "key": "weapons.assault_rifle.name"}'
```

### set_translation

**Update translation for specific language.**

```bash
curl -X POST http://localhost:7655/set_translation \
  -d '{
    "language": "Spanish",
    "category": "weapons",
    "key": "weapons.my_rifle.name",
    "text": "Mi Rifle"
  }'
```

### list_localization_languages

**List all available languages.**

```bash
curl -X POST http://localhost:7655/list_localization_languages
```

### list_localization_categories

**List categories in a language.**

```bash
curl -X POST http://localhost:7655/list_localization_categories \
  -d '{"language": "English"}'
```

### list_localization_keys

**List keys in a category.**

```bash
curl -X POST http://localhost:7655/list_localization_keys \
  -d '{"language": "English", "category": "weapons", "limit": 50}'
```

### export_localization

**Export language to CSV.**

```bash
curl -X POST http://localhost:7655/export_localization \
  -d '{"language": "Spanish", "outputPath": "Spanish.csv"}'
```

### import_localization

**Import language from CSV.**

```bash
curl -X POST http://localhost:7655/import_localization \
  -d '{"language": "Spanish", "inputPath": "Spanish_Updated.csv"}'
```

### find_missing_translations

**Find keys that exist in source language but missing in target.**

```bash
curl -X POST http://localhost:7655/find_missing_translations \
  -d '{
    "sourceLanguage": "English",
    "targetLanguage": "Spanish",
    "category": "weapons"
  }'
```

Response:
```json
{
  "success": true,
  "sourceLanguage": "English",
  "targetLanguage": "Spanish",
  "missingCount": 12,
  "missingKeys": [
    {
      "category": "weapons",
      "key": "weapons.new_rifle.name",
      "sourceText": "New Rifle"
    }
  ]
}
```

---

## Workflows

### Workflow 1: Create Fully Translated Custom Weapon

```csharp
// 1. Create weapon template (English)
var weaponJson = new JObject
{
    ["name"] = "weapon.my_heavy_cannon",
    ["m_Damage"] = 25,
    ["m_LocaState"] = new JObject
    {
        ["m_Key"] = "weapons.my_heavy_cannon.name",
        ["m_TableID"] = "weapons"
    }
};

// 2. Add English translation
MultiLingualLocalization.SetTranslation(
    MultiLingualLocalization.Languages.English,
    "weapons",
    "weapons.my_heavy_cannon.name",
    "Heavy Cannon"
);

// 3. Add Spanish translation
MultiLingualLocalization.SetTranslation(
    MultiLingualLocalization.Languages.Spanish,
    "weapons",
    "weapons.my_heavy_cannon.name",
    "Cañón Pesado"
);

// 4. Add German translation
MultiLingualLocalization.SetTranslation(
    MultiLingualLocalization.Languages.German,
    "weapons",
    "weapons.my_heavy_cannon.name",
    "Schwere Kanone"
);

// 5. Add French translation
MultiLingualLocalization.SetTranslation(
    MultiLingualLocalization.Languages.French,
    "weapons",
    "weapons.my_heavy_cannon.name",
    "Canon Lourd"
);

// Now weapon shows correctly in ALL languages!
```

### Workflow 2: Send CSV to Translators

```csharp
// 1. Export base game Spanish translations for reference
MultiLingualLocalization.ExportLanguage(
    MultiLingualLocalization.Languages.Spanish,
    "Reference/BaseGame_Spanish.csv"
);

// 2. Create CSV with your mod's English keys
// (Use template or manually create)

// 3. Send to Spanish translator
// They fill in translations using the base game as reference

// 4. Import translated CSV
MultiLingualLocalization.ImportLanguage(
    MultiLingualLocalization.Languages.Spanish,
    "MyModpack/Spanish_Translated.csv"
);

// 5. Verify completeness
var missing = FindMissingKeys(MultiLingualLocalization.Languages.English, MultiLingualLocalization.Languages.Spanish);
if (missing.Count > 0)
{
    Debug.LogWarning($"Spanish translation incomplete: {missing.Count} keys missing");
}
```

### Workflow 3: Validate All Translations

```csharp
// Check which keys are missing in each language
var baseLanguage = MultiLingualLocalization.Languages.English;
var otherLanguages = MultiLingualLocalization.GetAvailableLanguages()
    .Where(l => l != baseLanguage);

foreach (var language in otherLanguages)
{
    var missing = FindMissingKeys(baseLanguage, language);
    Debug.Log($"{language}: {missing.Count} keys missing");

    foreach (var key in missing.Take(5))
    {
        Debug.Log($"  - {key}");
    }
}

// Helper method
List<string> FindMissingKeys(LocaLanguage source, LocaLanguage target)
{
    var missing = new List<string>();
    var categories = MultiLingualLocalization.GetCategories(source);

    foreach (var category in categories)
    {
        var keys = MultiLingualLocalization.GetKeys(source, category);
        foreach (var key in keys)
        {
            if (!MultiLingualLocalization.HasKey(target, category, key))
            {
                missing.Add(key);
            }
        }
    }

    return missing;
}
```

---

## CSV File Format

### Structure

Localization files are CSV (comma-separated values) with the following columns:

```
Key,Type,Default,Translation
```

- **Key**: Localization key (e.g., "weapons.assault_rifle.name")
- **Type**: Entry type (usually "Text" or empty)
- **Default**: English/default text
- **Translation**: Text in this language (Spanish, German, etc.)

### Example

```csv
Key,Type,Default,Translation
weapons.assault_rifle.name,Text,Assault Rifle,Fusil de Asalto
weapons.assault_rifle.description,Text,"Standard military rifle. Good damage and accuracy.","Fusil militar estándar. Buen daño y precisión."
weapons.pistol.name,Text,Pistol,Pistola
skills.overwatch.name,Text,Overwatch,Vigilancia
```

### Notes

- Use quotes for text containing commas
- Escape quotes by doubling them: `"He said ""Hello"""`
- UTF-8 encoding for special characters (é, ñ, ü, 中, etc.)
- Line breaks in text: Use `\n` in CSV

---

## Best Practices

### 1. Always Provide English

English is the fallback language. Even if your mod is Spanish-only, provide English translations so English players see something meaningful.

```csharp
// ✅ Good: Provide both
SetTranslation(MultiLingualLocalization.Languages.English, "weapons", "my_key", "Heavy Cannon");
SetTranslation(MultiLingualLocalization.Languages.Spanish, "weapons", "my_key", "Cañón Pesado");

// ❌ Bad: Spanish only
SetTranslation(MultiLingualLocalization.Languages.Spanish, "weapons", "my_key", "Cañón Pesado");
// English players see raw key "my_key"
```

### 2. Use Descriptive Keys

Keys should be unique and descriptive:

```csharp
// ✅ Good: Clear and unique
"weapons.plasma_rifle_mk2.name"
"weapons.plasma_rifle_mk2.description"

// ❌ Bad: Vague and likely to collide
"my_weapon.name"
"weapon.text1"
```

### 3. Reuse Base Game Translations When Possible

Don't reinvent the wheel. If base game has "Damage", "Accuracy", etc., reuse those keys:

```csharp
// Check if key exists
if (MultiLingualLocalization.HasKey(MultiLingualLocalization.Languages.Spanish, "ui", "ui.damage_label"))
{
    // Reuse existing translation
    var damageLabel = MultiLingualLocalization.GetTranslation(
        MultiLingualLocalization.Languages.Spanish,
        "ui",
        "ui.damage_label"
    );
    // Use in your UI
}
```

### 4. Test in Multiple Languages

Before releasing, test your mod in at least 2-3 languages:

```csharp
// Switch game language programmatically for testing
LocaManager.Get()?.SetCurrentLanguage(MultiLingualLocalization.Languages.Spanish);

// Verify your custom content displays correctly
// Check for raw keys, broken formatting, etc.
```

### 5. Export CSV for Version Control

Store translations in CSV files, not hardcoded in C#:

```
MyModpack/
  Localization/
    English.csv
    Spanish.csv
    German.csv
    French.csv
```

Benefits:
- Easier to edit (any text editor, Excel, Google Sheets)
- Version control friendly (Git diffs work)
- Translators don't need C# knowledge
- Can be hot-reloaded during development

### 6. Use Translation Tools

Consider using translation memory tools:
- **Google Sheets** - Collaborative editing, auto-translate suggestions
- **Weblate** - Open source translation platform
- **Crowdin** - Professional localization management

Export CSV → Import to tool → Translators work → Export → Import back to mod.

---

## Localization Key Naming Conventions

Follow game's conventions for consistency:

### Category Prefixes

```
weapons.{weapon_name}.{field}       - Weapon names/descriptions
skills.{skill_name}.{field}         - Skill names/descriptions
perks.{perk_name}.{field}           - Perk names/descriptions
items.{item_name}.{field}           - Item names/descriptions
missions.{mission_name}.{field}     - Mission briefings
operations.{operation_name}.{field} - Operation descriptions
ui.{element_name}                   - UI text
conversations.{conv_name}.{line}    - Dialogue lines
```

### Common Field Suffixes

```
.name              - Short name (displayed in lists)
.description       - Full description (tooltips, details)
.flavor_text       - Lore/flavor text
.tooltip           - Tooltip text
.short_description - Brief description (UI cards)
.headline          - Headline/title
.success_text      - Text shown on success
.failure_text      - Text shown on failure
```

### Examples

```
weapons.plasma_cannon.name
weapons.plasma_cannon.description
weapons.plasma_cannon.flavor_text

skills.orbital_strike.name
skills.orbital_strike.description
skills.orbital_strike.tooltip

perks.heavy_weapons_expert.name
perks.heavy_weapons_expert.description
```

---

## Troubleshooting

### Translations Don't Show in Game

**Problem:** You set translations but game still shows English (or raw keys).

**Possible Causes:**

1. **Template not using MultiLingualLocalization system**
   - Game's LocaManager only loads ONE language
   - MultiLingualLocalization is separate (for modders, not game runtime)
   - **Solution:** Export translations to CSV, place in game's Localization folder

2. **Key doesn't match**
   - Template has `m_Key: "weapons.rifle.name"`
   - You set `"weapons.my_rifle.name"`
   - **Solution:** Use exact key from template

3. **Category doesn't match**
   - Template has `m_TableID: "weapons"`
   - You used category `"items"`
   - **Solution:** Use correct category

### CSV Export/Import Fails

**Problem:** Export succeeds but import fails or CSV is corrupt.

**Solutions:**

1. **Encoding issues**
   - Use UTF-8 encoding for special characters
   - Open in text editor that supports UTF-8 (VS Code, Notepad++, not Notepad)

2. **Excel mangles CSV**
   - Excel sometimes changes encoding or adds extra formatting
   - Use LibreOffice Calc or Google Sheets instead
   - Or edit as plain text

3. **Path issues**
   - Use absolute paths: `C:\MyMod\Spanish.csv`
   - Or relative to game directory: `UserData\MyMod\Spanish.csv`

### Memory Usage Too High

**Problem:** Loading all languages uses too much RAM.

**Solutions:**

1. **Lazy load languages**
   - Only load languages when first accessed
   - Unload unused languages after export

2. **Don't initialize on startup**
   - Call `MultiLingualLocalization.Initialize()` only when needed
   - E.g., when entering mod settings, not on game boot

3. **Filter categories**
   - Only load categories you need
   - Implement filtered loading in MultiLingualLocalization

---

## Advanced: Localization Debugging

### Log All Translations for a Key

```csharp
void DebugKey(string category, string key)
{
    Debug.Log($"=== Translations for {category}.{key} ===");

    var allTranslations = MultiLingualLocalization.GetAllTranslations(category, key);

    if (allTranslations.Count == 0)
    {
        Debug.LogWarning("Key not found in any language!");
        return;
    }

    foreach (var (language, text) in allTranslations.OrderBy(t => t.Key))
    {
        Debug.Log($"{language,-12}: {text}");
    }
}

// Usage
DebugKey("weapons", "weapons.assault_rifle.name");
```

### Find Untranslated Keys

```csharp
void FindUntranslatedKeys(LocaLanguage targetLanguage)
{
    var baseLanguage = MultiLingualLocalization.Languages.English;
    var baseCategories = MultiLingualLocalization.GetCategories(baseLanguage);

    var untranslated = new List<string>();

    foreach (var category in baseCategories)
    {
        var keys = MultiLingualLocalization.GetKeys(baseLanguage, category);

        foreach (var key in keys)
        {
            if (!MultiLingualLocalization.HasKey(targetLanguage, category, key))
            {
                untranslated.Add($"{category}.{key}");
            }
        }
    }

    Debug.Log($"=== Untranslated Keys in {targetLanguage} ===");
    Debug.Log($"Total: {untranslated.Count}");

    foreach (var key in untranslated.Take(20))
    {
        Debug.Log($"  - {key}");
    }

    if (untranslated.Count > 20)
        Debug.Log($"  ... and {untranslated.Count - 20} more");
}
```

### Export Only Mod Keys

```csharp
void ExportModKeys(string modpackName, LocaLanguage language)
{
    // Get all keys in base game
    var baseKeys = GetAllKeys(MultiLingualLocalization.Languages.English);

    // Load mod's templates to find its keys
    var modKeys = ExtractModKeys(modpackName);

    // Filter to only mod keys
    var modOnlyKeys = modKeys.Except(baseKeys).ToList();

    // Export filtered CSV
    ExportFilteredKeys(language, modOnlyKeys, $"{modpackName}_{language}.csv");
}
```

---

## Related Documentation

- **[Localization Patterns Analysis](../../reverse-engineering/localization-patterns.md)** - All localization field types
- **[Localization System Architecture](../../reverse-engineering/localization-system-architecture.md)** - Deep dive into game's system
- **[Template Field Compatibility](../reference/template-field-compatibility.md)** - Which fields work

---

## Summary

**Multi-lingual localization enables:**
- ✅ View all translations simultaneously
- ✅ Edit any language without switching game language
- ✅ Complete translations for global communities
- ✅ Professional translator workflows (CSV export/import)
- ✅ Validation tools (find missing keys)

**Key takeaways:**
1. Always provide English (fallback language)
2. Use descriptive, unique keys
3. Store translations in CSV files (not hardcoded)
4. Test in multiple languages before release
5. Leverage CSV export/import for translators
6. Validate completeness before shipping

**Result:** Mods that work correctly for players worldwide, not just English speakers.
