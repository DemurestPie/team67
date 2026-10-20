using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Dynamic;
using BucHunt.Models;

namespace team67.Controllers
{
    public class PlayerController : Controller
    {
        private readonly IWebHostEnvironment environment;

        public PlayerController(IWebHostEnvironment _environment)
        {
            environment = _environment;
        }

        public IActionResult Index()
        {
            string filePath = Path.Combine(environment.ContentRootPath, "Data", "tasks.json");

            List<ParsedTasks> parsedTasks = new List<ParsedTasks>();

            try
            {
                string jsonData = System.IO.File.ReadAllText(filePath);

                var upperCaseJsonData = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                parsedTasks = JsonSerializer.Deserialize<List<ParsedTasks>>(jsonData, upperCaseJsonData);

                if (parsedTasks == null)
                {
                    throw new JsonException("Task file contains no task list.");
                }
            }
            catch (IOException)
            {
                ViewBag.ErrorMessage = "Unable to load hunt tasks. Please try again later.";
            }
            catch (UnauthorizedAccessException)
            {
                ViewBag.ErrorMessage = "Unable to load hunt tasks. Please try again later.";
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage = "Unable to load hunt tasks. Please try again later.";
            }

            return View(parsedTasks);
        }
    }
}