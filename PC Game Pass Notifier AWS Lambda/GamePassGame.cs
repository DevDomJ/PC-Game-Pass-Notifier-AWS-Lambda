using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public GamePassGame()
		{
            DeveloperName = "";
            ProductTitle = "";
            PublisherName = "";
            ProductDescription = "";
            ShortDescription = "";
            ProductId = "";
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

        public void AsUpdateEmbed()
		{

		}
    }
}
