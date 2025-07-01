using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace ApiClientLibrary
{
	/// <summary>
	/// A static HTTP API client that provides simplified, resilient access to RESTful endpoints.
	/// Supports common HTTP methods (GET, POST, PUT, DELETE, PATCH) and automatically handles JSON
	/// serialization/deserialization using Newtonsoft.Json.
	/// 
	/// Includes built-in retry policy using Polly, with exponential backoff for transient errors
	/// such as rate limits (HTTP 429) and service unavailability (HTTP 503).
	/// 
	/// Requests support optional bearer token authentication and customizable JSON serialization settings.
	/// </summary>
	/// <remarks>
	/// Usage:
	/// <code>
	/// var response = await ApiClient.GetAsync<MyResponseType>(new Uri("https://api.example.com/resource"));
	/// if (response.Success)
	/// {
	///     var data = response.Data;
	/// }
	/// else
	/// {
	///     Console.WriteLine(response.ErrorMessage);
	/// }
	/// </code>
	/// </remarks>

	public static class ApiClient
	{
		private static readonly HttpClient _httpClient;
		private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

		static ApiClient()
		{
			_httpClient = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(30)
			};
			_httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

			// Política de retentativa com Polly: 3 tentativas com backoff exponencial
			 _retryPolicy = Policy
								.Handle<HttpRequestException>()
								.OrResult<HttpResponseMessage>(r =>
								!r.IsSuccessStatusCode && ((int)r.StatusCode == 429 || (int)r.StatusCode == 503))
								.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

		}

		/// <summary>
		/// Sends an asynchronous HTTP GET request to the specified URI and deserializes the JSON response to the specified type.
		/// </summary>
		/// <typeparam name="T">The type to deserialize the response into.</typeparam>
		/// <param name="url">The target URI.</param>
		/// <param name="bearerToken">Optional bearer token for authentication.</param>
		/// <param name="jsonSettings">Optional JSON serialization settings.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>An <see cref="ApiResponse{T}"/> containing the result.</returns>
		public static async Task<ApiResponse<T>> GetAsync<T>(
			Uri url,
			string bearerToken = null,
			JsonSerializerSettings jsonSettings = null,
			CancellationToken cancellationToken = default)
		{
			return await ExecuteRequestAsync<T>(
				HttpMethod.Get,
				url,
				null,
				bearerToken,
				jsonSettings,
				cancellationToken);
		}

		/// <summary>
		/// Sends an asynchronous HTTP POST request with a JSON body and deserializes the JSON response to the specified type.
		/// </summary>
		/// <typeparam name="TRequest">The type of the request payload.</typeparam>
		/// <typeparam name="TResponse">The type to deserialize the response into.</typeparam>
		/// <param name="url">The target URI.</param>
		/// <param name="request">The request payload.</param>
		/// <param name="bearerToken">Optional bearer token for authentication.</param>
		/// <param name="jsonSettings">Optional JSON serialization settings.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>An <see cref="ApiResponse{TResponse}"/> containing the result.</returns>

		public static async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
			Uri url,
			TRequest request,
			string bearerToken = null,
			JsonSerializerSettings jsonSettings = null,
			CancellationToken cancellationToken = default)
		{
			return await ExecuteRequestAsync<TResponse>(
				HttpMethod.Post,
				url,
				request,
				bearerToken,
				jsonSettings,
				cancellationToken);
		}

		/// <summary>
		/// Sends an asynchronous HTTP PUT request with a JSON body and deserializes the JSON response to the specified type.
		/// </summary>
		/// <typeparam name="TRequest">The type of the request payload.</typeparam>
		/// <typeparam name="TResponse">The type to deserialize the response into.</typeparam>
		/// <param name="url">The target URI.</param>
		/// <param name="request">The request payload.</param>
		/// <param name="bearerToken">Optional bearer token for authentication.</param>
		/// <param name="jsonSettings">Optional JSON serialization settings.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>An <see cref="ApiResponse{TResponse}"/> containing the result.</returns>
		public static async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(
			Uri url,
			TRequest request,
			string bearerToken = null,
			JsonSerializerSettings jsonSettings = null,
			CancellationToken cancellationToken = default)
		{
			return await ExecuteRequestAsync<TResponse>(
				HttpMethod.Put,
				url,
				request,
				bearerToken,
				jsonSettings,
				cancellationToken);
		}

		/// <summary>
		/// Sends an asynchronous HTTP DELETE request and deserializes the JSON response to the specified type.
		/// </summary>
		/// <typeparam name="TResponse">The type to deserialize the response into.</typeparam>
		/// <param name="url">The target URI.</param>
		/// <param name="bearerToken">Optional bearer token for authentication.</param>
		/// <param name="jsonSettings">Optional JSON serialization settings.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>An <see cref="ApiResponse{TResponse}"/> containing the result.</returns>
		public static async Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(
			Uri url,
			string bearerToken = null,
			JsonSerializerSettings jsonSettings = null,
			CancellationToken cancellationToken = default)
		{
			return await ExecuteRequestAsync<TResponse>(
				HttpMethod.Delete,
				url,
				null,
				bearerToken,
				jsonSettings,
				cancellationToken);
		}

		/// <summary>
		/// Sends an asynchronous HTTP PATCH request with a JSON body and deserializes the JSON response to the specified type.
		/// </summary>
		/// <typeparam name="TRequest">The type of the request payload.</typeparam>
		/// <typeparam name="TResponse">The type to deserialize the response into.</typeparam>
		/// <param name="url">The target URI.</param>
		/// <param name="request">The request payload.</param>
		/// <param name="bearerToken">Optional bearer token for authentication.</param>
		/// <param name="jsonSettings">Optional JSON serialization settings.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>An <see cref="ApiResponse{TResponse}"/> containing the result.</returns>
		public static async Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(
			Uri url,
			TRequest request,
			string bearerToken = null,
			JsonSerializerSettings jsonSettings = null,
			CancellationToken cancellationToken = default)
		{
			return await ExecuteRequestAsync<TResponse>(
				new HttpMethod("PATCH"),
				url,
				request,
				bearerToken,
				jsonSettings,
				cancellationToken);
		}

		private static async Task<ApiResponse<T>> ExecuteRequestAsync<T>(
			HttpMethod method,
			Uri url,
			object requestBody,
			string bearerToken,
			JsonSerializerSettings jsonSettings,
			CancellationToken cancellationToken)
		{
			if (url == null)
				return new ApiResponse<T> { Success = false, ErrorMessage = "URL cannot be null." };

			var response = new ApiResponse<T>();

			try
			{
				using (var request = new HttpRequestMessage(method, url))
				{
					// Adicionar Bearer Token, se fornecido
					if (!string.IsNullOrEmpty(bearerToken))
					{
						request.Headers.Add("Authorization", $"Bearer {bearerToken}");
					}

					// Serializar corpo da requisição, se aplicável
					if (requestBody != null && (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH"))
					{
						var json = JsonConvert.SerializeObject(requestBody, jsonSettings ?? DefaultJsonSettings());
						request.Content = new StringContent(json, Encoding.UTF8, "application/json");
					}

					// Executar com política de retentativa
					var httpResponse = await _retryPolicy.ExecuteAsync(async () =>
					{
						return await _httpClient.SendAsync(request, cancellationToken);
					});

					var responseJson = await httpResponse.Content.ReadAsStringAsync();

					if (httpResponse.IsSuccessStatusCode)
					{
						response.Success = true;
						response.Data = JsonConvert.DeserializeObject<T>(responseJson, jsonSettings ?? DefaultJsonSettings());
					}
					else
					{
						response.Success = false;
						response.ErrorMessage = $"HTTP Error {httpResponse.StatusCode}: {responseJson}";
						response.ErrorData = responseJson;
					}
				}
			}
			catch (HttpRequestException ex)
			{
				response.Success = false;
				response.ErrorMessage = $"Request failed: {ex.Message}";
			}
			catch (JsonException ex)
			{
				response.Success = false;
				response.ErrorMessage = $"JSON deserialization failed: {ex.Message}";
			}
			catch (Exception ex)
			{
				response.Success = false;
				response.ErrorMessage = $"Unexpected error: {ex.Message}";
			}

			return response;
		}

		private static JsonSerializerSettings DefaultJsonSettings()
		{
			return new JsonSerializerSettings
			{
				ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
				{
					NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()
				},
				DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ"
			};
		}
	}

	/// <summary>
	/// Represents a standard response wrapper for HTTP API requests.
	/// </summary>
	/// <typeparam name="T">The type of the response data.</typeparam>
	public class ApiResponse<T>
	{
		/// <summary>
		/// Indicates whether the request was successful (2xx status).
		/// </summary>
		public bool Success { get; set; }

		/// <summary>
		/// The deserialized data returned by the API.
		/// </summary>
		public T Data { get; set; }

		/// <summary>
		/// A human-readable error message, if the request failed.
		/// </summary>
		public string ErrorMessage { get; set; }

		/// <summary>
		/// The raw error content returned by the API (if any).
		/// </summary>
		public string ErrorData { get; set; }
	}

}