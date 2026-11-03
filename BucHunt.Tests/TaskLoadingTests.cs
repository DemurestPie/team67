using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using BucHunt.Models;
using team67.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Moq;

namespace BucHunt.Tests
{
    public class TaskLoadingTests
    {
        [Fact]
        public void Index_LoadsTasksFromJson()
        {
            var environment = new TestEnvironment();
            var controller = new PlayerController(environment);

            // Create a fake session for our registered test player.
            var session = new Mock<ISession>();

            session.Setup(s => s.TryGetValue(
                    "PlayerPhone",
                    out It.Ref<byte[]?>.IsAny))
                .Returns((string key, out byte[]? value) =>
                {
                    value = System.Text.Encoding.UTF8.GetBytes("4235550123");
                    return true;
                });

            var httpContext = new DefaultHttpContext();

            // Give the controller a fake session for testing.
            httpContext.Session = session.Object;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = controller.Index() as ViewResult;
            var tasks = result?.Model as List<ParsedTasks>;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(tasks);
            Assert.Equal(8, tasks.Count);
            Assert.Equal(1, tasks[0].ID);
            Assert.Equal("Find the Buccaneer Statue", tasks[0].Label);
            Assert.Null(controller.ViewBag.ErrorMessage);
        }
    }

    public class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "BucHunt";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } =
            Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "../../../../")
            );
        public string WebRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();

        public IFileProvider WebRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}