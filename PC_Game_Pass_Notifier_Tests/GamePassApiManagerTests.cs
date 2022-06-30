namespace PC_Game_Pass_Notifier_Tests
{
	public class GamePassApiManagerTests
	{
		private GamePassApiManager apiManager;

		// Setup - called for each test
		public GamePassApiManagerTests()
		{
			apiManager = new GamePassApiManager("", "", "{0}");
		}

		[Fact]
		public void CreateGamePassGamesFromJsonString_ValidJsonString_ReturnsFullGameList()
		{
			
		}

		[Theory]
		[InlineData("{}")]
		[InlineData("{test:\"test\"}")]
		public void CreateGamePassGamesFromJsonString_MissingProductsToken_ReturnsEmptyList(string jsonString)
		{
			Assert.Empty(apiManager.CreateGamePassGamesFromJsonString(jsonString));
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
