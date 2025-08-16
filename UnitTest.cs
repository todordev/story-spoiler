using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;
using ExamBackendTestAutomation.Models;

namespace ExamBackendTestAutomation
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string baseURL = "https://d3s5nxhwblsjbi.cloudfront.net";
        private static string createdStoryId;

        [OneTimeSetUp]
        public void Setup()
        {
            var loginClient = new RestClient(baseURL);

            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username = "todqaexp101", password = "qwerty123!@#" });

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            var token = json.GetProperty("accessToken").GetString();

            var options = new RestClientOptions(baseURL)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        [Test, Order(1)]
        public void CreateNewStorySpoilerWithRequiredFields()
        {
            // Arrange
            var request = new RestRequest("/api/Story/Create", Method.Post);
            var story = new StoryDTO
            {
                Title = "Story " + DateTime.Now.Ticks,
                Description = "Test story description by my.",
                Url = "https://funfex.com/images/sampledata/cassiopeia/nasa3-640.jpg"
            };
            request.AddJsonBody(story);

            // Act
            var response = client.Execute(request);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(responseData.StoryId, Is.Not.Null);
            Assert.That(responseData.Msg, Is.EqualTo("Successfully created!"));

            // Store for future tests
            createdStoryId = responseData.StoryId;
        }

        [Test, Order(2)]
        public void EditStorySpoilerThatWasCreated()
        {
            // Arrange
            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);
            var updatedStory = new StoryDTO
            {
                Title = "Story UPDATED",
                Description = "The story was updated by me for testing the updates functionality.",
                Url = "https://funfex.com/images/sampledata/cassiopeia/nasa1-640.jpg"
            };
            request.AddJsonBody(updatedStory);

            // Act
            var response = client.Execute(request);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseData.Msg, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllStorySpoilers()
        {
            // Arrange
            var request = new RestRequest("/api/Story/All", Method.Get);

            // Act
            var response = client.Execute(request);
            var stories = JsonSerializer.Deserialize<JsonElement>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(stories.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(stories.GetArrayLength(), Is.GreaterThan(0));
        }

        [Test, Order(4)]
        public void DeleteStorySpoiler()
        {
            // Arrange
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);

            // Act
            var response = client.Execute(request);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseData.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void TryToCreateStorySpoilerWithoutRequiredFields()
        {
            // Arrange
            var request = new RestRequest("/api/Story/Create", Method.Post);
            var incompleteStory = new StoryDTO
            {
                Url = "https://funfex.com/images/sampledata/cassiopeia/nasa2-1200.jpg"
            };
            request.AddJsonBody(incompleteStory);

            // Act
            var response = client.Execute(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingStorySpoiler()
        {
            // Arrange
            var request = new RestRequest("/api/Story/Edit/b623373e-90b3-4ec6-8b70-8a419fd02cec", Method.Put);
            var story = new StoryDTO
            {
                Title = "Wrong story",
                Description = "The story does not exists right now!",
                Url = "https://funfex.com/images/sampledata/cassiopeia/nasa3-1200.jpg"
            };
            request.AddJsonBody(story);

            // Act
            var response = client.Execute(request);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(responseData.Msg, Does.Contain("No spoilers"));
        }

        [Test, Order(7)]
        public void DeleteNonExistingStorySpoiler()
        {
            // Arrange
            var request = new RestRequest("/api/Story/Delete/b623373e-90b3-4ec6-8b70-8a419fd02cef", Method.Delete);

            // Act
            var response = client.Execute(request);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(responseData.Msg, Is.EqualTo("Unable to delete this story spoiler!"));
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}