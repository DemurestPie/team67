
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
            // Use temporary storage instead of the real progress file.
            using var environment = new IsolatedTestEnvironment();
            var controller = new PlayerController(environment);
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
            using var environment = new IsolatedTestEnvironment();
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
        public void CompleteTask_TwoPlayersKeepSeparateProgress()
        {
            // Arrange: Create a controller using the test environment.
            using var environment = new IsolatedTestEnvironment();
            var controller = new PlayerController(environment);

            var session = new Mock<ISession>();

            // This variable lets us simulate switching between players.
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

            // Act 1: Player A completes Task 3.
            controller.CompleteTask(3);

            // Switch to Player B and check their progress.
            activePhone = "4235550456";

            var playerBResult = Assert.IsType<ViewResult>(controller.Index());
            var playerBTasks =
                Assert.IsAssignableFrom<List<ParsedTasks>>(playerBResult.Model);

            // Player B must not inherit Player A's completed Task 3.
            Assert.False(playerBTasks.Single(task => task.ID == 3).Completed);

            // Act 2: Player B completes Task 4.
            controller.CompleteTask(4);

            // Switch back to Player A.
            activePhone = "4235550123";

            var playerAResult = Assert.IsType<ViewResult>(controller.Index());
            var playerATasks =
                Assert.IsAssignableFrom<List<ParsedTasks>>(playerAResult.Model);

            // Player A keeps Task 3, but does not inherit Player B's Task 4.
            Assert.True(playerATasks.Single(task => task.ID == 3).Completed);
            Assert.False(playerATasks.Single(task => task.ID == 4).Completed);

            // Switch back to Player B and verify their completion remains saved.
            activePhone = "4235550456";

            var finalPlayerBResult = Assert.IsType<ViewResult>(controller.Index());
            var finalPlayerBTasks =
                Assert.IsAssignableFrom<List<ParsedTasks>>(finalPlayerBResult.Model);

            Assert.True(finalPlayerBTasks.Single(task => task.ID == 4).Completed);
            Assert.False(finalPlayerBTasks.Single(task => task.ID == 3).Completed);

            // Create a new controller to simulate loading saved progress again.
            var newController = new PlayerController(environment);

            newController.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Reload Player A's saved progress.
            activePhone = "4235550123";

            var reloadedA = Assert.IsType<ViewResult>(newController.Index());
            var savedTasksA =
                Assert.IsAssignableFrom<List<ParsedTasks>>(reloadedA.Model);

            // Player A should still have Task 3, but not Task 4.
            Assert.True(savedTasksA.Single(task => task.ID == 3).Completed);
            Assert.False(savedTasksA.Single(task => task.ID == 4).Completed);

            // Reload Player B's saved progress.
            activePhone = "4235550456";

            var reloadedB = Assert.IsType<ViewResult>(newController.Index());
            var savedTasksB =
                Assert.IsAssignableFrom<List<ParsedTasks>>(reloadedB.Model);

            // Player B should still have Task 4, but not Task 3.
            Assert.True(savedTasksB.Single(task => task.ID == 4).Completed);
            Assert.False(savedTasksB.Single(task => task.ID == 3).Completed);
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
            using var environment = new IsolatedTestEnvironment();
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
            // Read the saved progress directly from the test JSON file.
            string progressPath = Path.Combine(
                environment.ContentRootPath,
                "Data",
                "playerProgress.json"
            );

            string savedJson = File.ReadAllText(progressPath);

            var savedProgress =
                System.Text.Json.JsonSerializer.Deserialize<
                    Dictionary<string, HashSet<int>>
                >(savedJson);

            // Verify that the invalid task was never saved.
            Assert.NotNull(savedProgress);
            // Verify that the invalid task ID was not saved for Player A.
            Assert.DoesNotContain(9999, savedProgress["4235550123"]);
        }

        // Creates separate files so tests do not change real player progress.
        public class IsolatedTestEnvironment : TestEnvironment, IDisposable
        {
            public IsolatedTestEnvironment()
            {
                // Give this test its own temporary folder.
                ContentRootPath = Path.Combine(
                    Path.GetTempPath(),
                    "BucHuntTests",
                    Guid.NewGuid().ToString()
                );

                string dataFolder = Path.Combine(ContentRootPath, "Data");
                Directory.CreateDirectory(dataFolder);

                // Copy the hunt tasks into the temporary folder.
                string projectPath = Path.GetFullPath(
                    Path.Combine(AppContext.BaseDirectory, "../../../../")
                );

                File.Copy(
                    Path.Combine(projectPath, "Data", "tasks.json"),
                    Path.Combine(dataFolder, "tasks.json")
                );

                // Start with no completed tasks for either player.
                File.WriteAllText(
                    Path.Combine(dataFolder, "playerProgress.json"),
                    """
                {
                  "4235550123": [],
                  "4235550456": []
                }
                """
                );
            }

            // Remove the temporary files when testing finishes.
            public void Dispose()
            {
                if (Directory.Exists(ContentRootPath))
                {
                    Directory.Delete(ContentRootPath, true);
                }
            }
        }




    }
}
