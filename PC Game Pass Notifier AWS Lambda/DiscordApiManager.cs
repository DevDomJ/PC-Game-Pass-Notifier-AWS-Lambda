using System;
using System.Text;
using Newtonsoft.Json;

namespace PC_Game_Pass_Notifier_AWS_Lambda
{
	// rename to DiscordWebhook or similar
	internal class DiscordApiManager
	{
		private readonly string _discordWebHookUrl;

		public DiscordApiManager(string discordWebHookUrl)
		{
			_discordWebHookUrl = discordWebHookUrl;
		}

		public void SendMessage(List<DiscordEmbed> embeds, string? content = null)
		{
			HttpResponseMessage? response = null;
			string embedJsonString = JsonConvert.SerializeObject(embeds);
			try
			{
				var dictionary = new Dictionary<string, string>
				{
					{"embeds", embedJsonString}
				};
				if (content != null)
				{
					dictionary.Add("content", content);
				}

				var httpContent = new FormUrlEncodedContent(dictionary);
				response = PcGamePassNotifier.HttpClient.PostAsync(_discordWebHookUrl, httpContent).Result;
				response.EnsureSuccessStatusCode();
			} catch (Exception exception)
			{
				// Check response for rate limit and retry when viable: https://discord.com/developers/docs/topics/rate-limits
				if (response != null)
				{
					var retryAfter = response.Headers.RetryAfter;
					// TODO: Move retry to sender and maybe send an "and x more" message, when retryAfter time exceeds time remaining before lambda timeout?
					if (retryAfter != null && retryAfter.Delta != null && retryAfter.Delta >= new TimeSpan(0, 0, 3))
					{
						Task.Delay((TimeSpan) retryAfter.Delta);
						SendMessage(embeds, content);
						return;
					}
				}
				PcGamePassNotifier.LogError($"An exception occured while trying to send Discord message: {content} \nembeds: {embedJsonString}\nexception: {exception.Message}");
			}
		}

		public void SendAddedGamesMessage(List<GamePassGame> addedGames)
		{
			StringBuilder stringBuilder = new();
			if (addedGames.Count == 1)
			{
				stringBuilder.Append("Ein neues Spiel wurde");
			} else
			{
				stringBuilder
				.Append(addedGames.Count)
				.Append(" neue Spiele wurden");
			}
			stringBuilder.AppendLine(" soeben dem PC Game Pass hinzugefügt:");
			SendGamesListWithIntro(addedGames, stringBuilder.ToString());
		}
		public void SendRemovedGamesMessage(List<GamePassGame> removedGames)
		{
			StringBuilder stringBuilder = new();
			if (removedGames.Count == 1)
			{
				stringBuilder.Append("Es wurde ein Spiel");
			} else
			{
				stringBuilder.Append($"Es wurden {removedGames.Count} Spiele");
			}
			stringBuilder.AppendLine($" aus dem Game Pass entfernt:");
			SendGamesListWithIntro(removedGames, stringBuilder.ToString());
		}

		private void SendGamesListWithIntro(List<GamePassGame> gamesList, string intro)
		{
			int index = 1;
			List<DiscordEmbed> embeds = new();
			foreach (GamePassGame game in gamesList)
			{
				embeds.Add(game.ToDiscordEmbedWithIndex(index));
				index++;
			}
			List<List<DiscordEmbed>> embedChunks = DiscordEmbed.SplitEmbedsIntoSendableChunks(embeds);
			bool introAdded = false;
			foreach (List<DiscordEmbed> embedChunk in embedChunks)
			{
				if (introAdded)
				{
					SendMessage(embedChunk);
				} else
				{
					introAdded = true;
					SendMessage(embedChunk, intro);
				}
			}
		}
	}
}
