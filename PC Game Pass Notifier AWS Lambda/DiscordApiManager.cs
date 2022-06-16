using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;

namespace PC_Game_Pass_Notifier_AWS_Lambda
{
	//rename to DiscordWebhook or similar
	internal class DiscordApiManager
	{
		private readonly string _discordWebHookUrl;
		private const int DiscordContentCharacterLimit = 2000;

		public DiscordApiManager(string discordWebHookUrl)
		{
			_discordWebHookUrl = discordWebHookUrl;
		}

		public void SendMessage(string content)
		{
			HttpResponseMessage? response = null;
			try
			{
				var dictionary = new Dictionary<string, string>
			{
				{ "content", content }
			};
				var httpContent = new FormUrlEncodedContent(dictionary);
				response = PcGamePassNotifier.HttpClient.PostAsync(_discordWebHookUrl, httpContent).Result;
				response.EnsureSuccessStatusCode();

			} catch (Exception exception)
			{
				//Check response for rate limit and retry when viable: https://discord.com/developers/docs/topics/rate-limits
				if (response != null)
				{
					var retryAfter = response.Headers.RetryAfter;
					//TODO: Move retry to sender and maybe send an "and x more" message, when retryAfter time exceeds time remaining before lambda timeout?
					if (retryAfter != null && retryAfter.Delta != null && retryAfter.Delta >= new TimeSpan(0, 0, 3))
					{
						Task.Delay((TimeSpan) retryAfter.Delta);
						SendMessage(content);
						return;
					}
				}
				PcGamePassNotifier.LogError($"An exception occured while trying to send Discord message: {content} \nexception: {exception.Message}");
			}
		}

		public void SendAddedGamesMessage(List<GamePassGame> addedGames)
		{
			StringBuilder stringBuilder = new();
			if(addedGames.Count == 1)
			{
				stringBuilder.Append("Ein neues Spiel wurde");
			} else
			{
				stringBuilder
				.Append(addedGames.Count)
				.Append(" neue Spiele wurden");
			}
			stringBuilder.AppendLine(" soeben dem PC Game Pass hinzugefügt:");
			SendGamesListForStringBuilder(addedGames, stringBuilder);
		}
		public void SendRemovedGamesMessage(List<GamePassGame> removedGames)
		{
			StringBuilder stringBuilder = new();
			if(removedGames.Count == 1)
			{
				stringBuilder.Append("Es wurde ein Spiel");
			} else
			{
				stringBuilder.Append($"Es wurden {removedGames.Count} Spiele");
			}
			stringBuilder.AppendLine($" aus dem Game Pass entfernt:");
			SendGamesListForStringBuilder(removedGames, stringBuilder);
		}

		private void SendGamesListForStringBuilder(List<GamePassGame> gamesList, StringBuilder stringBuilder)
		{
			int counter = 1;
			foreach (var game in gamesList)
			{
				string gameUpdateMessage = game.AsUpdateDescription();
				if (stringBuilder.Length + gameUpdateMessage.Length > DiscordContentCharacterLimit)
				{
					SendMessage(stringBuilder.ToString());
					stringBuilder.Clear();
					AppendGameUpdateMessageToStringBuilder(counter, gameUpdateMessage, stringBuilder);
				} else
				{
					AppendGameUpdateMessageToStringBuilder(counter, gameUpdateMessage, stringBuilder);
				}
				counter++;
			}
			SendMessage(stringBuilder.ToString());
		}

		private void AppendGameUpdateMessageToStringBuilder(int counter, string gameUpdateMessage, StringBuilder stringBuilder)
		{
			//"```" triggers code blocks in discord
			//TODO: Find a way to convert into rich embed messages --> needs something like shop URL however
			stringBuilder
				.Append("```")		
				.Append(counter)
				.Append(". ")
				.Append(gameUpdateMessage)
				.Append("```");
		}
	}
}
