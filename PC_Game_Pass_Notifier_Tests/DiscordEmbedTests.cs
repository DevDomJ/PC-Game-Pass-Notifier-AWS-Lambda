namespace PC_Game_Pass_Notifier_Tests
{
	public class DiscordEmbedTests
	{
		[Fact]
		public void CreateDiscordEmbedWithoutError_ParametersWithinLimits_ReturnTrue()
		{
			string url = TestCaseUtilities.LoremIpsum(1024);
			string title = TestCaseUtilities.LoremIpsum(256);
			string description = TestCaseUtilities.LoremIpsum(4096);
			string imageUrl = url;
			new DiscordEmbed(url, title, description, imageUrl);
			// Assert not resolved in VS Code, fix in Visual Studio later
			// Assert.??
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		public void Test2(int i)
		{

		}
	}
}