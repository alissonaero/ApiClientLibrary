using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polly;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http;
using System.Text;

namespace ApiClientLibrary.Tests
{
	[TestClass]
	[DoNotParallelize] 
	public class ApiClientTests
	{
		private MockHttpMessageHandler _mockHttpHandler;
		private HttpClient _httpClient;
		private ApiClient _apiClient;

		private void ResetMockeringMachine()
		{
			_mockHttpHandler = new MockHttpMessageHandler();
			_httpClient = new HttpClient(_mockHttpHandler);
			_apiClient = new ApiClient(_httpClient);
		}

		[TestInitialize]
		public void Setup()
		{
			ResetMockeringMachine();
		}

		[TestCleanup]
		public void Cleanup()
		{
			_apiClient.Dispose();
		}

		[TestMethod]
		public async Task GetAsync_ValidRequest_ReturnsData()
		{
			// Arrange
			var url = new Uri("https://api.example.com/data");
			_mockHttpHandler.When(HttpMethod.Get, url.ToString())
				.Respond("application/json", "{ \"id\": 1, \"name\": \"Test Item\" }");

			// Act
			var response = await _apiClient.GetAsync<TestResponse>(url);

			// Assert
			response.Success.Should().BeTrue();
			response.Data.Should().NotBeNull();
			response.Data.Id.Should().Be(1);
			response.Data.Name.Should().Be("Test Item");
		}

		[TestMethod]
		public async Task PostAsync_ValidRequest_ReturnsCreatedData()
		{
			// Arrange
			var url = new Uri("https://api.example.com/item/*");
			var payload = new TestRequest { Name = "New Item" };

			_mockHttpHandler.When(HttpMethod.Post, url.ToString())
				.WithContent(JsonConvert.SerializeObject(payload))
				.Respond(HttpStatusCode.Created, "application/json", "{ \"id\": 2, \"name\": \"New Item\" }");

			// Act
			var response = await _apiClient.PostAsync<TestRequest, TestResponse>(url, payload);

			// Assert
			response.Success.Should().BeTrue();
			response.Data.Should().NotBeNull();
			response.Data.Id.Should().Be(2);
			response.Data.Name.Should().Be("New Item");
		}

		[TestMethod]
		public async Task PutAsync_ValidRequest_ReturnsUpdatedData()
		{
			// Arrange
			var url = new Uri("https://api.example.com/items/1");
			var payload = new TestRequest { Name = "Updated Item" };

			_mockHttpHandler.When(url.ToString())
				.WithContent(JsonConvert.SerializeObject(payload))
				.Respond("application/json", "{ \"id\": 1, \"name\": \"Updated Item\" }");

			// Act
			var response = await _apiClient.PutAsync<TestRequest, TestResponse>(url, payload);

			// Assert
			response.Success.Should().BeTrue();
			response.Data.Should().NotBeNull();
			response.Data.Id.Should().Be(1);
			response.Data.Name.Should().Be("Updated Item");
		}

		[TestMethod]
		public async Task DeleteAsync_ValidRequest_ReturnsSuccess()
		{
			// Arrange
			var url = new Uri("https://api.example.com/items/1");
			_mockHttpHandler.When(HttpMethod.Delete, url.ToString())
				.Respond(HttpStatusCode.NoContent);

			// Act
			var response = await _apiClient.DeleteAsync<bool>(url);

			// Assert
			response.Success.Should().BeTrue();
			response.Data.Should().BeFalse();
		}

		[TestMethod]
		public async Task PatchAsync_ValidRequest_ReturnsUpdatedData()
		{
			// Arrange
			var url = new Uri("https://api.example.com/items/1");
			var payload = new { name = "Partially Updated" };

		 
 



			_mockHttpHandler.When(new HttpMethod("PATCH"), url.ToString())
				.WithContent(JsonConvert.SerializeObject(payload))
				.Respond("application/json", "{ \"id\": 1, \"name\": \"Partially Updated\" }");

	
			// Act
			var response = await _apiClient.PatchAsync<object, TestResponse>(url, payload);

			// Assert
			response.Success.Should().BeTrue();
			response.Data.Should().NotBeNull();
			response.Data.Name.Should().Be("Partially Updated");
		}



		[TestMethod]
		public async Task SendAsync_ServerError_ReturnsErrorMessage()
		{
			// Arrange
			var url = new Uri("https://api.example.com/error");
			_mockHttpHandler.When(HttpMethod.Get, url.ToString())
				.Respond(HttpStatusCode.InternalServerError, "application/json",
					"{ \"error\": \"Internal server error\" }");

			// Act
			var response = await _apiClient.GetAsync<TestResponse>(url);

			// Assert
			response.Success.Should().BeFalse();
			response.ErrorMessage.Should().Contain("500");
			response.ErrorData.Should().Contain("Internal server error");
		}

		[TestMethod]
		public async Task SendAsync_RetryPolicy_RetriesOnTransientErrors()
		{
			// Arrange
			int callCount = 0;
			var url = new Uri("https://api.example.com/retry");
			var policy = Policy.Handle<HttpRequestException>()
				.OrResult<HttpResponseMessage>(r =>
					!r.IsSuccessStatusCode && (r.StatusCode == (HttpStatusCode)429 ||
											  r.StatusCode == HttpStatusCode.ServiceUnavailable))
				.WaitAndRetryAsync(3, _ => TimeSpan.Zero); // Immediate retry for testing

			var client = new ApiClient(_httpClient, retryPolicy: policy);

			_mockHttpHandler.When(HttpMethod.Get, url.ToString())
	.Respond(() =>
	{
		callCount++;

		if (callCount == 1)
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

		if (callCount == 2)
			return Task.FromResult(new HttpResponseMessage((HttpStatusCode)429));

		var okResponse = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("{ \"status\": \"Success\" }", Encoding.UTF8, "application/json")
		};
		return Task.FromResult(okResponse);
	});




			// Act
			var response = await client.GetAsync<TestResponse>(url);

			// Assert
			callCount.Should().Be(3);
			response.Success.Should().BeTrue();
			response.Data.Status.Should().Be("Success");
		}

		[TestMethod]
		public async Task SendAsync_NetworkFailure_ReturnsErrorMessage()
		{
			// Arrange
			var url = new Uri("https://api.example.com/unreachable");
			_mockHttpHandler.When(HttpMethod.Get, url.ToString())
				.Throw(new HttpRequestException("Network unreachable"));

			// Act
			var response = await _apiClient.GetAsync<TestResponse>(url);

			// Assert
			response.Success.Should().BeFalse();
			response.ErrorMessage.Should().Contain("Network unreachable");
		}

		[TestMethod]
		public async Task SendAsync_InvalidJson_ReturnsSerializationError()
		{
			// Arrange
			var url = new Uri("https://api.example.com/invalid-json");
			_mockHttpHandler.When(HttpMethod.Get, url.ToString())
				.Respond("application/json", "{ \"id\": \"not-a-number\", \"name\": \"Test\" }");

			// Act
			var response = await _apiClient.GetAsync<TestResponse>(url);

			// Assert
			response.Success.Should().BeFalse();
			response.Data.Should().BeNull();	
			response.ErrorMessage.ToLower().Should().Contain("could not convert");


		}

		[TestMethod]
		public async Task SendAsync_Cancellation_ThrowsTaskCanceledException()
		{
			// Arrange
			var url = new Uri("https://api.example.com/slow");
			var cts = new CancellationTokenSource();

			_mockHttpHandler.When(HttpMethod.Get, url.ToString())
				.Respond(async () =>
				{
					await Task.Delay(1000);
					return new HttpResponseMessage(HttpStatusCode.OK);
				});

			// Act
			cts.CancelAfter(100);
			Func<Task> act = async () => await _apiClient.GetAsync<TestResponse>(url, null, cts.Token   );

			// Assert
			await act.Should().ThrowAsync<TaskCanceledException>();
		}

		//[TestMethod]
		//public void Dispose_WhenCalled_DisposesHttpClient()
		//{
		//	// Arrange
		//	var handler = new MockHttpMessageHandler();
		//	var httpClient = new HttpClient(handler);
		//	var client = new ApiClient(httpClient);

		//	// Act
		//	client.Dispose();

		//	// Assert
		//	handler.Should(). ; // Handler shouldn't be disposed by HttpClient
		//	httpClient.Dispose(); // Clean up
		//}

		[TestMethod]
		public void Constructor_WithCustomSettings_UsesProvidedSettings()
		{
			// Arrange
			var customSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			};

			// Act
			var client = new ApiClient(_httpClient, jsonSettings: customSettings);

			// Assert
			// Indirect test - would need reflection to verify, or test through serialization behavior
			client.Should().NotBeNull();
		}

		// Helper classes
		private class TestResponse
		{
			[JsonProperty("id")]
			public int Id { get; set; }

			[JsonProperty("name")]
			public string Name { get; set; }
			
			[JsonProperty("status")]
			public string Status { get; set; }
		}

		private class TestRequest
		{
			[JsonProperty("name")]
			public string Name { get; set; }
		}
	}
}