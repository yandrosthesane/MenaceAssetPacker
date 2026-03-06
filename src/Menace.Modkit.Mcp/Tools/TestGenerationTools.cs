using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using Menace.Modkit.App.Services;
using ModelContextProtocol.Server;

namespace Menace.Modkit.Mcp.Tools;

/// <summary>
/// MCP tools for auto-generating tests from modpack contents.
/// Analyzes modpack structure and creates test specifications automatically.
/// </summary>
[McpServerToolType]
public static class TestGenerationTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [McpServerTool(Name = "test_generate", Destructive = false)]
    [Description("Auto-generate test specifications for a modpack based on its contents (templates, assets, lua, clones). Creates test files in modpack's tests/ directory.")]
    public static async Task<string> GenerateTests(
        ModpackManager modpackManager,
        [Description("Modpack name to generate tests for")] string modpack,
        [Description("Overwrite existing tests (default false)")] bool overwrite = false)
    {
        try
        {
            var modpacks = modpackManager.GetStagingModpacks();
            var modpackInfo = modpacks.FirstOrDefault(m => m.Name.Equals(modpack, StringComparison.OrdinalIgnoreCase));
            if (modpackInfo == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Modpack '{modpack}' not found"
                }, JsonOptions);
            }

            var generatedTests = new List<string>();
            var testsDir = Path.Combine(modpackInfo.Path, "tests");

            // Create tests directory if it doesn't exist
            if (!Directory.Exists(testsDir))
            {
                Directory.CreateDirectory(testsDir);
            }

            // Load modpack.json to analyze contents
            var manifestPath = Path.Combine(modpackInfo.Path, "modpack.json");
            var manifestJson = await File.ReadAllTextAsync(manifestPath);
            var manifest = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(manifestJson);

            if (manifest == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Failed to parse modpack.json"
                }, JsonOptions);
            }

            // Generate template tests
            if (manifest.TryGetValue("patches", out var patches) || manifest.TryGetValue("templates", out patches))
            {
                var testPath = Path.Combine(testsDir, "templates.json");
                if (overwrite || !File.Exists(testPath))
                {
                    var templateTest = GenerateTemplateTests(modpack, patches);
                    await File.WriteAllTextAsync(testPath, JsonSerializer.Serialize(templateTest, JsonOptions));
                    generatedTests.Add("templates.json");
                }
            }

            // Generate clone tests
            if (manifest.TryGetValue("clones", out var clones))
            {
                var testPath = Path.Combine(testsDir, "clones.json");
                if (overwrite || !File.Exists(testPath))
                {
                    var cloneTest = GenerateCloneTests(modpack, clones);
                    await File.WriteAllTextAsync(testPath, JsonSerializer.Serialize(cloneTest, JsonOptions));
                    generatedTests.Add("clones.json");
                }
            }

            // Generate asset tests
            var assetsDir = Path.Combine(modpackInfo.Path, "assets");
            if (Directory.Exists(assetsDir))
            {
                var assetFiles = Directory.GetFiles(assetsDir, "*.*", SearchOption.AllDirectories);
                if (assetFiles.Length > 0)
                {
                    var testPath = Path.Combine(testsDir, "assets.json");
                    if (overwrite || !File.Exists(testPath))
                    {
                        var assetTest = GenerateAssetTests(modpack, assetFiles, modpackInfo.Path);
                        await File.WriteAllTextAsync(testPath, JsonSerializer.Serialize(assetTest, JsonOptions));
                        generatedTests.Add("assets.json");
                    }
                }
            }

            // Generate Lua tests
            var luaDir = Path.Combine(modpackInfo.Path, "lua");
            if (Directory.Exists(luaDir))
            {
                var luaFiles = Directory.GetFiles(luaDir, "*.lua", SearchOption.AllDirectories);
                if (luaFiles.Length > 0)
                {
                    var testPath = Path.Combine(testsDir, "lua.json");
                    if (overwrite || !File.Exists(testPath))
                    {
                        var luaTest = GenerateLuaTests(modpack, luaFiles);
                        await File.WriteAllTextAsync(testPath, JsonSerializer.Serialize(luaTest, JsonOptions));
                        generatedTests.Add("lua.json");
                    }
                }
            }

            // Generate sanity test
            var sanityPath = Path.Combine(testsDir, "sanity.json");
            if (overwrite || !File.Exists(sanityPath))
            {
                var sanityTest = GenerateSanityTest(modpack);
                await File.WriteAllTextAsync(sanityPath, JsonSerializer.Serialize(sanityTest, JsonOptions));
                generatedTests.Add("sanity.json");
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                modpack = modpack,
                testsGenerated = generatedTests.Count,
                tests = generatedTests,
                testsDirectory = testsDir
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            }, JsonOptions);
        }
    }

    [McpServerTool(Name = "test_run_modpack", Destructive = false)]
    [Description("Run all tests for a modpack. Automatically finds all test files in modpack's tests/ directory and runs them sequentially.")]
    public static async Task<string> RunModpackTests(
        ModpackManager modpackManager,
        DeployManager deployManager,
        [Description("Modpack name to test")] string modpack,
        [Description("Auto-launch game if not running (default true)")] bool autoLaunch = true,
        [Description("Continue running tests even if one fails (default false)")] bool continueOnFailure = false)
    {
        try
        {
            // Find all test files in modpack's tests/ directory
            var modpackPath = Path.Combine(Directory.GetCurrentDirectory(), "staging", modpack);
            var testsDir = Path.Combine(modpackPath, "tests");

            if (!Directory.Exists(testsDir))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"No tests directory found for modpack '{modpack}'",
                    hint = $"Run test_generate to create tests: test_generate(\"{modpack}\")"
                }, JsonOptions);
            }

            var testFiles = Directory.GetFiles(testsDir, "*.json");
            if (testFiles.Length == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"No test files found in {testsDir}",
                    hint = $"Run test_generate to create tests: test_generate(\"{modpack}\")"
                }, JsonOptions);
            }

            var results = new List<object>();
            var allPassed = true;

            foreach (var testFile in testFiles.OrderBy(f => f))
            {
                var testName = Path.GetFileNameWithoutExtension(testFile);

                try
                {
                    var result = await TestTools.RunTest(modpackManager, deployManager, testFile, modpack, autoLaunch);
                    var resultData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(result);

                    var passed = resultData?["passed"].GetBoolean() ?? false;
                    if (!passed) allPassed = false;

                    results.Add(new
                    {
                        test = testName,
                        passed = passed,
                        result = resultData
                    });

                    if (!passed && !continueOnFailure)
                    {
                        break; // Stop on first failure
                    }
                }
                catch (Exception ex)
                {
                    allPassed = false;
                    results.Add(new
                    {
                        test = testName,
                        passed = false,
                        error = ex.Message
                    });

                    if (!continueOnFailure)
                    {
                        break;
                    }
                }
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                modpack = modpack,
                allPassed = allPassed,
                totalTests = testFiles.Length,
                testsRun = results.Count,
                testsPassed = results.Count(r => ((dynamic)r).passed),
                testsFailed = results.Count(r => !((dynamic)r).passed),
                results = results
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            }, JsonOptions);
        }
    }

    private static object GenerateTemplateTests(string modpack, JsonElement patches)
    {
        var steps = new List<object>();

        // Navigate to a scene where templates are loaded
        steps.Add(new
        {
            type = "command",
            name = "Navigate to main menu to load templates",
            command = "test.goto_main"
        });

        steps.Add(new
        {
            type = "wait",
            name = "Wait for scene load",
            durationMs = 3000
        });

        // First, test that all template types load
        var templateTypes = new HashSet<string>();
        foreach (var templateType in patches.EnumerateObject())
        {
            templateTypes.Add(templateType.Name);
        }

        foreach (var templateType in templateTypes)
        {
            steps.Add(new
            {
                type = "repl",
                name = $"Verify {templateType} templates load",
                code = $"Templates.FindAll(\"{templateType}\").Length > 0"
            });
        }

        // Then test specific field values using GetProperty
        foreach (var templateType in patches.EnumerateObject())
        {
            foreach (var instance in templateType.Value.EnumerateObject())
            {
                // First verify the instance exists
                steps.Add(new
                {
                    type = "repl",
                    name = $"Verify {templateType.Name}.{instance.Name} exists",
                    code = $"!Templates.Find(\"{templateType.Name}\", \"{instance.Name}\").IsNull"
                });

                // Then verify each field using GetProperty
                foreach (var field in instance.Value.EnumerateObject())
                {
                    var expectedValue = field.Value.ToString();

                    steps.Add(new
                    {
                        type = "assert",
                        name = $"Verify {templateType.Name}.{instance.Name}.{field.Name}",
                        expression = $"Templates.GetProperty(\"{templateType.Name}\", \"{instance.Name}\", \"{field.Name}\")?.ToString()",
                        expected = expectedValue
                    });
                }
            }
        }

        return new
        {
            name = $"{modpack} - Template Patches",
            modpack = modpack,
            steps = steps
        };
    }

    private static object GenerateCloneTests(string modpack, JsonElement clones)
    {
        var steps = new List<object>();

        steps.Add(new
        {
            type = "command",
            name = "Navigate to main menu",
            command = "test.goto_main"
        });

        steps.Add(new
        {
            type = "wait",
            name = "Wait for templates to load",
            durationMs = 3000
        });

        foreach (var templateType in clones.EnumerateObject())
        {
            foreach (var clone in templateType.Value.EnumerateObject())
            {
                // Test that clone exists
                steps.Add(new
                {
                    type = "repl",
                    name = $"Verify clone {templateType.Name}.{clone.Name} exists",
                    code = $"!Templates.Find(\"{templateType.Name}\", \"{clone.Name}\").IsNull"
                });

                // Test clone properties if specified using GetProperty
                if (clone.Value.TryGetProperty("properties", out var properties))
                {
                    foreach (var prop in properties.EnumerateObject())
                    {
                        steps.Add(new
                        {
                            type = "assert",
                            name = $"Verify {clone.Name}.{prop.Name}",
                            expression = $"Templates.GetProperty(\"{templateType.Name}\", \"{clone.Name}\", \"{prop.Name}\")?.ToString()",
                            expected = prop.Value.ToString()
                        });
                    }
                }
            }
        }

        return new
        {
            name = $"{modpack} - Clones",
            modpack = modpack,
            steps = steps
        };
    }

    private static object GenerateAssetTests(string modpack, string[] assetFiles, string modpackPath)
    {
        var steps = new List<object>();

        foreach (var assetFile in assetFiles)
        {
            var relativePath = Path.GetRelativePath(modpackPath, assetFile).Replace('\\', '/');

            steps.Add(new
            {
                type = "repl",
                name = $"Verify asset {relativePath} loads",
                code = $"AssetManager.LoadAsset(\"{modpack}/{relativePath}\") != null"
            });
        }

        return new
        {
            name = $"{modpack} - Assets",
            modpack = modpack,
            steps = steps
        };
    }

    private static object GenerateLuaTests(string modpack, string[] luaFiles)
    {
        var steps = new List<object>();

        // Scan Lua files for registered commands
        var commands = new List<string>();

        foreach (var luaFile in luaFiles)
        {
            var content = File.ReadAllText(luaFile);

            // Simple regex to find DevConsole.RegisterCommand calls
            var matches = System.Text.RegularExpressions.Regex.Matches(
                content,
                @"DevConsole\.RegisterCommand\s*\(\s*[""']([^""']+)[""']"
            );

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                commands.Add(match.Groups[1].Value);
            }
        }

        // Test each command
        foreach (var command in commands.Distinct())
        {
            steps.Add(new
            {
                type = "command",
                name = $"Test Lua command: {command}",
                command = command
            });
        }

        // Generic Lua engine test
        steps.Add(new
        {
            type = "assert",
            name = "Verify Lua engine is initialized",
            expression = "LuaScriptEngine.Instance.IsInitialized",
            expected = "True"
        });

        return new
        {
            name = $"{modpack} - Lua Scripts",
            modpack = modpack,
            steps = steps
        };
    }

    private static object GenerateSanityTest(string modpack)
    {
        return new
        {
            name = $"{modpack} - Sanity Check",
            modpack = modpack,
            steps = new object[]
            {
                new
                {
                    type = "command",
                    name = "Check test harness status",
                    command = "test.status"
                },
                new
                {
                    type = "repl",
                    name = "Verify modpack is loaded",
                    code = $"Modpacks.IsModpackLoaded(\"{modpack}\")"
                },
                new
                {
                    type = "command",
                    name = "Check for mod errors",
                    command = "errors"
                }
            }
        };
    }
}
