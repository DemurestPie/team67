using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using BucHunt.Models;

namespace team67.Controllers
{
    public class PlayerController : Controller
    {
        private readonly IWebHostEnvironment environment;

        // Test players that are allowed to access the hunt.
        private static readonly HashSet<string> registeredPlayers =
            new HashSet<string>()
            {
                "4235550123",
                "4235550456"
            };

        public PlayerController(IWebHostEnvironment _environment)
        {
            environment = _environment;
        }

        // Loads saved player progress from the server.
        private Dictionary<string, HashSet<int>> LoadPlayerProgress()
        {
            string filePath = Path.Combine(
                environment.ContentRootPath,
                "Data",
                "playerProgress.json"
            );

            string jsonData = System.IO.File.ReadAllText(filePath);

            return JsonSerializer.Deserialize<Dictionary<string, HashSet<int>>>(jsonData)
                ?? new Dictionary<string, HashSet<int>>();
        }

        // Saves player progress to the server.
        private void SavePlayerProgress(
            Dictionary<string, HashSet<int>> progress)
        {
            string filePath = Path.Combine(
                environment.ContentRootPath,
                "Data",
                "playerProgress.json"
            );

            string jsonData = JsonSerializer.Serialize(
                progress,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            System.IO.File.WriteAllText(filePath, jsonData);
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
            // Clear the previous player before checking a new phone number.
            HttpContext.Session.Remove("PlayerPhone");

            string cleanedPhone = new string(
                (phoneNumber ?? "").Where(char.IsDigit).ToArray()
            );

            if (cleanedPhone.Length != 10)
            {
                ViewBag.ErrorMessage =
                    "Please enter a valid 10-digit phone number.";

                return View();
            }

            // Reject phone numbers that are not registered players.
            if (!registeredPlayers.Contains(cleanedPhone))
            {
                ViewBag.ErrorMessage =
                    "Phone number not found. Please check your number and try again.";

                return View();
            }

            // Remember the active player for this browser session.
            HttpContext.Session.SetString(
                "PlayerPhone",
                cleanedPhone
            );

            return RedirectToAction("Index");
        }

        // Loads the hunt tasks and the current player's saved progress.
        public IActionResult Index()
        {
            string? currentPhone =
                HttpContext.Session.GetString("PlayerPhone");

            if (currentPhone == null ||
                !registeredPlayers.Contains(currentPhone))
            {
                return RedirectToAction("EnterPhone");
            }

            ViewBag.ActivePhone = currentPhone;

            string filePath = Path.Combine(
                environment.ContentRootPath,
                "Data",
                "tasks.json"
            );

            List<ParsedTasks> parsedTasks =
                new List<ParsedTasks>();

            try
            {
                string jsonData =
                    System.IO.File.ReadAllText(filePath);

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

                // Load the current player's progress from server storage.
                Dictionary<string, HashSet<int>> progress =
                    LoadPlayerProgress();

                if (!progress.ContainsKey(currentPhone))
                {
                    progress[currentPhone] =
                        new HashSet<int>();
                }

                foreach (ParsedTasks task in parsedTasks)
                {
                    task.Completed =
                        progress[currentPhone].Contains(task.ID);
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

        // Marks a valid task completed and saves it under the active phone number.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteTask(int id)
        {
            string? phoneNumber =
                HttpContext.Session.GetString("PlayerPhone");

            if (phoneNumber == null ||
                !registeredPlayers.Contains(phoneNumber))
            {
                return RedirectToAction("EnterPhone");
            }

            // Load the current hunt's tasks so the ID can be validated.
            string filePath = Path.Combine(
                environment.ContentRootPath,
                "Data",
                "tasks.json"
            );

            List<ParsedTasks> tasks;

            try
            {
                string jsonData =
                    System.IO.File.ReadAllText(filePath);

                tasks =
                    JsonSerializer.Deserialize<List<ParsedTasks>>(
                        jsonData,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }
                    ) ?? new List<ParsedTasks>();
            }
            catch (IOException)
            {
                TempData["ErrorMessage"] =
                    "Unable to validate the selected task.";

                return RedirectToAction("Index");
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] =
                    "Unable to validate the selected task.";

                return RedirectToAction("Index");
            }
            catch (JsonException)
            {
                TempData["ErrorMessage"] =
                    "Unable to validate the selected task.";

                return RedirectToAction("Index");
            }

            // PBI 16.4: reject IDs that do not belong to the current hunt.
            bool validTask =
                tasks.Any(task => task.ID == id);

            if (!validTask)
            {
                TempData["ErrorMessage"] =
                    "Invalid task. This task does not belong to the current hunt.";

                return RedirectToAction("Index");
            }

            // PBI 16.1: load progress for the active player.
            Dictionary<string, HashSet<int>> progress =
                LoadPlayerProgress();

            if (!progress.ContainsKey(phoneNumber))
            {
                progress[phoneNumber] =
                    new HashSet<int>();
            }

            // Save the valid completed task under that player's phone number.
            progress[phoneNumber].Add(id);

            SavePlayerProgress(progress);

            return RedirectToAction("Index");
        }
    }
}