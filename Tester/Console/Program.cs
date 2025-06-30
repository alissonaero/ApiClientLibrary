using ApiClientLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
 

namespace ApiClientConsoleTest
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("=== API Client Test ===");
			Console.WriteLine("1. Test GET");
			Console.WriteLine("2. Test POST");
			Console.WriteLine("3. Test PUT");
			Console.WriteLine("4. Test DELETE");
			Console.WriteLine("5. Test PATCH");
			Console.WriteLine("0. Exit");
			Console.Write("Choose an option: ");

			string option = Console.ReadLine();

			var bearerToken = "123456"; // Substitua por um token real, se necessário

			var baseUrl = new Uri("https://httpbin.org/");

			switch (option)
			{
				case "1":
					await TestGet(baseUrl);
					break;
				case "2":
					await TestPost(baseUrl);
					break;
				case "3":
					await TestPut(baseUrl);
					break;
				case "4":
					await TestDelete(baseUrl);
					break;
				case "5":
					await TestPatch(baseUrl);
					break;
				case "0":
					Console.WriteLine("Exiting...");
					return;
				default:
					Console.WriteLine("Invalid option!");
					break;
			}

			Console.WriteLine("Press any key to continue...");

			Console.ReadKey();

			Console.Clear();

			await Main(args); // Loop para voltar ao menu
		}

		static async Task TestGet(Uri baseUrl)
		{
			Console.WriteLine("Testing GET...");
			var url = new Uri(baseUrl, "get");
			var response = await ApiClient.GetAsync<HttpBinResponse>(url, bearerToken: null);

			if (response.Success)
			{
				Console.WriteLine("Sucess!");
				Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
			}
			else
			{
				Console.WriteLine($"Error: {response.ErrorMessage}");
			}
		}

		static async Task TestPost(Uri baseUrl)
		{
			Console.WriteLine("Testing POST...");

			var url = new Uri(baseUrl, "post");

			var postData = new
			{
				title = "New Test Post",
				body = "Test post content",
				userId = 1
			};

			var response = await ApiClient.PostAsync<object, HttpBinResponse>(url, postData, bearerToken: null);

			if (response.Success)
			{
				Console.WriteLine("Sucess!");
				Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
			}
			else
			{
				Console.WriteLine($"Error: {response.ErrorMessage}");
			}
		}

		static async Task TestPut(Uri baseUrl)
		{
			Console.WriteLine("Testing PUT...");
			var url = new Uri(baseUrl, "put");
			var putData = new
			{
				id = 1,
				title = "Updated Post",
				body = "Updated content",
				userId = 1
			};
			var response = await ApiClient.PutAsync<object, HttpBinResponse>(url, putData, bearerToken: null);

			if (response.Success)
			{
				Console.WriteLine("Sucess!");
				Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
			}
			else
			{
				Console.WriteLine($"Error: {response.ErrorMessage}");
			}
		}

		static async Task TestDelete(Uri baseUrl)
		{
			Console.WriteLine("Testing DELETE...");
			var url = new Uri(baseUrl, "delete");
			var response = await ApiClient.DeleteAsync<HttpBinResponse>(url, bearerToken: null);

			if (response.Success)
			{
				Console.WriteLine("Success! Post deleted (simulated).");
			}
			else
			{
				Console.WriteLine($"Error: {response.ErrorMessage}");
			}
		}

		static async Task TestPatch(Uri baseUrl)
		{
			Console.WriteLine("Testing PATCH...");
			var url = new Uri(baseUrl, "patch");
			var patchData = new
			{
				title = "Partially Updated Post",
			};

			var response = await ApiClient.PatchAsync<object, HttpBinResponse>(url, patchData, bearerToken: null);

			if (response.Success)
			{
				Console.WriteLine("Sucess!");
				Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
			}
			else
			{
				Console.WriteLine($"Error: {response.ErrorMessage}");
			}
		}
	}

	public class Post
	{
		public int UserId { get; set; }
		public int Id { get; set; }
		public string Title { get; set; }
		public string Body { get; set; }
	}

	public class HttpBinResponse
	{
		public Dictionary<string, string> Args { get; set; }
		public Dictionary<string, string> Headers { get; set; }
		public string Origin { get; set; }
		public string Url { get; set; }
	}

}