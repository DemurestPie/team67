
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using static BucHunt.Tests.PlayerCompletionTests;

namespace BucHunt.Tests
{
    // Task 27.1: Verify manual code entry through the real application.
    public class ManualCodeEntryTests
    {
        // Create a test application that uses isolated data files.
        private static WebApplicationFactory<Program> CreateApplication(
            IsolatedTestEnvironment environment)
        {
            return new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                    builder.UseContentRoot(environment.ContentRootPath));
        }

        // Extract the antiforgery token from a rendered HTML form.
        private static string GetToken(string html)
        {
            string token = Regex.Match(
                html,
                "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\""
            ).Groups[1].Value;

            Assert.NotEmpty(token);

            return WebUtility.HtmlDecode(token);
        }

        // Log in through the actual player entry page.
        private static async Task<string> Login(HttpClient client)
        {
            string html = await client.GetStringAsync("/Player/EnterPhone");

            using var response = await client.PostAsync(
                "/Player/EnterPhone",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["phoneNumber"] = "4235550123",
                    ["__RequestVerificationToken"] = GetToken(html)
                })
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        // Extract the rendered HTML for an individual task.
        private static string GetTaskRow(string html, int taskId)
        {
            string row = Regex.Match(
                html,
                $@"<li\b[^>]*data-task-id=""{taskId}""[^>]*>.*?</li>",
                RegexOptions.Singleline
            ).Value;

            Assert.NotEmpty(row);

            return row;
        }

        // Submit a code through the existing completion endpoint.
        private static async Task<string> SubmitManualCode(
            HttpClient client,
            string html,
            int taskId,
            string code)
        {
            string row = GetTaskRow(html, taskId);

            using var response = await client.PostAsync(
                "/Player/SubmitCode",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["taskId"] = taskId.ToString(),
                    ["code"] = code,
                    ["__RequestVerificationToken"] = GetToken(row)
                })
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        // Verify that an incomplete task offers both completion methods.
        private static void AssertCompletionOptions(
            string row, int taskId)
        {
            Assert.Contains($"id=\"btn-{taskId}\"", row);
            Assert.Contains("Enter Code", row);

            Assert.Contains($"id=\"scan-btn-{taskId}\"", row);
            Assert.Contains("Scan QR Code", row);

            Assert.Contains($"id=\"completeTaskForm-{taskId}\"", row);
        }

        // Test 1: Every incomplete task offers manual entry and QR scanning.
        [Fact]
        public async Task IncompleteTasks_DisplayBothCompletionOptions()
        {
            using var environment = new IsolatedTestEnvironment();
            using var application = CreateApplication(environment);

            using var client = application.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost")
                });

            string html = await Login(client);

            for (int taskId = 1; taskId <= 8; taskId++)
            {
                string row = GetTaskRow(html, taskId);

                AssertCompletionOptions(row, taskId);
            }
        }

        // Test 2: Manual forms use the existing validation endpoint.
        [Fact]
        public async Task ManualEntryForms_UseExistingValidationEndpoint()
        {
            using var environment = new IsolatedTestEnvironment();
            using var application = CreateApplication(environment);

            using var client = application.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost")
                });

            string html = await Login(client);

            for (int taskId = 1; taskId <= 8; taskId++)
            {
                string row = GetTaskRow(html, taskId);

                Assert.Contains("action=\"/Player/SubmitCode\"", row);
                Assert.Contains("method=\"post\"", row);
                Assert.Contains("name=\"taskId\"", row);
                Assert.Contains($"value=\"{taskId}\"", row);
                Assert.Contains("name=\"code\"", row);

                // Each completion form must contain an antiforgery token.
                Assert.NotEmpty(GetToken(row));
            }
        }

        // Tests 3 and 4: Manual completion works for tasks in both JSON files.
        [Theory]
        [InlineData(1, "BUC-001")]
        [InlineData(8, "BUC-008")]
        public async Task ValidManualCode_CompletesOnlySelectedTask(
            int taskId, string code)
        {
            using var environment = new IsolatedTestEnvironment();
            using var application = CreateApplication(environment);

            using var client = application.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost")
                });

            string html = await Login(client);

            html = await SubmitManualCode(client, html, taskId, code);

            string completedRow = GetTaskRow(html, taskId);

            // Verify successful completion in the rendered page.
            Assert.Contains("Task completed successfully.", completedRow);
            Assert.Contains("task-completed", completedRow);
            Assert.Contains("<span>Completed</span>", completedRow);

            // Neither completion option should remain available.
            Assert.DoesNotContain($"id=\"btn-{taskId}\"", completedRow);
            Assert.DoesNotContain($"id=\"scan-btn-{taskId}\"", completedRow);
            Assert.DoesNotContain(
                $"id=\"completeTaskForm-{taskId}\"", completedRow);

            // Unrelated tasks must still offer both completion options.
            for (int otherId = 1; otherId <= 8; otherId++)
            {
                if (otherId != taskId)
                {
                    AssertCompletionOptions(
                        GetTaskRow(html, otherId), otherId);
                }
            }

            // Verify that completion was saved to isolated server storage.
            string progressPath = Path.Combine(
                environment.ContentRootPath,
                "Data",
                "playerProgress.json"
            );

            var progress = JsonSerializer.Deserialize<
                Dictionary<string, HashSet<int>>>(
                    File.ReadAllText(progressPath));

            Assert.NotNull(progress);
            Assert.Single(progress["4235550123"]);
            Assert.Contains(taskId, progress["4235550123"]);
            Assert.Empty(progress["4235550456"]);

            // Refresh the page to confirm that completion persists.
            html = await client.GetStringAsync("/Player/Index");

            Assert.Contains(
                "task-completed",
                GetTaskRow(html, taskId)
            );
        }

        // Test 5: Incorrect manual codes preserve previously saved progress.
        [Fact]
        public async Task InvalidManualCode_PreservesExistingProgress()
        {
            using var environment = new IsolatedTestEnvironment();
            using var application = CreateApplication(environment);

            using var client = application.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost")
                });

            string html = await Login(client);

            // Successfully complete Task 1 first.
            html = await SubmitManualCode(client, html, 1, "BUC-001");

            string progressPath = Path.Combine(
                environment.ContentRootPath,
                "Data",
                "playerProgress.json"
            );

            string originalProgress = File.ReadAllText(progressPath);

            // Attempt to complete Task 2 with an incorrect code.
            html = await SubmitManualCode(client, html, 2, "WRONG-CODE");

            string incompleteRow = GetTaskRow(html, 2);

            Assert.Contains("Incorrect completion code.", incompleteRow);
            Assert.DoesNotContain("task-completed", incompleteRow);

            // Both options must remain available so the player can retry.
            AssertCompletionOptions(incompleteRow, 2);

            // Task 1 must remain completed.
            Assert.Contains("task-completed", GetTaskRow(html, 1));

            // The invalid submission must not modify saved progress.
            Assert.Equal(
                originalProgress,
                File.ReadAllText(progressPath)
            );
        }
    }
}
