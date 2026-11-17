using System.Text.RegularExpressions;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using static BucHunt.Tests.PlayerCompletionTests;

namespace BucHunt.Tests
{
    // Task 27.3: Verify QR error handling and preservation
    // of existing player progress.
    public class QRCodeErrorTests
    {
        // Create an application using isolated test data.
        private static WebApplicationFactory<Program> CreateApplication(
            IsolatedTestEnvironment environment)
        {
            return new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                    builder.UseContentRoot(environment.ContentRootPath));
        }

        // Extract the antiforgery token from an HTML form.
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

        // Extract the rendered HTML for the selected task.
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

        // Submit through the endpoint shared by QR and manual entry.
        private static async Task<string> SubmitCode(
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

        // Test 1: Every incomplete task has a hidden error container.
        [Fact]
        public async Task IncompleteTasks_DisplayHiddenQrErrorContainers()
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

                string errorContainer = Regex.Match(
                    row,
                    $@"<div\b[^>]*id=""qr-error-{taskId}""[^>]*>"
                ).Value;

                Assert.NotEmpty(errorContainer);
                Assert.Contains("hidden", errorContainer);
                Assert.Contains("role=\"alert\"", errorContainer);

                // Manual entry must remain available.
                Assert.Contains($"id=\"btn-{taskId}\"", row);
                Assert.Contains($"id=\"completeTaskForm-{taskId}\"", row);
            }
        }

        // Test 2: The error handler loads before the submission script.
        [Fact]
        public async Task PlayerPage_LoadsQrErrorHandlerBeforeSubmission()
        {
            using var environment = new IsolatedTestEnvironment();
            using var application = CreateApplication(environment);

            using var client = application.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost")
                });

            string html = await Login(client);

            int errorScriptIndex = html.IndexOf(
                "/js/qrCodeErrors.js", StringComparison.Ordinal);

            int submissionScriptIndex = html.IndexOf(
                "/js/qrCodeSubmission.js", StringComparison.Ordinal);

            Assert.True(errorScriptIndex >= 0);
            Assert.True(submissionScriptIndex > errorScriptIndex);
        }

        // Test 3: Completed tasks no longer need QR error containers.
        [Fact]
        public async Task CompletedTasks_DoNotDisplayQrErrorContainers()
        {
            using var environment = new IsolatedTestEnvironment();
            using var application = CreateApplication(environment);

            using var client = application.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost")
                });

            string html = await Login(client);

            html = await SubmitCode(client, html, 1, "BUC-001");

            string completedRow = GetTaskRow(html, 1);

            Assert.Contains("task-completed", completedRow);
            Assert.DoesNotContain("id=\"qr-error-1\"", completedRow);

            // Incomplete tasks must retain their error containers.
            Assert.Contains("id=\"qr-error-2\"", GetTaskRow(html, 2));
        }

        // Tests 4-8: Invalid values sent to the shared endpoint
        // must not change any previously saved player progress.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("INVALID-QR")]
        [InlineData("BUC-001")]
        [InlineData("https://example.com")]
        public async Task InvalidDecodedValues_PreserveSavedProgress(
            string scannedCode)
        {
            using var environment = new IsolatedTestEnvironment();
            using var application = CreateApplication(environment);

            using var client = application.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost")
                });

            string html = await Login(client);

            // Establish existing progress before testing a failure.
            html = await SubmitCode(client, html, 1, "BUC-001");
            html = await SubmitCode(client, html, 8, "BUC-008");

            string progressPath = Path.Combine(
                environment.ContentRootPath,
                "Data",
                "playerProgress.json"
            );

            // Save the original file contents for comparison.
            string originalProgress = File.ReadAllText(progressPath);

            // Attempt to complete Task 2 with an invalid value.
            html = await SubmitCode(client, html, 2, scannedCode);

            string failedTask = GetTaskRow(html, 2);

            Assert.Contains("Incorrect completion code.", failedTask);
            Assert.DoesNotContain("task-completed", failedTask);

            // Manual entry and QR scanning must remain available.
            Assert.Contains("id=\"btn-2\"", failedTask);
            Assert.Contains("id=\"scan-btn-2\"", failedTask);
            Assert.Contains("id=\"qr-error-2\"", failedTask);

            // Previously completed tasks must remain completed.
            Assert.Contains("task-completed", GetTaskRow(html, 1));
            Assert.Contains("task-completed", GetTaskRow(html, 8));

            // Verify that the entire saved progress file is unchanged.
            Assert.Equal(
                originalProgress,
                File.ReadAllText(progressPath)
            );

            // Refresh to verify that existing progress persists.
            html = await client.GetStringAsync("/Player/Index");

            Assert.Contains("task-completed", GetTaskRow(html, 1));
            Assert.Contains("task-completed", GetTaskRow(html, 8));
            Assert.DoesNotContain("task-completed", GetTaskRow(html, 2));

            Assert.Equal(
                originalProgress,
                File.ReadAllText(progressPath)
            );
        }
    }
}