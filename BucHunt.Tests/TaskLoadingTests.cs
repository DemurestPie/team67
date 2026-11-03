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

        [Fact]
        public void Index_DisplaysOnlyActivePlayersPhoneNumber()
        {
            // Arrange: Create the player controller.
            var environment = new TestEnvironment();
            var controller = new PlayerController(environment);

            var session = new Mock<ISession>();

            // Start with Player A.
            string activePhone = "4235550123";

            session.Setup(s => s.TryGetValue(
                    "PlayerPhone",
                    out It.Ref<byte[]?>.IsAny))
                .Returns((string key, out byte[]? value) =>
                {
                    value = System.Text.Encoding.UTF8.GetBytes(activePhone);
                    return true;
                });

            var httpContext = new DefaultHttpContext();
            httpContext.Session = session.Object;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act: Load Player A's task page.
            var resultA = controller.Index() as ViewResult;

            // Assert: Only Player A's number is provided to the view.
            Assert.NotNull(resultA);
            Assert.Equal("4235550123", resultA.ViewData["ActivePhone"]);
            Assert.NotEqual("4235550456", resultA.ViewData["ActivePhone"]);

            // Switch to Player B.
            activePhone = "4235550456";

            // Act: Load Player B's task page.
            var resultB = controller.Index() as ViewResult;

            // Assert: Only Player B's number is provided to the view.
            Assert.NotNull(resultB);
            Assert.Equal("4235550456", resultB.ViewData["ActivePhone"]);
            Assert.NotEqual("4235550123", resultB.ViewData["ActivePhone"]);
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