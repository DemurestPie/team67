namespace BucHunt.Models
{
    public class ParsedTasks
    {
        public ParsedTasks()
        {
        }
        public int ID { get; set; }
        public string Label { get; set; } = string.Empty;

        public bool Completed { get; set; } = false;
        public ParsedTasks(int Id, string label)
        {
            ID = Id;
            Label = label;
        }
    }
}