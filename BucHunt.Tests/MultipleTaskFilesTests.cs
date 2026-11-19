using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BucHunt.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using team67.Controllers;
using static BucHunt.Tests.PlayerCompletionTests;

namespace BucHunt.Tests
{
    // PBI 34 regression tests use isolated data so real player progress is untouched.
    public class MultipleTaskFilesTests
    {
        private static PlayerController CreateController(IsolatedTestEnvironment environment)
        {
            var session = new Mock<ISession>();
            session.Setup(s => s.TryGetValue("PlayerPhone", out It.Ref<byte[]?>.IsAny))
                .Returns((string key, out byte[]? value) =>
                {
                    value = Encoding.UTF8.GetBytes("4235550123");
                    return true;
                });
            var context = new DefaultHttpContext { Session = session.Object };
            return new PlayerController(environment)
            {
                ControllerContext = new ControllerContext { HttpContext = context },
                TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>())
            };
        }

        private static string TaskFolder(IsolatedTestEnvironment environment) =>
            Path.Combine(environment.ContentRootPath, "Data", "Tasks");

        private static List<ParsedTasks> DisplayedTasks(PlayerController controller) =>
            Assert.IsType<List<ParsedTasks>>(Assert.IsType<ViewResult>(controller.Index()).Model);

        [Fact]
        public void Index_DiscoversNewFilesOnNextRequest_AndKeepsFileOrder()
        {
            using var environment = new IsolatedTestEnvironment();
            var controller = CreateController(environment);
            Assert.Equal(8, DisplayedTasks(controller).Count);

            // No controller, view, or manifest changes are needed for another file.
            File.WriteAllText(Path.Combine(TaskFolder(environment), "03-extra.json"),
                """[{"ID":9,"Label":"New task","CompletionCode":"BUC-009"}]""");

            Assert.Equal(Enumerable.Range(1, 9), DisplayedTasks(controller).Select(t => t.ID));
            Assert.Null(controller.ViewBag.ErrorMessage);
        }

        [Fact]
        public void Index_IgnoresNonTaskFiles_AndAllowsAnEmptyArrayAlongsideTasks()
        {
            using var environment = new IsolatedTestEnvironment();
            File.WriteAllText(Path.Combine(TaskFolder(environment), "notes.txt"), "not JSON");
            File.WriteAllText(Path.Combine(TaskFolder(environment), "03-empty.json"), "[]");
            // JSON outside Data/Tasks must not be read as task data.
            File.WriteAllText(Path.Combine(environment.ContentRootPath, "Data", "other.json"), "invalid");
            var controller = CreateController(environment);
            Assert.Equal(8, DisplayedTasks(controller).Count);
            Assert.Null(controller.ViewBag.ErrorMessage);
        }

        [Theory]
        [InlineData("broken JSON")]
        [InlineData("{}")]
        [InlineData("null")]
        [InlineData("[null]")]
        [InlineData("[{\"id\":1,\"label\":\"Duplicate\",\"completionCode\":\"OTHER\"}]")]
        [InlineData("[{\"id\":9},{\"id\":9}]")]
        public void InvalidFile_RejectsWholeHunt_AndDoesNotChangeProgress(string json)
        {
            using var environment = new IsolatedTestEnvironment();
            File.WriteAllText(Path.Combine(TaskFolder(environment), "03-invalid.json"), json);
            var controller = CreateController(environment);
            string progressPath = Path.Combine(environment.ContentRootPath, "Data", "playerProgress.json");
            string original = File.ReadAllText(progressPath);

            Assert.Empty(DisplayedTasks(controller));
            Assert.Equal("Unable to load hunt tasks. Please try again later.",
                (string)controller.ViewBag.ErrorMessage);
            controller.SubmitCode(1, "BUC-001");
            Assert.Equal("Unable to validate the submitted code.", controller.TempData["ErrorMessage"]);
            Assert.Equal(original, File.ReadAllText(progressPath));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MissingOrEmptyTaskFolder_ShowsExistingError(bool removeFolder)
        {
            using var environment = new IsolatedTestEnvironment();
            Directory.Delete(TaskFolder(environment), true);
            if (!removeFolder)
                Directory.CreateDirectory(TaskFolder(environment));

            var controller = CreateController(environment);
            Assert.Empty(DisplayedTasks(controller));
            Assert.NotNull(controller.ViewBag.ErrorMessage);
        }

        [Fact]
        public void EmptyTaskArrays_ShowExistingError()
        {
            using var environment = new IsolatedTestEnvironment();
            foreach (string path in Directory.GetFiles(TaskFolder(environment), "*.json"))
                File.WriteAllText(path, "[]");
            var controller = CreateController(environment);
            Assert.Empty(DisplayedTasks(controller));
            Assert.NotNull(controller.ViewBag.ErrorMessage);
        }

        [Fact]
        public void SubmitCode_UsesSecondFile_PreservesPlayerIsolation_AndIsIdempotent()
        {
            using var environment = new IsolatedTestEnvironment();
            var controller = CreateController(environment);
            controller.SubmitCode(8, "BUC-008");
            controller.SubmitCode(8, "BUC-008");

            var tasks = DisplayedTasks(CreateController(environment));
            Assert.True(tasks.Single(t => t.ID == 8).Completed);
            Assert.All(tasks.Where(t => t.ID != 8), t => Assert.False(t.Completed));
            var progress = JsonSerializer.Deserialize<Dictionary<string, List<int>>>(
                File.ReadAllText(Path.Combine(environment.ContentRootPath, "Data", "playerProgress.json")))!;
            Assert.Equal(new[] { 8 }, progress["4235550123"]);
            Assert.Empty(progress["4235550456"]);
        }

        [Theory]
        [InlineData(8, "BUC-001")]
        [InlineData(999, "BUC-008")]
        [InlineData(8, "")]
        public void SubmitCode_RejectsWrongCodeOrUnknownTask(int taskId, string code)
        {
            using var environment = new IsolatedTestEnvironment();
            var controller = CreateController(environment);
            controller.SubmitCode(taskId, code);
            Assert.Equal("Incorrect completion code.", controller.TempData["ErrorMessage"]);
            Assert.All(DisplayedTasks(controller), t => Assert.False(t.Completed));
        }

        [Fact]
        public async Task PlayerPage_RendersTasksFromEveryFile_IncludingNewFiles()
        {
            using var environment = new IsolatedTestEnvironment();
            using var application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.UseContentRoot(environment.ContentRootPath));
            using var client = application.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

            // Log in through the real form so session and antiforgery behavior are exercised.
            string loginHtml = await client.GetStringAsync("/Player/EnterPhone");
            string token = Regex.Match(loginHtml,
                "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
            Assert.NotEmpty(token);
            using var response = await client.PostAsync("/Player/EnterPhone",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["phoneNumber"] = "4235550123",
                    ["__RequestVerificationToken"] = System.Net.WebUtility.HtmlDecode(token)
                }));
            response.EnsureSuccessStatusCode();
            string html = await response.Content.ReadAsStringAsync();
            Assert.Contains("data-task-id=\"1\"", html);
            Assert.Contains("data-task-id=\"8\"", html);

            File.WriteAllText(Path.Combine(TaskFolder(environment), "03-extra.json"),
                """[{"id":9,"label":"New task","completionCode":"BUC-009"}]""");
            html = await client.GetStringAsync("/Player/Index");
            Assert.Contains("data-task-id=\"9\"", html);
            Assert.Contains("New task", html);
            Assert.Contains("completeTaskForm-9", html);
        }
    }
}
