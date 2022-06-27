using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PC_Game_Pass_Notifier_AWS_Lambda
{
	class GamePassApiManager
	{
		private string _pcGamePassAllGamesCollectionUrl;
		private string _pcGamePassConsoleGamesCollectionUrl;
		private string _pcGamePassDetailsUrlPattern;

		public GamePassApiManager(string pcGamePassAllGamesCollectionUrl, string pcGamePassConsoleGamesCollectionUrl, string pcGamePassDetailsUrlPattern)
		{
			_pcGamePassAllGamesCollectionUrl = pcGamePassAllGamesCollectionUrl;
			_pcGamePassConsoleGamesCollectionUrl = pcGamePassConsoleGamesCollectionUrl;
			_pcGamePassDetailsUrlPattern = pcGamePassDetailsUrlPattern;

			if (!_pcGamePassDetailsUrlPattern.Contains("{0}"))
			{
				throw new ArgumentException($"The pcGamePassDetailsUrlPattern parameter '{_pcGamePassDetailsUrlPattern}' must contain a placeHolder for the game id collection, looking like this: {{0}}");
			}
		}

		public List<string> GetCurrentPcGamePassGameList()
		{
			List<string> allGamePassGameIds = GetGamePassGameListForUrl(_pcGamePassAllGamesCollectionUrl);
			List<string> consoleGamePassGameIds = GetGamePassGameListForUrl(_pcGamePassConsoleGamesCollectionUrl);
			List<string> consoleOnlyGameIds = consoleGamePassGameIds.Except(allGamePassGameIds).ToList();
			return allGamePassGameIds.Except(consoleOnlyGameIds).ToList();
		}

		public List<string> GetGamePassGameListForUrl(string url)
		{
			string webResponseContent = PcGamePassNotifier.HttpClient.GetStringAsync(url).Result;
			JArray? tokenArray = JsonConvert.DeserializeObject<JArray>(webResponseContent);
			var gameIds = new List<string>();
			if (tokenArray == null || tokenArray.Count == 0)
			{
				PcGamePassNotifier.LogWarning($"Could not deserialize pcGamesList of {url} from response: [{webResponseContent}]");
				return gameIds;
			}
			foreach (JToken token in tokenArray)
			{
				var idToken = token["id"];
				var idTokenValue = idToken?.Value<string>();
				if (idToken == null)
				{
					if (token["siglId"] == null)
					{
						PcGamePassNotifier.LogWarning("idToken is null");
					}
					continue;
				}
				if (idTokenValue == null)
				{
					PcGamePassNotifier.LogWarning("Could not retrieve id from token: " + idToken.ToString());
					continue;
				}
				gameIds.Add(idTokenValue);
			}
			return gameIds;
		}

		public string GetDetailsForGameIdList(List<string> gameIds)
		{
			StringBuilder stringBuilder = new();
			foreach (string id in gameIds)
			{
				stringBuilder
					.Append(id)
					.Append(',');
			}
			stringBuilder.Remove(stringBuilder.Length - 1, 1);
			string pcGamePassDetailsUrl = String.Format(_pcGamePassDetailsUrlPattern, stringBuilder.ToString());
			return PcGamePassNotifier.HttpClient.GetStringAsync(pcGamePassDetailsUrl).Result;
		}

		public List<GamePassGame> CreateGamePassGamesFromJsonString(string jsonString)
		{
			List<GamePassGame> gamePassGames = new();
			JObject pcGamePassDetails = JObject.Parse(jsonString);
			JToken? productsToken = pcGamePassDetails["Products"];
			if (productsToken == null)
			{
				PcGamePassNotifier.LogWarning($"gamePassDetails JSON string does not include 'Products' item. JSON string: [{jsonString}]");
				return gamePassGames;

			}
			IList<JToken> products = productsToken.Children().ToList();
			foreach (JToken product in products)
			{
				JToken? localizedPropertiesToken = product["LocalizedProperties"];
				JToken? productIdToken = product["ProductId"];
				if (localizedPropertiesToken == null || productIdToken == null)
				{
					PcGamePassNotifier.LogWarning("Product does not contain LocalizedProperties or ProductId. Product: " + product.ToString());
					continue;
				}
				JToken firstLocalizedPropertiesToken = localizedPropertiesToken.Children().First();
				GamePassGame? gamePassGame = firstLocalizedPropertiesToken.ToObject<GamePassGame>();
				if (gamePassGame == null)
				{
					PcGamePassNotifier.LogWarning("Failed to convert localizedProperties to GamePassGame. LocalizedProperties: " + firstLocalizedPropertiesToken.ToString());
					continue;
				}
				JToken? imagesToken = localizedPropertiesToken["Images"];
				if (imagesToken != null)
				{
					gamePassGame.SetProductArtUrlFromImagesToken(imagesToken);
				}
				var productId = productIdToken.Value<string>();
				if (productId == null)
				{
					PcGamePassNotifier.LogWarning($"Failed to retrieve productId from productIdToken: {productIdToken.ToString()} of game: " + JsonConvert.SerializeObject(gamePassGame, Formatting.Indented));
					continue;
				}
				gamePassGame.ProductId = productId;
				gamePassGames.Add(gamePassGame);
			}

			return gamePassGames;
		}
	}
}