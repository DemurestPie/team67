using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using BucHunt.Models;

namespace team67.Controllers
{
    public class PlayerController : Controller
    {
        private readonly IWebHostEnvironment environment;

        // Keeps track of completed tasks while the app is running.
        // PBI 8 and 9 will handle permanent/browser-local storage later.
        private static readonly HashSet<int> completedTaskIds = new HashSet<int>();

        public PlayerController(IWebHostEnvironment _environment)
        {
            environment = _environment;
        }
        // Displays the phone number entry page.
        [HttpGet]
        public IActionResult EnterPhone()
        {
            return View();
        }

        // Checks the phone number submitted by the player.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EnterPhone(string phoneNumber)
        {
            // Keep only the digits from the phone number.
            string cleanedPhone = new string(
                (phoneNumber ?? "").Where(char.IsDigit).ToArray()
            );

            // A test phone number must contain 10 digits.
            if (cleanedPhone.Length != 10)
            {
                ViewBag.ErrorMessage = "Please enter a valid 10-digit phone number.";
                return View();
            }

            // Player progress will be connected in Task 15.2.
            ViewBag.SuccessMessage = "Phone number entered successfully!";

            return View();
        }

        // Loads the hunt tasks from tasks.json
        public IActionResult Index()
        {
            string filePath = Path.Combine(
                environment.ContentRootPath,
                "Data",
                "tasks.json"
            );

            List<ParsedTasks> parsedTasks = new List<ParsedTasks>();

            try
            {
                string jsonData = System.IO.File.ReadAllText(filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                parsedTasks =
                    JsonSerializer.Deserialize<List<ParsedTasks>>(
                        jsonData,
                        options
                    ) ?? new List<ParsedTasks>();

                if (parsedTasks.Count == 0)
                {
                    throw new JsonException(
                        "Task file contains no task list."
                    );
                }

                // Apply the current completion state.
                foreach (ParsedTasks task in parsedTasks)
                {
                    task.Completed = completedTaskIds.Contains(task.ID);
                }
            }
            catch (IOException)
            {
                ViewBag.ErrorMessage =
                    "Unable to load hunt tasks. Please try again later.";
            }
            catch (UnauthorizedAccessException)
            {
                ViewBag.ErrorMessage =
                    "Unable to load hunt tasks. Please try again later.";
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage =
                    "Unable to load hunt tasks. Please try again later.";
            }

            return View(parsedTasks);
        }

        // Marks one selected task as completed.
        [HttpPost]
        public IActionResult CompleteTask(int id)
        {
            completedTaskIds.Add(id);

            return RedirectToAction("Index");
        }
    }
}