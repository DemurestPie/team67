
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BucHunt.Tests
{
    public class PlayerPageTests
    {
        [Fact]
        public async Task PlayerPage_WithoutSession_RedirectsToPhoneEntry()
        {
            // Arrange
            using var application = new WebApplicationFactory<Program>();

            // Do not automatically follow redirects.
            using var client = application.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

            // Act
            var response = await client.GetAsync("/Player/Index");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            Assert.Contains(
                "/Player/EnterPhone",
                response.Headers.Location?.ToString()
            );
        }

        [Fact]
        public async Task PhoneEntryPage_DisplaysPhoneNumberForm()
        {
            // Arrange
            using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();

            // Act
            var response = await client.GetAsync("/Player/EnterPhone");
            string html = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Player Entry", html);
            Assert.Contains("Phone Number", html);
            Assert.Contains("name=\"phoneNumber\"", html);
        }
    }
}
