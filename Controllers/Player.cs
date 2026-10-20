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

            if (System.IO.File.Exists(filePath))
            {
                string jsonData = System.IO.File.ReadAllText(filePath);

                var upperCaseJsonData = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                parsedTasks = JsonSerializer.Deserialize<List<ParsedTasks>>(jsonData, upperCaseJsonData);
            }
            return View(parsedTasks);
        }
    }
}