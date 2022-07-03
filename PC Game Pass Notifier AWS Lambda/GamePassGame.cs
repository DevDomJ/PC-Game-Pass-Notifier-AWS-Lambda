using System;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PC_Game_Pass_Notifier_AWS_Lambda
{
	public class GamePassGame
	{
		public string DeveloperName { get; set; }
		public string ProductTitle { get; set; }
		public string PublisherName { get; set; }
		public string ProductDescription { get; set; }
		public string ShortDescription { get; set; }
		public string ProductId { get; set; }
		public string ProductArtUrl { get; set; }
		public ImagePurpose ProductArtImagePurpose { get; set; }

		public GamePassGame()
		{
			DeveloperName = "";
			ProductTitle = "";
			PublisherName = "";
			ProductDescription = "";
			ShortDescription = "";
			ProductId = "";
			ProductArtUrl = "";
			ProductArtImagePurpose = ImagePurpose.Other;
		}

		public enum ImagePurpose
		{
			TitledHeroArt,
			SuperHeroArt,
			BoxArt,
			Poster,
			Other
		};

		public void SetProductArtUrlFromImagesToken(JToken imagesToken)
		{
			foreach (JToken imageToken in imagesToken.Children())
			{
				var imagePurposeToken = imageToken["ImagePurpose"];
				var imagePurposeTokenValue = imagePurposeToken?.Value<string>();
				if (imagePurposeTokenValue != null)
				{
					ImagePurpose purpose = GetImagePurposeForString(imagePurposeTokenValue);
					if (ProductArtUrl.Length == 0 || purpose < ProductArtImagePurpose)
					{
						string? imageUrl = imageToken["Uri"]?.Value<string>();
						if (imageUrl != null)
						{
							ProductArtImagePurpose = purpose;
							ProductArtUrl = "https:" + imageUrl;
						}
					}
				}
			}
		}

		private ImagePurpose GetImagePurposeForString(string purposeString)
		{
			switch (purposeString)
			{
				case "TitledHeroArt":
					return ImagePurpose.TitledHeroArt;
				case "SuperHeroArt":
					return ImagePurpose.SuperHeroArt;
				case "BoxArt":
					return ImagePurpose.BoxArt;
				case "Poster":
					return ImagePurpose.Poster;
				default:
					return ImagePurpose.Other;
			}
		}

		public string AsUpdateDescription()
		{
			StringBuilder stringBuilder = new();
			return stringBuilder
				.AppendLine(ProductTitle)
				.AppendLine(ShortDescription)
				.Append("Entwickler: ").AppendLine(DeveloperName)
				.Append("Publisher: ").AppendLine(PublisherName)
				.ToString();
		}

		public override bool Equals(Object? other)
		{
			return other is GamePassGame game
				&& game.DeveloperName == DeveloperName
				&& game.ProductTitle == ProductTitle
				&& game.PublisherName == PublisherName
				&& game.ProductDescription == ProductDescription
				&& game.ShortDescription == ShortDescription
				&& game.ProductId == ProductId
				&& game.ProductArtUrl == ProductArtUrl
				&& game.ProductArtImagePurpose == ProductArtImagePurpose;
		}


		public DiscordEmbed ToDiscordEmbedWithIndex(int index)
		{
			return new DiscordEmbed($"https://www.xbox.com/de-de/games/store/gamepass/{ProductId}", $"{index}. {ProductTitle}", ShortDescription, ProductArtUrl);
		}

		public override int GetHashCode()
		{
			return ProductId.GetHashCode();
		}
	}
}
