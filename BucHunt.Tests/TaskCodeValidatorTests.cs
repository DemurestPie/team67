
using BucHunt.Models;

namespace BucHunt.Tests
{
    public class TaskCodeValidatorTests
    {
        // Use test tasks with known completion codes.
        private readonly List<ParsedTasks> tasks = new()
        {
            new ParsedTasks
            {
                ID = 1,
                Label = "Find the Buccaneer Statue",
                CompletionCode = "BUC-001"
            },
            new ParsedTasks
            {
                ID = 2,
                Label = "Visit the Sherrod Library",
                CompletionCode = "BUC-002"
            }
        };

        [Fact]
        public void ValidCode_ReturnsTrue()
        {
            bool result = TaskCodeValidator.IsValid(
                tasks, 1, "BUC-001");

            Assert.True(result);
        }

        [Fact]
        public void CodeForDifferentTask_ReturnsFalse()
        {
            bool result = TaskCodeValidator.IsValid(
                tasks, 1, "BUC-002");

            Assert.False(result);
        }

        [Fact]
        public void IncorrectCode_ReturnsFalse()
        {
            bool result = TaskCodeValidator.IsValid(
                tasks, 1, "WRONG");

            Assert.False(result);
        }

        [Fact]
        public void UnknownTaskId_ReturnsFalse()
        {
            bool result = TaskCodeValidator.IsValid(
                tasks, 99, "BUC-001");

            Assert.False(result);
        }

        [Fact]
        public void EmptyCode_ReturnsFalse()
        {
            bool result = TaskCodeValidator.IsValid(
                tasks, 1, "");

            Assert.False(result);
        }

        [Fact]
        public void MissingConfiguredCode_ReturnsFalse()
        {
            bool result = TaskCodeValidator.IsValid(
                new List<ParsedTasks>
                {
                    new ParsedTasks
                    {
                        ID = 3,
                        Label = "Test task"
                    }
                },
                3,
                "BUC-003");

            Assert.False(result);
        }
    }
}

