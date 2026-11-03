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
        // Test players and their completed tasks.
        // Each phone number has its own progress record.
        private static readonly Dictionary<string, HashSet<int>> playerProgress =
            new Dictionary<string, HashSet<int>>()
            {
        { "4235550123", new HashSet<int>() },
        { "4235550456", new HashSet<int>() }
            };

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

        // Connects a test phone number to its progress record.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EnterPhone(string phoneNumber)
        {
            string cleanedPhone = new string(
                (phoneNumber ?? "").Where(char.IsDigit).ToArray()
            );

            if (cleanedPhone.Length != 10)
            {
                ViewBag.ErrorMessage = "Please enter a valid 10-digit phone number.";
                return View();
            }

            // Only use registered test players for now.
            // Task 15.3 will improve unknown-number error handling.
            if (!playerProgress.ContainsKey(cleanedPhone))
            {
                ViewBag.ErrorMessage = "Test player not found.";
                return View();
            }

            // Remember the selected player for this browser session.
            HttpContext.Session.SetString("PlayerPhone", cleanedPhone);

            return RedirectToAction("Index");
        }

        // Loads the hunt tasks from tasks.json
        public IActionResult Index()
        {
            // Require a selected test player before displaying tasks.
            string? currentPhone = HttpContext.Session.GetString("PlayerPhone");
            // Show which test player is currently active.
            ViewBag.ActivePhone = currentPhone;

            if (currentPhone == null || !playerProgress.ContainsKey(currentPhone))
            {
                return RedirectToAction("EnterPhone");
            }
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

                // Load only the current player's completed tasks.
                string? phoneNumber = HttpContext.Session.GetString("PlayerPhone");

                if (phoneNumber != null && playerProgress.ContainsKey(phoneNumber))
                {
                    foreach (ParsedTasks task in parsedTasks)
                    {
                        task.Completed = playerProgress[phoneNumber].Contains(task.ID);
                    }
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

        // Marks a task completed for the current player only.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteTask(int id)
        {
            string? phoneNumber = HttpContext.Session.GetString("PlayerPhone");

            // Do not change progress when no test player is selected.
            if (phoneNumber == null || !playerProgress.ContainsKey(phoneNumber))
            {
                return RedirectToAction("EnterPhone");
            }

            playerProgress[phoneNumber].Add(id);

            return RedirectToAction("Index");
        }
    }
}