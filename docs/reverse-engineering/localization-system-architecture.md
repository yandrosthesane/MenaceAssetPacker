# Localization System Architecture (from Ghidra Analysis)

**Analysis Date:** 2026-03-04
**Source:** Ghidra reverse engineering of game binary
**Purpose:** Understand localization system to enable multilingual modding

---

## Key Classes

### Menace.Tools.LocaManager
Main localization manager singleton.

**Key Methods:**
- `SetCurrentLanguage(LocaLanguage language)` - Change active language
- `ReloadCurrentLanguage()` - Reload current language CSV
- `DetermineDefaultLanguage()` - Get system default language
- `Get()` - Get singleton instance

**Fields:**
- `+0x10`: Current language (int enum)
- `+0x18`: LocaData instance

### Menace.Tools.LocaData
Manages localization data loading and storage.

**Key Methods:**
- `LoadTranslation(LocaLanguage language, bool param)` - Load CSV for a language
- `ReadCsvFile(string filename)` - Read CSV file from Resources or disk
- `GetCategory(LocaCategory category)` - Get category data (weapons, skills, etc.)
- `GetCsvPath(string filename)` - Resolve CSV file path

**CSV File Loading:**
1. Try to load from filesystem: `{Application.dataPath}/../{path}/{filename}.csv`
2. If not found, load from Unity Resources: `Resources.Load<TextAsset>(filename)`
3. This means **modders can override localization by placing CSV files on disk!**

### Menace.Tools.LocaCategoryData
Manages localization entries for a specific category (e.g., "weapons", "skills").

**Key Methods:**
- `TryGetTranslation(string key, out string translation)` - Get translation for key
- `GetDefaultTranslation(string key)` - Get English/default translation
- `GetEntry(string key)` - Get full LocaEntry for key
- `HasEntry(string key)` - Check if key exists
- `AddEntry(string key, string defaultText, LocaEntryType type, ...)` - Add entry
- `RemoveEntry(string key)` - Remove entry
- `ChangeKey(string oldKey, string newKey)` - Rename key

**Fields:**
- `+0x18`: Dictionary<string, LocaEntry> - All entries in this category

### Menace.Tools.LocaEntry
Individual localization entry.

**Structure (from memory offsets in decompiled code):**
```
+0x20: string Key              - Localization key (e.g., "weapons.rifle.name")
+0x28: string DefaultText      - Default/English translation
+0x30: string CurrentLangText  - Translation for current language
+0x18: LocaEntryType Type      - Type enum (likely)
```

**Key Methods:**
- `IsTranslationOutdated()` - Check if translation needs update
- `MarkTranslationAsOutdated()` - Flag translation as outdated

---

## CSV File Format

### Discovery
From `LoadTranslation` function analysis:
- CSV files use comma (`,`) as delimiter
- Parsed line-by-line with `LocaReader.ParseCsvLine`
- At least 5 columns expected (checks indices 0-4)

### Column Structure (inferred from code)
```
Column 0: Key                   - "weapons.assault_rifle.name"
Column 1: Category              - "Key" (header) or actual category
Column 2: Type                  - LocaEntryType enum value
Column 3: Default Translation   - English text
Column 4: Current Lang Translation - Text in current language
```

### File Locations

**Embedded (Unity Resources):**
- Compiled into game as TextAssets
- Loaded via `Resources.Load<TextAsset>(languageName)`
- Examples: "English", "Spanish", "German", etc.

**External (File Override):**
- Path: `{Application.dataPath}/../{path}/{languageName}.csv`
- On Windows: Likely `Menace_Data/../Localization/English.csv`
- On Linux: Likely `Menace_Data/../Localization/English.csv`
- **Modders can provide custom CSV files here to override!**

### Language Enum
Files are named using `LocaLanguage` enum values:
- English (0)
- Spanish, German, French, Italian, Portuguese, Russian, etc.
- Exact values unknown, but enum is converted to string for filename

---

## How Current System Works

### Single Language Loading
**The game only loads ONE language at a time:**

1. User selects language (or system default detected)
2. `LocaManager.SetCurrentLanguage(language)` is called
3. `LocaData.LoadTranslation(language)` loads that language's CSV
4. Each LocaEntry stores:
   - **Default text** (English) in field `+0x28`
   - **Current language text** in field `+0x30`
5. When getting translation:
   - If current language is default (English): Return `+0x28`
   - If current language is other: Return `+0x30` if not empty, else `+0x28`

### Translation Lookup Flow

```
Templates.GetProperty("WeaponTemplate", "weapon.rifle", "m_LocaState")
  └─> Returns: { m_Key: "weapons.rifle.name", m_TableID: "weapons" }

Localization.Get("weapons", "weapons.rifle.name")
  ├─> LocaData.GetCategory("weapons")
  ├─> LocaCategoryData.TryGetTranslation("weapons.rifle.name", out translation)
  │   ├─> Get LocaEntry from dictionary
  │   ├─> If current language != default:
  │   │   └─> Return entry.CurrentLangText (if not empty)
  │   └─> Else return entry.DefaultText
  └─> Returns: "Assault Rifle" (or "Fusil de Asalto" in Spanish)
```

---

## Limitations of Current System

### 1. Only One Language Loaded at a Time
**Problem:** Cannot access Spanish translation while English is active.

**Impact:**
- Modders editing templates must switch game language to see translations
- Cannot export all translations simultaneously
- Cannot validate translations without loading each language

### 2. CurrentLangText Overwrites on Language Switch
**Problem:** When loading new language, previous `CurrentLangText` is discarded.

**Impact:**
- Each LocaEntry only stores 2 strings (default + current)
- Not designed for multi-language editing

### 3. No API to List Available Languages
**Problem:** No public method to enumerate all language files.

**Impact:**
- Cannot discover which languages exist
- Cannot iterate over all languages programmatically

---

## Proposed Solution: Multi-Language Localization API

### Goal
Allow modders to:
1. View translations for ALL languages simultaneously
2. Edit translations for specific languages
3. Export/import translations without switching game language

### Architecture

**New Classes:**

```csharp
/// <summary>
/// Multi-language localization API that loads ALL languages
/// </summary>
public static class MultiLingualLocalization
{
    // Stores translations for ALL languages
    private static Dictionary<LocaLanguage, Dictionary<string, Dictionary<string, string>>>
        _allTranslations;

    /// <summary>
    /// Load all available language CSV files
    /// </summary>
    public static void LoadAllLanguages()
    {
        foreach (LocaLanguage lang in Enum.GetValues(typeof(LocaLanguage)))
        {
            LoadLanguage(lang);
        }
    }

    /// <summary>
    /// Get translation for specific language
    /// </summary>
    public static string GetTranslation(LocaLanguage language, string category, string key)
    {
        if (_allTranslations.TryGetValue(language, out var categories))
        {
            if (categories.TryGetValue(category, out var entries))
            {
                if (entries.TryGetValue(key, out var translation))
                {
                    return translation;
                }
            }
        }
        return null; // Or fallback to English
    }

    /// <summary>
    /// Set translation for specific language
    /// </summary>
    public static void SetTranslation(LocaLanguage language, string category, string key, string text)
    {
        // Update in-memory dictionary
        // Optionally write to CSV file
    }

    /// <summary>
    /// Get all translations for a key (all languages)
    /// </summary>
    public static Dictionary<LocaLanguage, string> GetAllTranslations(string category, string key)
    {
        var result = new Dictionary<LocaLanguage, string>();
        foreach (var lang in _allTranslations.Keys)
        {
            var translation = GetTranslation(lang, category, key);
            if (translation != null)
            {
                result[lang] = translation;
            }
        }
        return result;
    }

    /// <summary>
    /// Export translations to CSV files
    /// </summary>
    public static void ExportLanguage(LocaLanguage language, string outputPath)
    {
        // Write CSV file for specific language
    }

    /// <summary>
    /// List all available languages
    /// </summary>
    public static LocaLanguage[] GetAvailableLanguages()
    {
        return _allTranslations.Keys.ToArray();
    }
}
```

### Usage Examples

**View all translations for a weapon name:**
```csharp
var translations = MultiLingualLocalization.GetAllTranslations("weapons", "weapons.assault_rifle.name");

// Results:
// English: "Assault Rifle"
// Spanish: "Fusil de Asalto"
// German: "Sturmgewehr"
// French: "Fusil d'Assaut"
// Italian: "Fucile d'Assalto"
// Portuguese: "Rifle de Assalto"
// Russian: "Штурмовая винтовка"

foreach (var (language, text) in translations)
{
    Debug.Log($"{language}: {text}");
}
```

**Edit specific language without switching game language:**
```csharp
// Modder wants to fix Spanish translation
MultiLingualLocalization.SetTranslation(
    LocaLanguage.Spanish,
    "weapons",
    "weapons.my_custom_rifle.name",
    "Mi Rifle Personalizado"
);

// Game still running in English, but Spanish translation is set
```

**Export all translations for modpack:**
```csharp
// Export Spanish translations for translators
MultiLingualLocalization.ExportLanguage(LocaLanguage.Spanish, "MyModpack/Localization/Spanish.csv");
```

---

## Implementation Plan

### Phase 1: CSV Parsing
- Reuse `LocaData.ReadCsvFile` to load CSV content
- Parse CSV ourselves (can't rely on LocaData.LoadTranslation which overwrites)
- Store all languages in memory simultaneously

### Phase 2: Multi-Language Dictionary
- Structure: `Dict<Language, Dict<Category, Dict<Key, Text>>>`
- Load all language CSVs at startup (or on-demand)
- Keep in sync with game's current language system

### Phase 3: Read API
- `GetTranslation(language, category, key)` - Get specific translation
- `GetAllTranslations(category, key)` - Get all languages for a key
- `GetAvailableLanguages()` - List available languages

### Phase 4: Write API
- `SetTranslation(language, category, key, text)` - Update translation
- `ExportLanguage(language, path)` - Write CSV file
- `ImportLanguage(language, path)` - Load CSV file

### Phase 5: MCP Tools
- `get_all_translations(category, key)` - Returns JSON with all languages
- `set_translation(language, category, key, text)` - Update translation
- `export_translations(language, outputPath)` - Export to CSV

### Phase 6: Template Integration
- Extend `Templates.GetProperty()` to support language parameter
- `Templates.GetProperty("WeaponTemplate", "weapon.rifle", "m_LocaState.Spanish")` returns Spanish translation
- `Templates.SetProperty("WeaponTemplate", "weapon.rifle", "m_LocaState.Spanish", "Nuevo Texto")`

---

## Technical Considerations

### Memory Usage
**Loading all languages simultaneously requires more memory:**
- English CSV: ~1-2 MB per category
- 7-10 languages * 10 categories * 2 MB = ~140-200 MB
- **Acceptable** for modern systems, especially for development/modding tools

### Performance
**CSV parsing is slow, so:**
- Cache parsed data in memory
- Only reload when CSV files change
- Lazy load languages on first access

### File Override System
**Leverage existing file override mechanism:**
- Game already checks filesystem before Resources
- Place CSVs in `{dataPath}/../Localization/{Language}.csv`
- Game will load custom CSVs automatically

### Compatibility with Game System
**Don't break existing localization:**
- MultiLingualLocalization is additive, not replacement
- Game's LocaManager continues to work normally
- Our system reads same CSV files, doesn't modify them

---

## File Locations (Discovered)

### Linux (SteamDeck/Linux)
```
/home/user/.steam/debian-installation/steamapps/common/Menace/
  ├─ Menace_Data/                    (Application.dataPath)
  │   └─ Resources/                   (Embedded CSVs)
  │       ├─ English.txt
  │       ├─ Spanish.txt
  │       └─ ...
  └─ Localization/                   (File override location)
      ├─ English.csv
      ├─ Spanish.csv
      └─ ...
```

### Windows
```
C:\Program Files (x86)\Steam\steamapps\common\Menace\
  ├─ Menace_Data\                     (Application.dataPath)
  │   └─ Resources\                   (Embedded CSVs)
  └─ Localization\                    (File override location)
```

**Note:** Exact paths may vary, use `Application.dataPath` at runtime.

---

## Benefits for Modders

### 1. Edit Any Language
- No need to switch game language back and forth
- See all translations side-by-side
- Fix typos in any language

### 2. Complete Translations
- Provide full translations for custom content
- Not rely on English fallback
- Support multilingual community

### 3. Translation Tools
- Export CSV for translators
- Import translated CSVs back
- Validate translations (check for missing keys)

### 4. Multi-Language Testing
- Test mod in multiple languages without restarting
- Verify translations display correctly
- Ensure no raw keys shown

---

## Next Steps

1. **Implement MultiLingualLocalization class** in ModpackLoader
2. **Add MCP tools** for translation access
3. **Test with actual CSV files** from game
4. **Document CSV format** precisely (column count, headers, etc.)
5. **Create example workflow** for modders
6. **Add to SDK documentation**

---

## Related Files
- `src/Menace.ModpackLoader/SDK/MultiLingualLocalization.cs` (to be created)
- `src/Menace.Modkit.Mcp/Tools/LocalizationTools.cs` (to be created)
- `docs/coding-sdk/guides/multilingual-modding.md` (to be created)

---

## References
- Ghidra functions analyzed:
  - `Menace.Tools.LocaManager$$SetCurrentLanguage`
  - `Menace.Tools.LocaData$$LoadTranslation`
  - `Menace.Tools.LocaData$$ReadCsvFile`
  - `Menace.Tools.LocaCategoryData$$TryGetTranslation`
- Template localization fields documented in `/docs/analysis/localization-patterns.md`
