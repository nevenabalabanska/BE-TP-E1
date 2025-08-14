using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using IdeaCenterExamPrep.Models;


namespace IdeaCenterExamPrep
{
    [TestFixture]
    public class IdeaCenterApiTests
    {
        private RestClient client;
        private static string lastCreatedIdeaId;

        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJhNmIyN2NjOS1lZWFmLTQ1YTctYjkzZi1jOGEzMDdkMjFhOGMiLCJpYXQiOiIwOC8xMi8yMDI1IDE2OjA5OjQwIiwiVXNlcklkIjoiODZmY2JiNDQtZWFmMy00ZGZiLWQyODYtMDhkZGQ0ZTA4YmQ4IiwiRW1haWwiOiJxYTEyM0BxYTEyMy5jb20iLCJVc2VyTmFtZSI6InFhMTIzIiwiZXhwIjoxNzU1MDM2NTgwLCJpc3MiOiJJZGVhQ2VudGVyX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiSWRlYUNlbnRlcl9XZWJBUElfU29mdFVuaSJ9._TJf73KzoUHAkDOpXKA8nF8AZTmpoOTbJZIkXquFjg8";

        private const string LoginEmail = "qa123@qa123.com";
        private const string LoginPassword = "qa123123";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken),
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempCLient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempCLient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if(string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }
        //All test here

        [Order(1)]
        [Test]
        public void CreateIdea_WithRequiredFields_ShouldReturnSuccess()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "Test Idea",
                Description = "This is a test idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]
        public void GetAllIdeas_ShouldReturnListOfIdeas()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);

            var responsItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responsItems, Is.Not.Null);
            Assert.That(responsItems, Is.Not.Empty);

            lastCreatedIdeaId = responsItems.LastOrDefault()?.Id;
        }

        [Order(3)]
        [Test]

        public void EditExistingIdea_ShouldReturnSuccess()
        {
            var editRequest = new IdeaDTO
            {
                Title = "Edited Idea",
                Description = "This is an updated test idea description.",
                Url = ""
            };

            var request = new RestRequest($"/api/Idea/Edit",Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));
        }

        [Order(4)]
        [Test]
        public void DeleteIdea_ShouldReturnSuccess()
        {
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);
            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }

        [Order(5)]
        [Test]

        public void CreateIdea_WithoutRequiredFields_ShouldReturnSuccessAgain()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        }

        [Order(6)]
        [Test]

        public void EditNonExistingIdea_ShouldReturnNotFound()
        {
            string nonExistingIdeaId = "123";
            var editRequest = new IdeaDTO
            {
                Title = "Edited Non-Existing Idea",
                Description = "This is an updated test idea description for a non-existing idea.",
                Url = ""
            };
            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", nonExistingIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [Order(7)]
        [Test]

        public void DeleteNonExistingIdea_ShouldReturnNotFound()
        {
            string nonExistingIdeaId = "123";
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", nonExistingIdeaId);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [OneTimeTearDown]
        public void Teardown()
        {
        this.client?.Dispose();
        }


    }
}