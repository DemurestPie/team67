using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using team67.Controllers;
using BucHunt.Models;

namespace BucHunt.Tests
{
    public class PlayerCompletionTests
    {
        [Fact]
        public void CompleteTask_MarksSelectedTaskAsCompleted()
        {
            // Arrange
            var environment = new Mock<IWebHostEnvironment>();

            environment
                .Setup(e => e.ContentRootPath)
                .Returns(Directory.GetCurrentDirectory());

            var controller = new PlayerController(environment.Object);

            // Act
            controller.CompleteTask(1);

            ViewResult result = (ViewResult)controller.Index();

            List<ParsedTasks> tasks =
                (List<ParsedTasks>)result.Model!;

            ParsedTasks? completedTask =
                tasks.FirstOrDefault(task => task.ID == 1);

            // Assert
            Assert.NotNull(completedTask);
            Assert.True(completedTask.Completed);
        }
    }
}