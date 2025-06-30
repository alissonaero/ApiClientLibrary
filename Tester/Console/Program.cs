using ApiClientLibrary;
using Newtonsoft.Json;
using System; 
using System.Threading.Tasks;
 

namespace ApiClientConsoleTest
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("=== Teste de API Client ===");
			Console.WriteLine("1. Testar GET");
			Console.WriteLine("2. Testar POST");
			Console.WriteLine("3. Testar PUT");
			Console.WriteLine("4. Testar DELETE");
			Console.WriteLine("5. Testar PATCH");
			Console.WriteLine("0. Sair");
			Console.Write("Escolha uma opção: ");

			string option = Console.ReadLine();

			var bearerToken = "seu-token-aqui"; // Substitua por um token real, se necessário
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
					Console.WriteLine("Saindo...");
					return;
				default:
					Console.WriteLine("Opção inválida!");
					break;
			}

			Console.WriteLine("Pressione qualquer tecla para continuar...");
			Console.ReadKey();
			Console.Clear();
			await Main(args); // Loop para voltar ao menu
		}

		static async Task TestGet(Uri baseUrl)
		{
			Console.WriteLine("Testando GET...");
			var url = new Uri(baseUrl, "get");
			var response = await ApiClient.GetAsync<Post>(url, bearerToken: null);

			if (response.Success)
			{
				Console.WriteLine("Sucesso!");
				Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
			}
			else
			{
				Console.WriteLine($"Erro: {response.ErrorMessage}");
			}
		}

		static async Task TestPost(Uri baseUrl)
		{
			Console.WriteLine("Testando POST...");
			var url = new Uri(baseUrl, "post");
			var postData = new
			{
				title = "Novo Post de Teste",
				body = "Conteúdo do post de teste",
				userId = 1
			};
			var response = await ApiClient.PostAsync<object, Post>(url, postData, bearerToken: null);

			if (response.Success)
			{
				Console.WriteLine("Sucesso!");
				Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
			}
			else
			{
				Console.WriteLine($"Erro: {response.ErrorMessage}");
			}
		}

		static async Task TestPut(Uri baseUrl)
		{
			Console.WriteLine("Testando PUT...");
			var url = new Uri(baseUrl, "posts/1");
			var putData = new
			{
				id = 1,
				title = "Post Atualizado",
				body = "Conteúdo atualizado",
				userId = 1
			};
			var response = await ApiClient.PutAsync<object, Post>(url, putData, bearerToken: null);

			if (response.Success)
			{
				Console.WriteLine("Sucesso!");
				Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
			}
			else
			{
				Console.WriteLine($"Erro: {response.ErrorMessage}");
			}
		}

		static async Task TestDelete(Uri baseUrl)
		{
			Console.WriteLine("Testando DELETE...");
			var url = new Uri(baseUrl, "posts/1");
			var response = await ApiClient.DeleteAsync<Post>(url, bearerToken: null);

			if (response.Success)
			{
				Console.WriteLine("Sucesso! Post deletado (simulado).");
			}
			else
			{
				Console.WriteLine($"Erro: {response.ErrorMessage}");
			}
		}

		static async Task TestPatch(Uri baseUrl)
		{
			Console.WriteLine("Testando PATCH...");
			var url = new Uri(baseUrl, "posts/1");
			var patchData = new
			{
				title = "Post Parcialmente Atualizado"
			};

			var response = await ApiClient.PatchAsync<object, Post>(url, patchData, bearerToken: null);

			if (response.Success)
			{
				Console.WriteLine("Sucesso!");
				Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
			}
			else
			{
				Console.WriteLine($"Erro: {response.ErrorMessage}");
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
}