using System;
using System.Text;
using System.Net.Http.Json;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Net.Http;
using DotNetEnv;

namespace PC_Game_Pass_Notifier_AWS_Lambda
{
	// rename to DiscordWebhook or similar
	public class DiscordApiManager
	{
		private const string DISCORD_WEBHOOK_URL_ENVIRONMENT_VARIABLE_NAME = "DISCORD_WEBHOOK_URL";
		private readonly string _discordWebHookUrl;

		public DiscordApiManager()
		{
			Env.Load();
			var discordWebHookUrl = Environment.GetEnvironmentVariable(DISCORD_WEBHOOK_URL_ENVIRONMENT_VARIABLE_NAME);
			if(string.IsNullOrEmpty(discordWebHookUrl))
			{
				PcGamePassNotifier.LogError($"Environment variable {DISCORD_WEBHOOK_URL_ENVIRONMENT_VARIABLE_NAME} not set.");
				throw new Exception($"Environment variable {DISCORD_WEBHOOK_URL_ENVIRONMENT_VARIABLE_NAME} not set.");
			} else
			{
				_discordWebHookUrl = discordWebHookUrl;
			}

		}

		public DiscordApiManager(string discordWebHookUrl)
		{
			_discordWebHookUrl = discordWebHookUrl;
		}

		public bool SendMessage(List<DiscordEmbed> embeds, string? content = null)
		{
			HttpResponseMessage? response = null;
			var dictionary = new Dictionary<string, object>
				{
					{"embeds", embeds}
				};
			if (content != null)
			{
				dictionary.Add("content", content);
			}
			var httpContent = new StringContent(JsonConvert.SerializeObject(dictionary), Encoding.UTF8, "application/json");

			try
			{
				response = PcGamePassNotifier.HttpClient.PostAsync(_discordWebHookUrl, httpContent).Result;
				response.EnsureSuccessStatusCode();
				return true;
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
						return SendMessage(embeds, content);
					}
				}
				PcGamePassNotifier.LogError($"An exception occured while trying to send Discord message: {new StreamReader(httpContent.ReadAsStream()).ReadToEnd()}\nexception: {exception.Message}");
				return false;
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
