using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
	/// var response = ApiClient.Get<MyResponseType>(new Uri("https://api.example.com/resource"));
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
		private static readonly RetryPolicy<HttpResponseMessage> _syncRetryPolicy;
		private static readonly string _defaultUserAgent = string.Empty;

		public static string DefaultUserAgent
		{
			get => _defaultUserAgent;
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(value);
				}
			}
		}

		static ApiClient()
		{
			_httpClient = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(30)
			};

			_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_defaultUserAgent);
			_httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

			// Async retry policy with Polly: 3 attempts with exponential backoff
			_retryPolicy = Policy
				.Handle<HttpRequestException>()
				.OrResult<HttpResponseMessage>(r =>
					!r.IsSuccessStatusCode && ((int)r.StatusCode == 429 || (int)r.StatusCode == 503))
				.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

			// Sync retry policy with Polly: 3 attempts with exponential backoff
			_syncRetryPolicy = Policy
				.Handle<HttpRequestException>()
				.OrResult<HttpResponseMessage>(r =>
					!r.IsSuccessStatusCode && ((int)r.StatusCode == 429 || (int)r.StatusCode == 503))
				.WaitAndRetry(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
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
				bearerToken,
				jsonSettings,
				cancellationToken);
		}

		/// <summary>
		/// Sends a synchronous HTTP GET request to the specified URI and deserializes the JSON response to the specified type.
		/// </summary>
		/// <typeparam name="T">The type to deserialize the response into.</typeparam>
		/// <param name="url">The target URI.</param>
		/// <param name="bearerToken">Optional bearer token for authentication.</param>
		/// <param name="jsonSettings">Optional JSON serialization settings.</param>
		/// <returns>An <see cref="ApiResponse{T}"/> containing the result.</returns>
		public static ApiResponse<T> Get<T>(
			Uri url,
			string bearerToken = null,
			JsonSerializerSettings jsonSettings = null)
		{
			return ExecuteRequest<T>(
				HttpMethod.Get,
				url,
				bearerToken,
				jsonSettings);
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
				bearerToken,
				jsonSettings,
				cancellationToken);
		}

		/// <summary>
		/// Sends a synchronous HTTP POST request with a JSON body and deserializes the JSON response to the specified type.
		/// </summary>
		/// <typeparam name="TRequest">The type of the request payload.</typeparam>
		/// <typeparam name="TResponse">The type to deserialize the response into.</typeparam>
		/// <param name="url">The target URI.</param>
		/// <param name="request">The request payload.</param>
		/// <param name="bearerToken">Optional bearer token for authentication.</param>
		/// <param name="jsonSettings">Optional JSON serialization settings.</param>
		/// <returns>An <see cref="ApiResponse{TResponse}"/> containing the result.</returns>
		public static ApiResponse<TResponse> Post<TRequest, TResponse>(
			Uri url,
			TRequest request,
			string bearerToken = null,
			JsonSerializerSettings jsonSettings = null)
		{
			return ExecuteRequest<TRequest, TResponse>(
				HttpMethod.Post,
				url,
				request,
				bearerToken,
				jsonSettings);
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
				bearerToken,
				jsonSettings,
				cancellationToken);
		}

		/// <summary>
		/// Sends a synchronous HTTP PUT request with a JSON body and deserializes the JSON response to the specified type.
		/// </summary>
		/// <typeparam name="TRequest">The type of the request payload.</typeparam>
		/// <typeparam name="TResponse">The type to deserialize the response into.</typeparam>
		/// <param name="url">The target URI.</param>
		/// <param name="request">The request payload.</param>
		/// <param name="bearerToken">Optional bearer token for authentication.</param>
		/// <param name="jsonSettings">Optional JSON serialization settings.</param>
		/// <returns>An <see cref="ApiResponse{TResponse}"/> containing the result.</returns>
		public static ApiResponse<TResponse> Put<TRequest, TResponse>(
			Uri url,
			TRequest request,
			string bearerToken = null,
			JsonSerializerSettings jsonSettings = null)
		{
			return ExecuteRequest<TRequest, TResponse>(
				HttpMethod.Put,
				url,
				request,
				bearerToken,
				jsonSettings);
		}

		/// <summary>
		/// Sends an asynchronous HTTP POST request with a JSON payload and expects a JSON array in response,
		/// which is deserialized to an array of the specified type.
		/// </summary>
		/// <typeparam name="TRequest">The type of the request payload.</typeparam>
		/// <typeparam name="TResponse">The type of each item in the response array.</typeparam>
		/// <param name="url">The target URI.</param>
		/// <param name="request">The request payload to send in the body.</param>
		/// <param name="bearerToken">Optional bearer token for authentication.</param>
		/// <param name="jsonSettings">Optional JSON serialization settings.</param>
		/// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
		/// <returns>An <see cref="ApiResponse{TResponse[]}"/> containing the deserialized response array.</returns>
		public static async Task<ApiResponse<TResponse[]>> PostArrayReturn<TRequest, TResponse>(
			Uri url,
			TRequest[] request,
			string bearerToken = null,
			JsonSerializerSettings jsonSettings = null,
			CancellationToken cancellationToken = default) where TResponse : new()
		{
			var requestResult = await PostAsync<TRequest[], TResponse[]>(url, request, bearerToken, jsonSettings, cancellationToken);
			return requestResult;
		}

		/// <summary>
		/// Sends a synchronous HTTP POST request with a JSON payload and expects a JSON array in response,
		/// which is deserialized to an array of the specified type.
		/// </summary>
		/// <typeparam name="TRequest">The type of the request payload.</typeparam>
		/// <typeparam name="TResponse">The type of each item in the response array.</typeparam>
		/// <param name="url">The target URI.</param>
		/// <param name="request">The request payload to send in the body.</param>
		/// <param name="bearerToken">Optional bearer token for authentication.</param>
		/// <param name="jsonSettings">Optional JSON serialization settings.</param>
		/// <returns>An <see cref="ApiResponse{TResponse[]}"/> containing the deserialized response array.</returns>
		public static ApiResponse<TResponse[]> PostArrayReturn<TRequest, TResponse>(
			Uri url,
			TRequest[] request,
			string bearerToken = null,
			JsonSerializerSettings jsonSettings = null) where TResponse : new()
		{
			var requestResult = Post<TRequest[], TResponse[]>(url, request, bearerToken, jsonSettings);
			return requestResult;
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
				bearerToken,
				jsonSettings,
				cancellationToken);
		}

		/// <summary>
		/// Sends a synchronous HTTP DELETE request and deserializes the JSON response to the specified type.
		/// </summary>
		/// <typeparam name="TResponse">The type to deserialize the response into.</typeparam>
		/// <param name="url">The target URI.</param>
		/// <param name="bearerToken">Optional bearer token for authentication.</param>
		/// <param name="jsonSettings">Optional JSON serialization settings.</param>
		/// <returns>An <see cref="ApiResponse{TResponse}"/> containing the result.</returns>
		public static ApiResponse<TResponse> Delete<TResponse>(
			Uri url,
			string bearerToken = null,
			JsonSerializerSettings jsonSettings = null)
		{
			return ExecuteRequest<TResponse>(
				HttpMethod.Delete,
				url,
				bearerToken,
				jsonSettings);
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
			return await ExecuteRequestAsync<TRequest, TResponse>(
				new HttpMethod("PATCH"),
				url,
				request,
				bearerToken,
				jsonSettings,
				cancellationToken);
		}

		/// <summary>
		/// Sends a synchronous HTTP PATCH request with a JSON body and deserializes the JSON response to the specified type.
		/// </summary>
		/// <typeparam name="TRequest">The type of the request payload.</typeparam>
		/// <typeparam name="TResponse">The type to deserialize the response into.</typeparam>
		/// <param name="url">The target URI.</param>
		/// <param name="request">The request payload.</param>
		/// <param name="bearerToken">Optional bearer token for authentication.</param>
		/// <param name="jsonSettings">Optional JSON serialization settings.</param>
		/// <returns>An <see cref="ApiResponse{TResponse}"/> containing the result.</returns>
		public static ApiResponse<TResponse> Patch<TRequest, TResponse>(
			Uri url,
			TRequest request,
			string bearerToken = null,
			JsonSerializerSettings jsonSettings = null)
		{
			return ExecuteRequest<TRequest, TResponse>(
				new HttpMethod("PATCH"),
				url,
				request,
				bearerToken,
				jsonSettings);
		}

		private static async Task<ApiResponse<TResponse>> ExecuteRequestAsync<TResponse>(
			HttpMethod method,
			Uri url,
			string bearerToken,
			JsonSerializerSettings jsonSettings,
			CancellationToken cancellationToken)
		{
			return await ExecuteRequestAsync<object, TResponse>(
				method,
				url,
				requestBody: null,
				bearerToken,
				jsonSettings,
				cancellationToken);
		}

		private static ApiResponse<TResponse> ExecuteRequest<TResponse>(
			HttpMethod method,
			Uri url,
			string bearerToken,
			JsonSerializerSettings jsonSettings)
		{
			return ExecuteRequest<object, TResponse>(
				method,
				url,
				requestBody: null,
				bearerToken,
				jsonSettings);
		}

		private static async Task<ApiResponse<TResponse>> ExecuteRequestAsync<TRequest, TResponse>(
			HttpMethod method,
			Uri url,
			TRequest requestBody,
			string bearerToken,
			JsonSerializerSettings jsonSettings,
			CancellationToken cancellationToken)
		{
			if (url == null)
				return new ApiResponse<TResponse> { Success = false, ErrorMessage = "URL cannot be null." };

			var response = new ApiResponse<TResponse>();

			try
			{
				using (var request = new HttpRequestMessage(method, url))
				{
					// Add Bearer Token if provided
					if (!string.IsNullOrEmpty(bearerToken))
					{
						request.Headers.Add("Authorization", $"Bearer {bearerToken}");
					}

					// Serialize request body if applicable
					if (requestBody != null && (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH"))
					{
						var json = JsonConvert.SerializeObject(requestBody, jsonSettings ?? DefaultJsonSettings());
						request.Content = new StringContent(json, Encoding.UTF8, "application/json");
					}

					// Execute with retry policy
					var httpResponse = await _retryPolicy.ExecuteAsync(async () =>
					{
						return await _httpClient.SendAsync(request, cancellationToken);
					});

					var responseJson = await httpResponse.Content.ReadAsStringAsync();

					if (httpResponse.IsSuccessStatusCode)
					{
						response.Success = true;
						response.Data = JsonConvert.DeserializeObject<TResponse>(responseJson, jsonSettings ?? DefaultJsonSettings());
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

		private static ApiResponse<TResponse> ExecuteRequest<TRequest, TResponse>(
			HttpMethod method,
			Uri url,
			TRequest requestBody,
			string bearerToken,
			JsonSerializerSettings jsonSettings)
		{
			if (url == null)
				return new ApiResponse<TResponse> { Success = false, ErrorMessage = "URL cannot be null." };

			var response = new ApiResponse<TResponse>();

			try
			{
				using (var request = new HttpRequestMessage(method, url))
				{
					// Add Bearer Token if provided
					if (!string.IsNullOrEmpty(bearerToken))
					{
						request.Headers.Add("Authorization", $"Bearer {bearerToken}");
					}

					// Serialize request body if applicable
					if (requestBody != null && (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH"))
					{
						var json = JsonConvert.SerializeObject(requestBody, jsonSettings ?? DefaultJsonSettings());
						request.Content = new StringContent(json, Encoding.UTF8, "application/json");
					}

					// Execute with retry policy
					var httpResponse = _syncRetryPolicy.Execute(() =>
					{
						return _httpClient.SendAsync(request).GetAwaiter().GetResult();
					});

					var responseJson = httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();

					if (httpResponse.IsSuccessStatusCode)
					{
						response.Success = true;
						response.Data = JsonConvert.DeserializeObject<TResponse>(responseJson, jsonSettings ?? DefaultJsonSettings());
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