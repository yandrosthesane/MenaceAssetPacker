using System.ComponentModel;
using System.Text.Json;
using Menace.Modkit.App.Services;
using ModelContextProtocol.Server;

namespace Menace.Modkit.Mcp.Tools;

/// <summary>
/// MCP tools for running automated tests against the game.
/// Orchestrates modpack deployment, game launch, test execution, and result collection.
/// </summary>
[McpServerToolType]
public static class TestTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [McpServerTool(Name = "test_run", Destructive = false)]
    [Description("Run an automated test against the game. Deploys modpack (if specified), ensures game is running, executes test steps, and returns results. Test can be a JSON file path or inline JSON.")]
    public static async Task<string> RunTest(
        ModpackManager modpackManager,
        DeployManager deployManager,
        [Description("Test specification (JSON file path or inline JSON object)")] string test,
        [Description("Modpack to deploy before test (optional - will deploy if specified)")] string? modpack = null,
        [Description("Auto-launch game if not running (default true)")] bool autoLaunch = true,
        [Description("Timeout in seconds for game operations (default 30)")] int timeout = 30)
    {
        var steps = new List<object>();
        var testName = "unknown";

        try
        {
            // 1. Parse test spec
            TestSpec testSpec;
            try
            {
                testSpec = ParseTestSpec(test);
                testName = testSpec.Name ?? "unnamed_test";
                steps.Add(new { step = "parse", name = "Parse test spec", status = "pass", message = $"Loaded test '{testName}'" });
            }
            catch (Exception ex)
            {
                steps.Add(new { step = "parse", name = "Parse test spec", status = "fail", error = ex.Message });
                return JsonSerializer.Serialize(new
                {
                    test = testName,
                    passed = false,
                    error = $"Failed to parse test spec: {ex.Message}",
                    steps
                }, JsonOptions);
            }

            // Override modpack if specified in test spec
            if (string.IsNullOrEmpty(modpack) && !string.IsNullOrEmpty(testSpec.Modpack))
                modpack = testSpec.Modpack;

            // 2. Deploy modpack if specified
            if (!string.IsNullOrEmpty(modpack))
            {
                try
                {
                    var deployResult = await DeploymentTools.DeployModpack(modpackManager, deployManager, modpack);
                    var success = !deployResult.Contains("error", StringComparison.OrdinalIgnoreCase);
                    steps.Add(new { step = "deploy", name = $"Deploy {modpack}", status = success ? "pass" : "fail", result = deployResult });

                    if (!success)
                    {
                        return JsonSerializer.Serialize(new
                        {
                            test = testName,
                            passed = false,
                            error = "Modpack deployment failed",
                            steps
                        }, JsonOptions);
                    }
                }
                catch (Exception ex)
                {
                    steps.Add(new { step = "deploy", name = "Deploy modpack", status = "fail", error = ex.Message });
                    return JsonSerializer.Serialize(new
                    {
                        test = testName,
                        passed = false,
                        error = $"Modpack deployment failed: {ex.Message}",
                        steps
                    }, JsonOptions);
                }
            }

            // 3. Ensure game is running
            var gameStatus = await GameTools.GameStatus();
            if (gameStatus.Contains("Game not running"))
            {
                if (autoLaunch)
                {
                    steps.Add(new { step = "check_game", name = "Check game status", status = "info", message = "Game not running, launching..." });

                    var launchResult = await GameTools.GameLaunch(timeout);
                    var launchSuccess = launchResult.Contains("\"success\": true");

                    steps.Add(new { step = "launch", name = "Launch game", status = launchSuccess ? "pass" : "fail", result = launchResult });

                    if (!launchSuccess)
                    {
                        return JsonSerializer.Serialize(new
                        {
                            test = testName,
                            passed = false,
                            error = "Failed to launch game",
                            steps
                        }, JsonOptions);
                    }
                }
                else
                {
                    steps.Add(new { step = "check_game", name = "Check game status", status = "fail", error = "Game not running and autoLaunch=false" });
                    return JsonSerializer.Serialize(new
                    {
                        test = testName,
                        passed = false,
                        error = "Game not running",
                        steps
                    }, JsonOptions);
                }
            }
            else
            {
                steps.Add(new { step = "check_game", name = "Check game status", status = "pass", message = "Game is running" });
            }

            // 4. Execute test steps
            foreach (var testStep in testSpec.Steps)
            {
                var stepResult = await ExecuteTestStep(testStep, timeout);
                steps.Add(stepResult);

                if (stepResult.status == "fail")
                {
                    // Stop on first failure unless test spec says continue
                    if (!testSpec.ContinueOnFailure)
                    {
                        return JsonSerializer.Serialize(new
                        {
                            test = testName,
                            passed = false,
                            error = $"Test failed at step: {testStep.Name}",
                            steps
                        }, JsonOptions);
                    }
                }
            }

            // 5. All steps completed
            var allPassed = steps.All(s =>
            {
                var status = ((dynamic)s).status;
                return status == "pass" || status == "info" || status == "skip";
            });

            return JsonSerializer.Serialize(new
            {
                test = testName,
                passed = allPassed,
                totalSteps = testSpec.Steps.Count,
                steps
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            steps.Add(new { step = "error", name = "Unexpected error", status = "fail", error = ex.Message, stackTrace = ex.StackTrace });
            return JsonSerializer.Serialize(new
            {
                test = testName,
                passed = false,
                error = $"Test execution failed: {ex.Message}",
                steps
            }, JsonOptions);
        }
    }

    private static TestSpec ParseTestSpec(string test)
    {
        // Check if it's a file path
        if (System.IO.File.Exists(test))
        {
            var json = System.IO.File.ReadAllText(test);
            return JsonSerializer.Deserialize<TestSpec>(json, JsonReadOptions)
                ?? throw new Exception("Failed to deserialize test spec");
        }

        // Try to parse as inline JSON
        return JsonSerializer.Deserialize<TestSpec>(test, JsonReadOptions)
            ?? throw new Exception("Failed to parse test spec JSON");
    }

    private static async Task<dynamic> ExecuteTestStep(TestStep step, int timeout)
    {
        try
        {
            switch (step.Type?.ToLowerInvariant())
            {
                case "command":
                case "cmd":
                    return await ExecuteCommand(step);

                case "assert":
                    return await ExecuteAssert(step);

                case "assert_contains":
                    return await ExecuteAssertContains(step);

                case "wait":
                    return await ExecuteWait(step);

                case "screenshot":
                    return await ExecuteScreenshot(step);

                case "repl":
                case "eval":
                    return await ExecuteRepl(step);

                default:
                    return new
                    {
                        step = step.Name,
                        type = step.Type,
                        status = "skip",
                        message = $"Unknown step type: {step.Type}"
                    };
            }
        }
        catch (Exception ex)
        {
            return new
            {
                step = step.Name,
                type = step.Type,
                status = "fail",
                error = ex.Message
            };
        }
    }

    private static async Task<dynamic> ExecuteCommand(TestStep step)
    {
        var result = await GameTools.GameCmd(step.Command ?? "");
        var success = !result.Contains("\"success\": false") && !result.Contains("ERROR") && !result.Contains("FAILED");

        return new
        {
            step = step.Name,
            type = "command",
            status = success ? "pass" : "fail",
            command = step.Command,
            result
        };
    }

    private static async Task<dynamic> ExecuteAssert(TestStep step)
    {
        var command = $"test.assert \"{step.Expression}\" \"{step.Expected}\"";
        var result = await GameTools.GameCmd(command);

        var passed = result.Contains("ASSERTION PASSED");

        return new
        {
            step = step.Name,
            type = "assert",
            status = passed ? "pass" : "fail",
            expression = step.Expression,
            expected = step.Expected,
            result
        };
    }

    private static async Task<dynamic> ExecuteAssertContains(TestStep step)
    {
        var command = $"test.assert_contains \"{step.Expression}\" \"{step.Expected}\"";
        var result = await GameTools.GameCmd(command);

        var passed = result.Contains("ASSERTION PASSED");

        return new
        {
            step = step.Name,
            type = "assert_contains",
            status = passed ? "pass" : "fail",
            expression = step.Expression,
            substring = step.Expected,
            result
        };
    }

    private static async Task<dynamic> ExecuteWait(TestStep step)
    {
        var durationMs = step.DurationMs ?? 1000;
        await Task.Delay(durationMs);

        return new
        {
            step = step.Name,
            type = "wait",
            status = "pass",
            durationMs
        };
    }

    private static async Task<dynamic> ExecuteScreenshot(TestStep step)
    {
        var filename = step.Filename ?? $"test_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var result = await GameTools.GameCmd($"test.screenshot {filename}");

        var success = result.Contains("Screenshot saved");

        return new
        {
            step = step.Name,
            type = "screenshot",
            status = success ? "pass" : "fail",
            filename,
            result
        };
    }

    private static async Task<dynamic> ExecuteRepl(TestStep step)
    {
        var result = await GameTools.GameRepl(step.Code ?? "");
        var success = !result.Contains("\"success\": false");

        return new
        {
            step = step.Name,
            type = "repl",
            status = success ? "pass" : "fail",
            code = step.Code,
            result
        };
    }

    // Test specification models
    private class TestSpec
    {
        public string? Name { get; set; }
        public string? Modpack { get; set; }
        public bool ContinueOnFailure { get; set; }
        public List<TestStep> Steps { get; set; } = new();
    }

    private class TestStep
    {
        public string? Type { get; set; }
        public string? Name { get; set; }
        public string? Command { get; set; }
        public string? Expression { get; set; }
        public string? Expected { get; set; }
        public string? Code { get; set; }
        public int? DurationMs { get; set; }
        public string? Filename { get; set; }
    }
}
