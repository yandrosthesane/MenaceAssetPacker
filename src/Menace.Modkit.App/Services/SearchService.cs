using System;
using System.Collections.Generic;
using System.Text;
using Raffinert.FuzzySharp;

namespace Menace.Modkit.App.Services;

/// <summary>
/// Shared search service providing token-based matching with fuzzy fallback.
/// Used by StatsEditor, AssetBrowser, and DocsViewer for consistent search behavior.
/// </summary>
public static class SearchService
{
    /// <summary>
    /// Minimum fuzzy ratio (0-100) required for a match.
    /// 85 is strict enough to avoid false positives like "grenade" matching "generic".
    /// </summary>
    private const int FuzzyThreshold = 85;

    /// <summary>
    /// Tokenize a search query into lowercase words.
    /// Splits on spaces, underscores, hyphens, and periods.
    /// </summary>
    public static string[] TokenizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<string>();

        return query.ToLowerInvariant()
            .Split(new[] { ' ', '_', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Tokenize a name into lowercase words.
    /// Handles snake_case, camelCase, PascalCase, spaces, and other separators.
    /// </summary>
    public static string[] TokenizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Array.Empty<string>();

        var result = new List<string>();
        var current = new StringBuilder();

        foreach (var c in name)
        {
            // Split on uppercase letters (camelCase/PascalCase boundaries)
            if (char.IsUpper(c) && current.Length > 0)
            {
                result.Add(current.ToString().ToLowerInvariant());
                current.Clear();
            }

            // Split on common separators
            if (c == '_' || c == ' ' || c == '-' || c == '.' || c == '/')
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString().ToLowerInvariant());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            result.Add(current.ToString().ToLowerInvariant());

        return result.ToArray();
    }

    /// <summary>
    /// Check if ALL query tokens are found in the name tokens.
    /// A query token matches a name token if either contains the other.
    /// </summary>
    public static bool AllTokensMatch(string[] queryTokens, string[] nameTokens)
    {
        if (queryTokens.Length == 0)
            return false;

        foreach (var qt in queryTokens)
        {
            bool found = false;
            foreach (var nt in nameTokens)
            {
                // Match if name token contains query token OR query token contains name token
                if (nt.Contains(qt) || qt.Contains(nt))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Score a match using token-based matching (no fuzzy).
    /// Returns baseTier for exact substring match, baseTier-10 for token match, -1 for no match.
    /// </summary>
    public static int ScoreTokenMatch(string query, string text, int baseTier)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
            return -1;

        var textLower = text.ToLowerInvariant();
        var queryLower = query.ToLowerInvariant();

        // Exact substring match (original behavior) - highest score
        if (textLower.Contains(queryLower))
            return baseTier;

        // Token-based match
        var queryTokens = TokenizeQuery(query);
        var textTokens = TokenizeName(text);

        if (AllTokensMatch(queryTokens, textTokens))
            return baseTier - 10; // Slightly lower than exact match

        return -1;
    }

    /// <summary>
    /// Score a match using token-based matching with fuzzy fallback for typo tolerance.
    /// Returns baseTier for exact match, baseTier-10 for token match, baseTier-20 for fuzzy match, -1 for no match.
    /// </summary>
    public static int ScoreTokenMatchFuzzy(string query, string text, int baseTier)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
            return -1;

        // Try exact/token match first
        var exactScore = ScoreTokenMatch(query, text, baseTier);
        if (exactScore >= 0)
            return exactScore;

        // Fuzzy fallback for typo tolerance
        // Use PartialRatio which is good for substring matching with typos
        var ratio = Fuzz.PartialRatio(query.ToLowerInvariant(), text.ToLowerInvariant());
        if (ratio >= FuzzyThreshold)
            return baseTier - 20; // Lower score for fuzzy matches

        // Also try token-level fuzzy matching
        // This helps when the user makes a typo in one of multiple search words
        // Only for longer tokens (4+ chars) to avoid false positives
        var queryTokens = TokenizeQuery(query);
        var textTokens = TokenizeName(text);

        if (queryTokens.Length > 0 && textTokens.Length > 0)
        {
            bool allFuzzyMatch = true;
            foreach (var qt in queryTokens)
            {
                // Skip fuzzy for short tokens - require exact token match instead
                if (qt.Length < 4)
                {
                    bool foundExact = false;
                    foreach (var tt in textTokens)
                    {
                        if (tt.Contains(qt) || qt.Contains(tt))
                        {
                            foundExact = true;
                            break;
                        }
                    }
                    if (!foundExact)
                    {
                        allFuzzyMatch = false;
                        break;
                    }
                    continue;
                }

                bool foundFuzzy = false;
                foreach (var tt in textTokens)
                {
                    // Use Ratio for token-to-token comparison (more strict)
                    if (Fuzz.Ratio(qt, tt) >= FuzzyThreshold)
                    {
                        foundFuzzy = true;
                        break;
                    }
                }
                if (!foundFuzzy)
                {
                    allFuzzyMatch = false;
                    break;
                }
            }
            if (allFuzzyMatch)
                return baseTier - 25; // Lowest score for token-level fuzzy matches
        }

        return -1;
    }
}
