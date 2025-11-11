using CloudKitchenChallenge.KitchenSystem.Actions;
using CloudKitchenChallenge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CloudKitchenChallenge.Utils
{
	public class ServerUtils
	{
		const string token = "u8ygz6dkx51x";
		const string testIdHeaderName = "x-test-id";
		const string serverAddress = "https://api.cloudkitchens.com/interview/challenge/";
		static HttpClient? httpClient;
		static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
			AllowTrailingCommas = true,
			Converters =
					{
						new JsonStringEnumConverter()
					}
		};

		public static async Task<OrderDetails[]?> ReadFromFile(string filename)
		{
			string text = await File.ReadAllTextAsync(filename);
			OrderDetails[]? orders = JsonSerializer.Deserialize<OrderDetails[]>(text, serializerOptions);
			return orders;
		}

		public static async Task<(string, OrderDetails[]?)> FetchProblem(int? seed = null) {
			if(httpClient == null) {
				httpClient = new HttpClient()
				{
					BaseAddress = new Uri(serverAddress),
				};
			}

			string urlSuffix = (seed != null) ? $"new?auth={token}&seed={seed}" : $"new?auth={token}";

			HttpResponseMessage? response = await httpClient.GetAsync(urlSuffix);
			if (response == null || response.StatusCode != System.Net.HttpStatusCode.OK)
			{
				throw new Exception("Unable to fetch problem from server");
			}
			
			string testId = response.Headers.GetValues(testIdHeaderName).FirstOrDefault();
			Console.WriteLine($"Fetched test problem with id: {testId}");

			OrderDetails[]? details = await response.Content.ReadFromJsonAsync<OrderDetails[]>(serializerOptions);

			return (testId, details);
		}


		public record class AppOptions(
			long rate,
			long min,
			long max
		);

		private record class JsonAction
		(
			string action,
			long timestamp,
			string id
		);
		private record class SolveRequestModel(
			AppOptions options,
			JsonAction[] actions
		);

		public static async Task TestOnChallengeServer(string actionLogName, int testId, AppOptions options)
		{
			string text = await File.ReadAllTextAsync(actionLogName);
			JsonAction[]? actions = JsonSerializer.Deserialize<JsonAction[]>(text, serializerOptions);
			Console.WriteLine("Deserialized action count: " + actions.Length);

			SolveRequestModel requestJson = new SolveRequestModel(options, actions);

			File.WriteAllText("testRequest.json", JsonSerializer.Serialize(requestJson, serializerOptions));

			if (httpClient == null)
			{
				httpClient = new HttpClient()
				{
					BaseAddress = new Uri(serverAddress)
				};
			}

			string urlSuffix = $"solve?auth={token}";

			StringContent content = new StringContent(JsonSerializer.Serialize(requestJson, serializerOptions), Encoding.UTF8, "application/json");
			content.Headers.Add(testIdHeaderName, testId.ToString());

			Console.WriteLine("Sending http post to challenge server");

			HttpResponseMessage response = await httpClient.PostAsync(urlSuffix, content);

			Console.WriteLine($"Response status: {response.StatusCode}");

			string body = await response.Content.ReadAsStringAsync();
			Console.WriteLine("Body: ");
			Console.WriteLine(body);

		}
	}
}
