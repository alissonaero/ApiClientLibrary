using ApiClientLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ApiClientConsoleTest
{
	class Program
	{
		static async Task Main(string[] args)
		{
			// Initialize instance-based client
			IApiClient client = new ApiClient(new HttpClient { BaseAddress = new Uri("https://httpbin.org/") });

			Console.WriteLine("=== API Client Test ===");
			Console.WriteLine("1. Test GET");
			Console.WriteLine("2. Test POST");
			Console.WriteLine("3. Test PUT");
			Console.WriteLine("4. Test DELETE");
			Console.WriteLine("5. Test PATCH");
			Console.WriteLine("6. Test POST (Array Response)");
			Console.WriteLine("0. Exit");
			Console.Write("Choose an option: ");

			string option = Console.ReadLine();

			switch (option)
			{
				case "1": await TestGetAsync(client); break;
				case "2": await TestPostAsync(client); break;
				case "3": await TestPutAsync(client); break;
				case "4": await TestDeleteAsync(client); break;
				case "5": await TestPatchAsync(client); break;
				case "6": await TestPostArrayAsync(client); break;
				case "0": Console.WriteLine("Exiting..."); return;
				default: Console.WriteLine("Invalid option!"); break;
			}

			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
			Console.Clear();
			await Main(args);
		}

		static async Task TestGetAsync(IApiClient client)
		{
			Console.WriteLine("Testing GET...");
			var response = await client.GetAsync<HttpBinResponse<object>>(new Uri("anything", UriKind.Relative));

			if (response.Success)
			{
				Console.WriteLine("Success!");
				Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
			}
			else
			{
				Console.WriteLine($"Error: {response.ErrorMessage}");
				Console.WriteLine($"Raw Error Data: {response.ErrorData}");
			}
		}

		static async Task TestPostAsync(IApiClient client)
		{
			Console.WriteLine("Testing POST...");
			var postData = new { title = "New Test Post", body = "Test post content", userId = 1 };
			var response = await client.PostAsync<object, HttpBinResponse<object>>(new Uri("post", UriKind.Relative), postData);

			if (response.Success)
			{
				Console.WriteLine("Success!");
				Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
			}
			else
			{
				Console.WriteLine($"Error: {response.ErrorMessage}");
				Console.WriteLine($"Raw Error Data: {response.ErrorData}");
			}
		}

		static async Task TestPutAsync(IApiClient client)
		{
			Console.WriteLine("Testing PUT...");
			var putData = new { id = 1, title = "Updated Post", body = "Updated content", userId = 1 };
			var response = await client.PutAsync<object, HttpBinResponse<object>>(new Uri("put", UriKind.Relative), putData);

			if (response.Success)
			{
				Console.WriteLine("Success!");
				Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
			}
			else
			{
				Console.WriteLine($"Error: {response.ErrorMessage}");
				Console.WriteLine($"Raw Error Data: {response.ErrorData}");
			}
		}

		static async Task TestDeleteAsync(IApiClient client)
		{
			Console.WriteLine("Testing DELETE...");
			var response = await client.DeleteAsync<HttpBinResponse<object>>(new Uri("delete", UriKind.Relative));

			if (response.Success)
			{
				Console.WriteLine("Success! Delete simulated.");
				Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
			}
			else
			{
				Console.WriteLine($"Error: {response.ErrorMessage}");
				Console.WriteLine($"Raw Error Data: {response.ErrorData}");
			}
		}

		static async Task TestPatchAsync(IApiClient client)
		{
			Console.WriteLine("Testing PATCH...");
			var patchData = new { title = "Partially Updated Post" };
			var response = await client.PatchAsync<object, HttpBinResponse<object>>(new Uri("patch", UriKind.Relative), patchData);

			if (response.Success)
			{
				Console.WriteLine("Success!");
				Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
			}
			else
			{
				Console.WriteLine($"Error: {response.ErrorMessage}");
				Console.WriteLine($"Raw Error Data: {response.ErrorData}");
			}
		}

		static async Task TestPostArrayAsync(IApiClient client)
		{
			Console.WriteLine("Testing POST (array response)...");
			var posts = new[]
			{
				new Post { Id = 1, Title = "From client", Body = "Testing", UserId = 100 },
				new Post { Id = 2, Title = "From client", Body = "Again", UserId = 101 }
			};
			var response = await client.PostAsync<Post[], Post[]>(new Uri("https://alissonaero.free.beeceptor.com/mockposts"), posts);

			if (response.Success && response.Data != null)
			{
				Console.WriteLine("✅ Success! Response contains:");
				foreach (var item in response.Data)
					Console.WriteLine(JsonConvert.SerializeObject(item, Formatting.Indented));
			}
			else
			{
				Console.WriteLine($"❌ Error: {response.ErrorMessage}");
				Console.WriteLine($"Raw Error Data: {response.ErrorData}");
			}
		}
	}

	public class Post
	{
		[JsonProperty("userId")] public int UserId { get; set; }
		[JsonProperty("id")] public int Id { get; set; }
		[JsonProperty("title")] public string Title { get; set; }
		[JsonProperty("body")] public string Body { get; set; }
	}

	public class HttpBinResponse<T>
	{
		[JsonProperty("args")] public Dictionary<string, string> Args { get; set; }
		[JsonProperty("data")] public string Data { get; set; }
		[JsonProperty("json")] public T Json { get; set; }
		[JsonProperty("url")] public string Url { get; set; }
	}
}
