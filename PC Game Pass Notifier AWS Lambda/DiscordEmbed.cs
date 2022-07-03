using System;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace PC_Game_Pass_Notifier_AWS_Lambda
{

	[Serializable()]
	public class DiscordEmbed : ISerializable
	{
		public const int DiscordTitleCharacterLimit = 256;
		public const int DiscordDescriptionCharacterLimit = 4096;
		public const int DiscordEmbedMessageCharacterLimit = 6000;
		public const int DiscordEmbedMessageNumberLimit = 10;

		public string Url { get; set; }
		public string Title
		{
			get { return _title; }
			[MemberNotNull(nameof(_title))] // Fixes compiler warning "CS8618 Non-nullable field must contain a non-null value when exiting constructor"
			set {
				if (value.Length <= DiscordTitleCharacterLimit)
				{
					_title = value;
				} else
				{
					throw new ArgumentOutOfRangeException($"Embed title character limit of {DiscordTitleCharacterLimit} exceeded by {value.Length - DiscordTitleCharacterLimit}");
				}
			}
		}
		public string Description
		{
			get { return _description; }
			[MemberNotNull(nameof(_description))]
			set {
				if (value.Length <= DiscordDescriptionCharacterLimit)
				{
					_description = value;
				} else
				{
					throw new ArgumentOutOfRangeException($"Embed description character limit of {DiscordDescriptionCharacterLimit} exceeded by {value.Length - DiscordDescriptionCharacterLimit}");
				}
			}
		}
		public string ImageUrl { get; set; }

		private string _description;
		private string _title;

		public DiscordEmbed(string url, string title, string description, string imageUrl)
		{
			Url = url;
			Title = title;
			Description = description;
			ImageUrl = imageUrl;
		}

		public static List<List<DiscordEmbed>> SplitEmbedsIntoSendableChunks(List<DiscordEmbed> embeds)
		{
			List<List<DiscordEmbed>> embedsChunkList = new();
			List<DiscordEmbed> currentEmbedsList = new();
			int currentEmbedListSize = 0;
			foreach (DiscordEmbed embed in embeds)
			{
				int embedSize = embed.CalculateSizeForEmbedMessage();
				if (currentEmbedListSize + embedSize > DiscordEmbedMessageCharacterLimit || currentEmbedsList.Count >= DiscordEmbedMessageNumberLimit)
				{
					embedsChunkList.Add(currentEmbedsList);
					currentEmbedsList = new List<DiscordEmbed>();
					currentEmbedsList.Add(embed);
					currentEmbedListSize = embedSize;
				} else
				{
					currentEmbedsList.Add(embed);
					currentEmbedListSize += embedSize;
				}
			}
			if (currentEmbedsList.Count > 0)
			{
				embedsChunkList.Add(currentEmbedsList);
			}
			return embedsChunkList;
		}

		public int CalculateSizeForEmbedMessage()
		{
			return Title.Length + Description.Length;
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("url", Url, typeof(string));
			info.AddValue("title", Title, typeof(string));
			info.AddValue("description", Description, typeof(string));

			if (ImageUrl.Length > 0)
			{
				Dictionary<string, string> imageObject = new();
				imageObject.Add("url", ImageUrl);
				info.AddValue("image", imageObject, typeof(Dictionary<string, string>));
			}
		}
	}
}