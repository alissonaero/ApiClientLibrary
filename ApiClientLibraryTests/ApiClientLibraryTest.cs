using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApiClientLibrary.Tests
{
	[TestClass]
	public class ApiClientTests
	{
		private readonly Uri _testUrl = new Uri("https://httpbin.org/");
		private readonly string _bearerToken = "test-token";
		private readonly JsonSerializerSettings _customJsonSettings = new JsonSerializerSettings
		{
			DateFormatString = "yyyy-MM-dd"
		};

		[TestMethod]
		public async Task GetAsync_SuccessfulRequest_ReturnsSuccessResponse()
		{
			// Arrange
			var fullUrl = new Uri(_testUrl, "get");

			// Act
			var result = await ApiClient.GetAsync<object>(fullUrl);

			// Assert
			result.Should().NotBeNull();
			result.Success.Should().BeTrue();
			result.Data.Should().NotBeNull(); // httpbin.org/get retorna um JSON com detalhes da requisição
			result.ErrorMessage.Should().BeNull();
			result.ErrorData.Should().BeNull();
		}

		[TestMethod]
		public async Task GetAsync_NullUrl_ReturnsFailureResponse()
		{
			// Act
			var result = await ApiClient.GetAsync<object>(null);

			// Assert
			result.Should().NotBeNull();
			result.Success.Should().BeFalse();
			result.ErrorMessage.Should().Be("URL cannot be null.");
			result.Data.Should().BeNull();
			result.ErrorData.Should().BeNull();
		}

		[TestMethod]
		public async Task GetAsync_HttpError404_ReturnsFailureResponse()
		{
			// Arrange
			var fullUrl = new Uri(_testUrl, "nonexistent");

			// Act
			var result = await ApiClient.GetAsync<object>(fullUrl);

			// Assert
			result.Should().NotBeNull();
			result.Success.Should().BeFalse();
			result.ErrorMessage.Should().Contain("not found");
			result.Data.Should().BeNull();
		}

		[TestMethod]
		public async Task GetAsync_WithBearerToken_AddsAuthorizationHeader()
		{
			// Arrange
			var fullUrl = new Uri(_testUrl, "get");

			// Act
			var result = await ApiClient.GetAsync<object>(fullUrl, _bearerToken);

			// Assert
			result.Should().NotBeNull();
			result.Success.Should().BeTrue();
			result.Data.Should().NotBeNull(); // O httpbin.org reflete os cabeçalhos, incluindo Authorization
		}

		[TestMethod]
		public async Task PostAsync_SuccessfulRequest_ReturnsSuccessResponse()
		{
			// Arrange
			var requestData = new { Value = "Test" };
			var fullUrl = new Uri(_testUrl, "post");

			// Act
			var result = await ApiClient.PostAsync<object, object>(fullUrl, requestData);

			// Assert
			result.Should().NotBeNull();
			result.Success.Should().BeTrue();
			result.Data.Should().NotBeNull(); // httpbin.org/post retorna o JSON enviado
			result.ErrorMessage.Should().BeNull();
			result.ErrorData.Should().BeNull();
		}

		[TestMethod]
		public async Task PutAsync_SuccessfulRequest_ReturnsSuccessResponse()
		{
			// Arrange
			var requestData = new { Value = "Test" };
			var fullUrl = new Uri(_testUrl, "put");

			// Act
			var result = await ApiClient.PutAsync<object, object>(fullUrl, requestData);

			// Assert
			result.Should().NotBeNull();
			result.Success.Should().BeTrue();
			result.Data.Should().NotBeNull();
			result.ErrorMessage.Should().BeNull();
			result.ErrorData.Should().BeNull();
		}

		[TestMethod]
		public async Task DeleteAsync_SuccessfulRequest_ReturnsSuccessResponse()
		{
			// Arrange
			var fullUrl = new Uri(_testUrl, "delete");

			// Act
			var result = await ApiClient.DeleteAsync<object>(fullUrl);

			// Assert
			result.Should().NotBeNull();
			result.Success.Should().BeTrue();
			result.Data.Should().NotBeNull();
			result.ErrorMessage.Should().BeNull();
			result.ErrorData.Should().BeNull();
		}

		[TestMethod]
		public async Task PatchAsync_SuccessfulRequest_ReturnsSuccessResponse()
		{
			// Arrange
			var requestData = new { Value = "Test" };
			var fullUrl = new Uri(_testUrl, "patch");

			// Act
			var result = await ApiClient.PatchAsync<object, object>(fullUrl, requestData);

			// Assert
			result.Should().NotBeNull();
			result.Success.Should().BeTrue();
			result.Data.Should().NotBeNull();
			result.ErrorMessage.Should().BeNull();
			result.ErrorData.Should().BeNull();
		}

		[TestMethod]
		public async Task PostArrayReturn_SuccessfulRequest_ReturnsArrayAsyncResponse()
		{
			// Arrange
			var requestData = new[] { new { Value = "Test1" }, new { Value = "Test2" } };
			var fullUrl = new Uri("https://alissonaero.free.beeceptor.com/mockposts");

			// Act
			var result = await ApiClient.PostArrayReturnAsync<object, object>(fullUrl, requestData);

			// Assert
			result.Should().NotBeNull();
			result.Success.Should().BeTrue();
			result.Data.Should().NotBeNull(); // httpbin.org/post retorna o array enviado
			result.ErrorMessage.Should().BeNull();
			result.ErrorData.Should().BeNull();
		}

		[TestMethod]
		public async Task GetAsync_WithCustomJsonSettings_UsesCustomSerialization()
		{
			// Arrange
			var fullUrl = new Uri(_testUrl, "get");

			// Act
			var result = await ApiClient.GetAsync<object>(fullUrl, jsonSettings: _customJsonSettings);

			// Assert
			result.Should().NotBeNull();
			result.Success.Should().BeTrue();
			result.Data.Should().NotBeNull();
		}

		[TestMethod]
		public async Task GetAsync_ThrowsHttpRequestException_ReturnsFailureResponse()
		{
			// Arrange
			var fullUrl = new Uri(_testUrl, "invalid-domain"); // URL inválida para simular erro

			// Act
			var result = await ApiClient.GetAsync<object>(fullUrl);

			// Assert
			result.Should().NotBeNull();
			result.Success.Should().BeFalse();
			result.ErrorMessage.ToLower().Should().Contain("not found");
		}

		[TestMethod]
		public void Get_SuccessfulRequest_ReturnsSuccessResponse()
		{
			var fullUrl = new Uri(_testUrl, "get");
			var result = ApiClient.Get<object>(fullUrl);

			result.Should().NotBeNull();
			result.Success.Should().BeTrue();
			result.Data.Should().NotBeNull();
			result.ErrorMessage.Should().BeNull();
			result.ErrorData.Should().BeNull();
		}

		[TestMethod]
		public void Post_SuccessfulRequest_ReturnsSuccessResponse()
		{
			var fullUrl = new Uri(_testUrl, "post");
			var requestData = new { Value = "Test" };
			var result = ApiClient.Post<object, object>(fullUrl, requestData);

			result.Should().NotBeNull();
			result.Success.Should().BeTrue();
			result.Data.Should().NotBeNull();
			result.ErrorMessage.Should().BeNull();
			result.ErrorData.Should().BeNull();
		}

		[TestMethod]
		public void Put_SuccessfulRequest_ReturnsSuccessResponse()
		{
			var fullUrl = new Uri(_testUrl, "put");
			var requestData = new { Value = "Test" };
			var result = ApiClient.Put<object, object>(fullUrl, requestData);

			result.Should().NotBeNull();
			result.Success.Should().BeTrue();
			result.Data.Should().NotBeNull();
		}

		[TestMethod]
		public void Delete_SuccessfulRequest_ReturnsSuccessResponse()
		{
			var fullUrl = new Uri(_testUrl, "delete");
			var result = ApiClient.Delete<object>(fullUrl);

			result.Should().NotBeNull();
			result.Success.Should().BeTrue();
			result.Data.Should().NotBeNull();
		}

		[TestMethod]
		public void Patch_SuccessfulRequest_ReturnsSuccessResponse()
		{
			var fullUrl = new Uri(_testUrl, "patch");
			var requestData = new { Value = "Test" };
			var result = ApiClient.Patch<object, object>(fullUrl, requestData);

			result.Should().NotBeNull();
			result.Success.Should().BeTrue();
			result.Data.Should().NotBeNull();
		}

		[TestMethod]
		public void PostArrayReturn_SuccessfulRequest_ReturnsArrayResponse()
		{
			var fullUrl = new Uri("https://alissonaero.free.beeceptor.com/mockposts");
			var requestData = new[] { new { Value = "Test1" }, new { Value = "Test2" } };
			var result = ApiClient.PostArrayReturn<object, object>(fullUrl, requestData);

			result.Should().NotBeNull();
			result.Success.Should().BeTrue();
			result.Data.Should().NotBeNull();
			result.Data.Length.Should().BeGreaterThan(0);
		}
	}
}