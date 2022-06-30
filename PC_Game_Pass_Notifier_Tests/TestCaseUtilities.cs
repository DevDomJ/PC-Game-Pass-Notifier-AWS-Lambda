using System.Text;

namespace PC_Game_Pass_Notifier_Tests
{
	public static class TestCaseUtilities
	{
		private static string s_loremIpsumPatternString = @"Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua.
At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.";

		public static string ExampleSingleEmbedJsonString = "{\"url\":\"https://testUrl.com\",\"title\":\"TestTitle\",\"description\":\"TestDescription\",\"image\":{\"url\":\"https://testImageUrl.com\"}}";
		public static string ExampleListOfEmbedsJsonString = "[{\"url\":\"https://testUrl1.com\",\"title\":\"FirstEmbed\",\"description\":\"First is best\",\"image\":{\"url\":\"https://testImageUrl1.com\"}},{\"url\":\"https://testUrl2.com\",\"title\":\"SecondEmbed\",\"description\":\"Second is best\",\"image\":{\"url\":\"https://testImageUrl2.com\"}},{\"url\":\"https://testUrl3.com\",\"title\":\"ThirdEmbed\",\"description\":\"Third is best\",\"image\":{\"url\":\"https://testImageUrl3.com\"}}]";

		public static string LoremIpsum(int desiredLength)
		{
			if (desiredLength == s_loremIpsumPatternString.Length)
				return s_loremIpsumPatternString;
			if (desiredLength < s_loremIpsumPatternString.Length)
			{
				return s_loremIpsumPatternString.Substring(0, desiredLength);
			}
			else
			{
				int repetitions = desiredLength / s_loremIpsumPatternString.Length;
				int charactersLeft = desiredLength % s_loremIpsumPatternString.Length;
				StringBuilder stringBuilder = new();
				for (int i = 0; i < repetitions; i++)
				{
					stringBuilder.Append(s_loremIpsumPatternString);
				}
				if (charactersLeft > 0)
				{
					stringBuilder.Append(LoremIpsum(charactersLeft));
				}
				return stringBuilder.ToString();
			}
		}

		[Theory]
		[InlineData(0)]
		[InlineData(296)] // Size of s_loremIpsumPatternString
		[InlineData(5000)]
		private static void LoremIpsum_NumbersLessThanEqualToAndGreaterThanPatternStringLength_ReturnsStringOfDesiredLength(int desiredLength)
		{
			Assert.True(desiredLength == LoremIpsum(desiredLength).Length);
		}
	}
}