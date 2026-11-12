
namespace BucHunt.Models
{
    public static class TaskCodeValidator
    {
        // Task 22.1: Check the submitted code against
        // the configured code for the selected task.
        public static bool IsValid(
            IEnumerable<ParsedTasks> tasks,
            int taskId,
            string? submittedCode)
        {
            // A missing or empty code cannot be valid.
            if (string.IsNullOrWhiteSpace(submittedCode))
            {
                return false;
            }

            // Find the task using its ID.
            ParsedTasks? selectedTask =
                tasks.FirstOrDefault(task => task.ID == taskId);

            // Reject an unknown task or a task without a code.
            if (selectedTask == null ||
                string.IsNullOrWhiteSpace(selectedTask.CompletionCode))
            {
                return false;
            }

            // Compare the submitted code to the configured code.
            // Normalize the codes by removing leading/trailing spaces
            // and ignoring capitalization
            string normalizedSubmittedCode = submittedCode.Trim();
            string normalizedCompletionCode = selectedTask.CompletionCode.Trim();
            
            return string.Equals(
                normalizedCompletionCode,
                normalizedSubmittedCode,
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}

