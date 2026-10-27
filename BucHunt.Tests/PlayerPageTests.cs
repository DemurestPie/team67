
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BucHunt.Tests
{
    public class PlayerPageTests
    {
        [Fact]
        public async Task PlayerPage_LoadsAndDisplaysAvailableTasks()
        {
            // Arrange
            using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();

            // Act
            var response = await client.GetAsync("/Player/Index");
            string html = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Available Tasks", html);
            Assert.Contains("Find the Buccaneer Statue", html);
            Assert.Contains("id=\"taskList\"", html);
        }
    }
}
