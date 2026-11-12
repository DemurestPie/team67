namespace BucHunt.Models
{
    public class ParsedTasks
    {
        // Required when reading tasks from JSON.
        public ParsedTasks()
        {
        }

        public int ID { get; set; }

        public string Label { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public bool Completed { get; set; } = false;

        // The configured code used by the existing validation logic.
        public string CompletionCode { get; set; } = string.Empty;

        // Preserve the existing two-parameter constructor.
        public ParsedTasks(int id, string label)
        {
            ID = id;
            Label = label;
        }

        // Preserve the three-parameter constructor from Task21.3-21.4.
        public ParsedTasks(int id, string label, string code)
        {
            ID = id;
            Label = label;
            Code = code;
        }
    }
}