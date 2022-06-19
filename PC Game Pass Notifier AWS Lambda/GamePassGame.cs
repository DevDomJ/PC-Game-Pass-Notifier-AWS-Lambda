using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

		public GamePassGame()
		{
			DeveloperName = "";
			ProductTitle = "";
			PublisherName = "";
			ProductDescription = "";
			ShortDescription = "";
			ProductId = "";
			ProductArtUrl = "";
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
			var currentPurpose = ImagePurpose.Other;
			foreach (JToken imageToken in imagesToken.Children())
			{
				var imagePurposeToken = imageToken["ImagePurpose"];
				var imagePurposeTokenValue = imagePurposeToken?.Value<string>();
				if (imagePurposeTokenValue != null)
				{
					ImagePurpose purpose = GetImagePurposeForString(imagePurposeTokenValue);
					if (ProductArtUrl.Length == 0 || purpose < currentPurpose)
					{
						ProductArtUrl = "https://" + imagePurposeTokenValue;
						currentPurpose = purpose;
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

		public string AsUpdateEmbed()
		{
			Dictionary<string, string> imageObject = new();
			imageObject.Add("url", ProductArtUrl);

			Dictionary<string, string> embedObject = new();
			embedObject.Add("url", $"https://www.xbox.com/de-de/games/store/gamepass/{ProductId}");
			embedObject.Add("title", ProductTitle);
			embedObject.Add("description", ShortDescription);
			embedObject.Add("image", JsonConvert.SerializeObject(imageObject));

			// Discord embed limit:
			// Additionally, the combined sum of characters in all title, description, field.name, field.value, footer.text, and author.name fields across all embeds attached
			// to a message must not exceed 6000 characters. Violating any of these constraints will result in a Bad Request response.
			return JsonConvert.SerializeObject(embedObject);
		}
	}
}
