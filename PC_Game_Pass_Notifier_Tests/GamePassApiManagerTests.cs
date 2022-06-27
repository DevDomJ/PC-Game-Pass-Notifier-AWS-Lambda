namespace PC_Game_Pass_Notifier_Tests
{
	public class GamePassApiManagerTests
	{
		[Fact]
		public void CreateGamePassGamesFromJsonString_ValidJsonString_ReturnsFullGameList()
		{
			Assert.True(false);
		}

		[Fact]
		public void CreateGamePassGamesFromJsonString_MissingProductsToken_ReturnsEmptyList()
		{

		}

		[Theory]
		[InlineData("")]
		public void CreateGamePassGamesFromJsonString_InvalidProductIdToken_SkipsIncompleteGames(string jsonString)
		{

		}

		[Theory]
		[InlineData("")]
		public void CreateGamePassGamesFromJsonString_InvalidLocalizedPropertiesToken_SkipsIncompleteGames(string jsonString)
		{

		}

		[Theory]
		[InlineData("")]
		public void CreateGamePassGamesFromJsonString_InvalidImagesToken_LeavesImageUrlEmpty(string jsonString)
		{

		}
	}
}
