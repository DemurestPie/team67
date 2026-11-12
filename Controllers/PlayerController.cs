using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using BucHunt.Models;
using System.Text.RegularExpressions;


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


        // Task 22.1: Check whether the submitted code matches
        // the configured completion code for the selected task.
        private bool IsValidCompletionCode(int taskId, string? submittedCode)
        {
            // Reject an empty or missing code.
            if (string.IsNullOrWhiteSpace(submittedCode))
            {
                return false;
            }

            // Find the file containing the hunt tasks.
            string filePath = Path.Combine(
                environment.ContentRootPath,
                "Data",
                "tasks.json"
            );

            // Read the tasks and their configured completion codes.
            string jsonData = System.IO.File.ReadAllText(filePath);

            List<ParsedTasks> tasks =
                JsonSerializer.Deserialize<List<ParsedTasks>>(
                    jsonData,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                ) ?? new List<ParsedTasks>();

            // Find the task that matches the submitted task ID.
            ParsedTasks? selectedTask =
                tasks.FirstOrDefault(task => task.ID == taskId);

            // Reject task IDs that do not exist.
            if (selectedTask == null)
            {
                return false;
            }

            // Reject tasks that have no configured completion code.
            if (string.IsNullOrWhiteSpace(selectedTask.CompletionCode))
            {
                return false;
            }

            return TaskCodeValidator.IsValid(
            tasks,
            taskId,
            submittedCode
            );
        }


        // Task 22.2: Complete the selected task only when
        // the player submits its correct completion code.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitCode(int taskId, string? code)
        {
            // Get the current player from the session.
            string? phoneNumber =
                HttpContext.Session.GetString("PlayerPhone");

            // Only registered players can submit completion codes.
            if (phoneNumber == null ||
                !registeredPlayers.Contains(phoneNumber))
            {
                return RedirectToAction("EnterPhone");
            }

            // Task 23.3: Associate validation feedback with the submitted task.
            // The view uses this ID to show feedback beside the correct task.
            TempData["FeedbackTaskId"] = taskId;

            bool validCode;

            try
            {
                // Task 22.1 checks the submitted code
                // against the selected task's configured code.
                validCode = IsValidCompletionCode(taskId, code);
            }
            catch (IOException)
            {
                TempData["ErrorMessage"] =
                    "Unable to validate the submitted code.";

                return RedirectToAction("Index");
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] =
                    "Unable to validate the submitted code.";

                return RedirectToAction("Index");
            }
            catch (JsonException)
            {
                TempData["ErrorMessage"] =
                    "Unable to validate the submitted code.";

                return RedirectToAction("Index");
            }

            // An incorrect code must not change saved progress.
            if (!validCode)
            {
                TempData["ErrorMessage"] =
                    "Incorrect completion code.";

                return RedirectToAction("Index");
            }

// Load the saved progress for all players.
            Dictionary<string, HashSet<int>> progress =
                LoadPlayerProgress();

            // Create a progress entry if this player has none.
            if (!progress.ContainsKey(phoneNumber))
            {
                progress[phoneNumber] = new HashSet<int>();
            }

            // Mark only the selected task as completed
            // for the current player.
            progress[phoneNumber].Add(taskId);

            // Save the updated progress to the server.
            SavePlayerProgress(progress);

            // Show a success message besides the tasks
            // when the correct completion code is submitted.
            TempData["SuccessMessage"] =
                "Task completed successfully.";

            return RedirectToAction("Index");
        }




        // Legacy endpoint: Task completion now requires a valid code.
        // This method no longer changes player progress.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteTask(int id)
        {
            // Identify the player using the current session.
            string? phoneNumber =
                HttpContext.Session.GetString("PlayerPhone");

            // Redirect players who are not registered.
            if (phoneNumber == null ||
                !registeredPlayers.Contains(phoneNumber))
            {
                return RedirectToAction("EnterPhone");
            }

            // Prevent completing a task without code validation.
            TempData["ErrorMessage"] =
                "Please enter a completion code to complete this task.";

            return RedirectToAction("Index");
        }

    }
}