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
	public interface IApiClient
	{
		Task<ApiResponse<TResponse>> GetAsync<TResponse>(Uri url, CancellationToken cancellationToken = default);
		Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(Uri url, TRequest payload, CancellationToken cancellationToken = default);
		Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(Uri url, TRequest payload, CancellationToken cancellationToken = default);
		Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(Uri url, CancellationToken cancellationToken = default);
		Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(Uri url, TRequest payload, CancellationToken cancellationToken = default);
	}

	public class ApiClient : IApiClient, IDisposable
	{
		private readonly HttpClient _httpClient;
		private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
		private readonly JsonSerializerSettings _jsonSettings;

		public ApiClient(HttpClient httpClient,
						 JsonSerializerSettings jsonSettings = null,
						 AsyncRetryPolicy<HttpResponseMessage> retryPolicy = null)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_jsonSettings = jsonSettings ?? DefaultJsonSettings();
			_retryPolicy = retryPolicy ?? Policy.Handle<HttpRequestException>()
										 .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && ((int)r.StatusCode == 429 || (int)r.StatusCode == 503))
										 .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
		}

		public async Task<ApiResponse<TResponse>> GetAsync<TResponse>(Uri url, CancellationToken cancellationToken = default)
		{
			return await SendAsync<object, TResponse>(HttpMethod.Get, url, null, cancellationToken);
		}

		public async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(Uri url, TRequest payload, CancellationToken cancellationToken = default)
		{
			return await SendAsync<TRequest, TResponse>(HttpMethod.Post, url, payload, cancellationToken);
		}

		public async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(Uri url, TRequest payload, CancellationToken cancellationToken = default)
		{
			return await SendAsync<TRequest, TResponse>(HttpMethod.Put, url, payload, cancellationToken);
		}

		public async Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(Uri url, CancellationToken cancellationToken = default)
		{
			return await SendAsync<object, TResponse>(HttpMethod.Delete, url, null, cancellationToken);
		}

		public async Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(Uri url, TRequest payload, CancellationToken cancellationToken = default)
		{
			return await SendAsync<TRequest, TResponse>(new HttpMethod("PATCH"), url, payload, cancellationToken);
		}

		private async Task<ApiResponse<TResponse>> SendAsync<TRequest, TResponse>(HttpMethod method, Uri url, TRequest payload, CancellationToken cancellationToken)
		{
			var result = new ApiResponse<TResponse>();
			try
			{
				var httpResponse = await _retryPolicy.ExecuteAsync(async () =>
				{
					using (var request = new HttpRequestMessage(method, url))
					{
						request.Headers.Add("Accept", "application/json");

						if (payload != null && (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH"))
						{
							var json = JsonConvert.SerializeObject(payload, _jsonSettings);
							request.Content = new StringContent(json, Encoding.UTF8, "application/json");
						}

						return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
					}
				});


				string responseJson = string.Empty;

				if (httpResponse.Content != null)
					responseJson = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

				if (httpResponse.IsSuccessStatusCode)
				{
					result.Success = true;
					result.Data = string.IsNullOrWhiteSpace(responseJson)
									? default
									: JsonConvert.DeserializeObject<TResponse>(responseJson, _jsonSettings);
				}
				else
				{
					result.Success = false;
					result.ErrorMessage = $"HTTP {(int)httpResponse.StatusCode}: {responseJson}";
					result.ErrorData = responseJson;
				}
			}
			catch (TaskCanceledException ex)
			{
				throw ex;
			}
			catch (HttpRequestException ex)
			{
				result.Success = false;
				result.ErrorMessage = $"HttpRequestException Error: {ex.Message}";
				result.ErrorData = ex.ToString();
			}
			catch (JsonSerializationException ex)
			{
				result.Success = false;
				result.ErrorMessage = $"JSON Serialization Error: {ex.Message}";
				result.ErrorData = ex.ToString();
			}
			catch (JsonReaderException ex)
			{
				result.Success = false;
				result.ErrorMessage = $"JSON Reader Error: {ex.Message}";
				result.ErrorData = ex.ToString();
			}
	

			return result;
		}

		private static JsonSerializerSettings DefaultJsonSettings()
		{
			return new JsonSerializerSettings
			{
				ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
				{
					NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy(),
					
				},
				DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ",
				MissingMemberHandling = MissingMemberHandling.Error,
			};
		}

		public void Dispose()
		{
			_httpClient?.Dispose();
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
