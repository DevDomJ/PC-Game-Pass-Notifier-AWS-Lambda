namespace PC_Game_Pass_Notifier_Tests
{
	public class GamePassApiManagerTests
	{
		private GamePassApiManager apiManager;

		private string GetJsonStringFromExampleJsonFile(string fileName)
		{
			return File.ReadAllText(@"..\..\..\ExampleJsonFiles\" + fileName);
		}

		private List<GamePassGame> GetDeserializedGamePassGamesFromJsonFile(string fileName)
		{
			List<GamePassGame>? gamesList = JsonConvert.DeserializeObject<List<GamePassGame>>(File.ReadAllText(@"..\..\..\ExampleJsonFiles\" + fileName));
			if(gamesList is null)
			{
				throw new Exception("Should not happen - revise your expected serialzed json file");
			}
			return gamesList;
		}

		// Setup - called for each test
		public GamePassApiManagerTests()
		{
			apiManager = new GamePassApiManager("", "", "{0}");
		}

		[Fact]
		public void CreateGamePassGamesFromJsonString_ValidJsonString_ReturnsFullGameList()
		{
			List<GamePassGame> expectedList = GetDeserializedGamePassGamesFromJsonFile("serialized_validDetailsFor10Games.json");
			List<GamePassGame> actualList = apiManager.CreateGamePassGamesFromJsonString(GetJsonStringFromExampleJsonFile("test_validDetailsFor10Games.json"));
			Assert.Equal(expectedList, actualList);
		}

		[Theory]
		[InlineData("{}")]
		[InlineData("{test:\"test\"}")]
		public void CreateGamePassGamesFromJsonString_MissingProductsToken_ReturnsEmptyList(string jsonString)
		{
			Assert.Empty(apiManager.CreateGamePassGamesFromJsonString(jsonString));
		}

		[Fact]
		public void CreateGamePassGamesFromJsonString_InvalidProductIdToken_SkipsIncompleteGames()
		{
			List<GamePassGame> expectedList = GetDeserializedGamePassGamesFromJsonFile("serialized_firstAndThirdGame.json");
			List<GamePassGame> actualList = apiManager.CreateGamePassGamesFromJsonString(GetJsonStringFromExampleJsonFile("test_secondGameMissingProductId.json"));
			Assert.Equal(expectedList, actualList);
		}

		[Theory]
		[InlineData("test_secondGameInvalidLocalizedPropertiesToken.json")]
		[InlineData("test_secondGameMissingLocalizedPropertiesToken.json")]
		public void CreateGamePassGamesFromJsonString_InvalidLocalizedPropertiesToken_SkipsIncompleteGames(string fileName)
		{
			List<GamePassGame> expectedList = GetDeserializedGamePassGamesFromJsonFile("serialized_firstAndThirdGame.json");
			List<GamePassGame> actualList = apiManager.CreateGamePassGamesFromJsonString(GetJsonStringFromExampleJsonFile(fileName));
			Assert.Equal(expectedList, actualList);
		}

		[Fact]
		public void CreateGamePassGamesFromJsonString_InvalidImagesToken_LeavesImageUrlEmpty()
		{
			List<GamePassGame> gamesList = apiManager.CreateGamePassGamesFromJsonString(GetJsonStringFromExampleJsonFile("test_secondGameMissingImagesToken.json"));
			Assert.NotEmpty(gamesList[0].ProductArtUrl);
			Assert.Empty(gamesList[1].ProductArtUrl);
			Assert.NotEmpty(gamesList[2].ProductArtUrl);
		}
	}
}
