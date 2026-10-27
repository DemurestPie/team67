using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using BucHunt.Models;
using team67.Controllers;

namespace BucHunt.Tests
{
    public class TaskLoadingTests
    {
        [Fact]
        public void Index_LoadsTasksFromJson()
        {
            // Arrange
            var environment = new TestEnvironment();
            var controller = new PlayerController(environment);

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