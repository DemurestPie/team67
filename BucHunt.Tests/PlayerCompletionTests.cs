
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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

            // Create a fake session for Player A.
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
            httpContext.Session = session.Object;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act: Complete Task 1 for Player A.
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

        [Fact]
        public void CompleteTask_DoesNotChangeAnotherPlayersProgress()
        {
            // Arrange: Create the controller and test environment.
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

            // Act: Player A completes Task 8.
            controller.CompleteTask(8);

            // Switch to Player B.
            activePhone = "4235550456";

            ViewResult result = (ViewResult)controller.Index();

            List<ParsedTasks> tasks =
                (List<ParsedTasks>)result.Model!;

            ParsedTasks? task8 =
                tasks.FirstOrDefault(task => task.ID == 8);

            // Assert: Player B must not have Task 8 completed.
            Assert.NotNull(task8);
            Assert.False(task8.Completed);
        }

        [Fact]
        public void EnterPhone_InvalidNumber_ClearsPlayerSession()
        {
            // Arrange: Create a controller with Player A already selected.
            var environment = new TestEnvironment();
            var controller = new PlayerController(environment);

            var session = new Mock<ISession>();

            // Check that the controller removes the previous player.
            session.Setup(s => s.Remove("PlayerPhone"))
                .Verifiable();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = session.Object;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act: Submit an invalid phone number.
            ViewResult result =
                (ViewResult)controller.EnterPhone("12345");

            // Assert: Show an error and clear the previous session.
            Assert.Equal(
                "Please enter a valid 10-digit phone number.",
                result.ViewData["ErrorMessage"]
            );

            session.Verify(s => s.Remove("PlayerPhone"), Times.Once);
        }

        [Fact]
        public void EnterPhone_UnknownNumber_DoesNotGrantAccess()
        {
            // Arrange: Create a controller with a fake player session.
            var environment = new TestEnvironment();
            var controller = new PlayerController(environment);

            var session = new Mock<ISession>();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = session.Object;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act: Enter a 10-digit number that is not registered.
            ViewResult result =
                (ViewResult)controller.EnterPhone("4235559999");

            // Assert: Display an error instead of granting access.
            Assert.Equal(
                "Phone number not found. Please check your number and try again.",
                result.ViewData["ErrorMessage"]
            );

            // The previous player session must be removed.
            session.Verify(
                s => s.Remove("PlayerPhone"),
                Times.Once
            );

            // An unknown number must never be saved as the active player.
            session.Verify(
                s => s.Set(
                    "PlayerPhone",
                    It.IsAny<byte[]>()),
                Times.Never
            );
        }

        [Fact]
        public void CompleteTask_InvalidTaskId_DoesNotSaveProgress()
        {
            // Arrange: Create the controller and test player.
            var environment = new TestEnvironment();
            var controller = new PlayerController(environment);

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
            httpContext.Session = session.Object;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // TempData is needed to display an error after redirecting.
            var tempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                httpContext,
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()
            );

            controller.TempData = tempData;

            // Act: Attempt to complete a task that does not exist.
            var result = controller.CompleteTask(9999);

            // Assert: The request is rejected with a clear error.
            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Index", redirect.ActionName);

            Assert.Equal(
                "Invalid task. This task does not belong to the current hunt.",
                controller.TempData["ErrorMessage"]
            );

            // Verify that the invalid task is not shown as completed.
            var indexResult = Assert.IsType<ViewResult>(controller.Index());
            var tasks = Assert.IsAssignableFrom<List<ParsedTasks>>(indexResult.Model);

            Assert.DoesNotContain(tasks, task => task.ID == 9999);
        }




    }
}
