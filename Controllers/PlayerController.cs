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

        // PBI 34: Each JSON file in Data/Tasks contains the existing task array format.
        // Keep progress data outside this folder so it is never treated as tasks.
        private List<ParsedTasks> LoadTasks()
        {
            string taskFolder = Path.Combine(
                environment.ContentRootPath, "Data", "Tasks"
            );

            var tasks = new List<ParsedTasks>();
            var taskIds = new HashSet<int>();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Sort filenames so the combined list has a predictable order.
            foreach (string filePath in Directory.GetFiles(taskFolder, "*.json")
                .OrderBy(path => path, StringComparer.Ordinal))
            {
                var fileTasks = JsonSerializer.Deserialize<List<ParsedTasks>>(
                    System.IO.File.ReadAllText(filePath), options
                ) ?? throw new JsonException("Task file must contain a task array.");

                foreach (ParsedTasks task in fileTasks)
                {
                    // IDs must be unique across files because progress is stored by ID.
                    if (task == null || !taskIds.Add(task.ID))
                    {
                        throw new JsonException("Task entries must have unique IDs.");
                    }

                    tasks.Add(task);
                }
            }

            if (tasks.Count == 0)
            {
                throw new JsonException("Task files contain no task list.");
            }

            // Return only after every file passes validation; never use a partial hunt.
            return tasks;
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

            List<ParsedTasks> parsedTasks =
                new List<ParsedTasks>();

            try
            {
                // PBI 34: Render the combined task list from every local task file.
                parsedTasks = LoadTasks();

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

            // PBI 34: Validate against the same files used to display tasks.
            List<ParsedTasks> tasks = LoadTasks();

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