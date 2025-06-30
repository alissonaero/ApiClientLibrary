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

	public class ApiResponse<T>
	{
		public bool Success { get; set; }
		public T Data { get; set; }
		public string ErrorMessage { get; set; }
		public string ErrorData { get; set; }
	}
}