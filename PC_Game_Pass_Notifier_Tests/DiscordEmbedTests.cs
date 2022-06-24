namespace PC_Game_Pass_Notifier_Tests
{
	public class DiscordEmbedTests
	{
		private readonly int urlCharacterNumber = 1024;

		// Notes:
		// xUnit creates class instance for each test
		// Test class constructor will be called per test
		// Use test class constructor for test setup
		// If you need TearDown, implement IDisposible and dispose method will be called per test

		private DiscordEmbed CreateTestEmbedWithCharacterCount(int urlCount, int titleCount, int descriptionCount)
		{
			string url = TestCaseUtilities.LoremIpsum(urlCount);
			string title = TestCaseUtilities.LoremIpsum(titleCount);
			string description = TestCaseUtilities.LoremIpsum(descriptionCount);
			string imageUrl = url;
			return new DiscordEmbed(url, title, description, imageUrl);
		}

		[Fact]
		public void CreateDiscordEmbedWithoutError_ParametersWithinLimits_IsSucessful()
		{
			Assert.IsType<DiscordEmbed>(CreateTestEmbedWithCharacterCount(urlCharacterNumber, DiscordEmbed.DiscordTitleCharacterLimit, DiscordEmbed.DiscordDescriptionCharacterLimit));
		}

		[Theory]
		[InlineData(DiscordEmbed.DiscordTitleCharacterLimit + 1, DiscordEmbed.DiscordDescriptionCharacterLimit)]
		[InlineData(DiscordEmbed.DiscordTitleCharacterLimit, DiscordEmbed.DiscordDescriptionCharacterLimit + 1)]
		public void CreateDiscordEmbedWithoutError_ParametersOutOfBounds_ThrowsException(int titleCharacters, int descriptionCharacters)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => CreateTestEmbedWithCharacterCount(urlCharacterNumber, titleCharacters, descriptionCharacters));
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(100, 500)]
		[InlineData(DiscordEmbed.DiscordTitleCharacterLimit, DiscordEmbed.DiscordDescriptionCharacterLimit)]
		public void CalculateSizeForEmbedMessage_IsSumOfTitleAndDescription_ReturnsTrue(int titleCharacters, int descriptionCharacters)
		{
			DiscordEmbed testEmbed = CreateTestEmbedWithCharacterCount(urlCharacterNumber, titleCharacters, descriptionCharacters);
			Assert.True(titleCharacters + descriptionCharacters == testEmbed.CalculateSizeForEmbedMessage());
		}

		[Fact]
		public void SplitEmbedsIntoSendableChunks_10EmbedsOfVariousSize_ReturnChunksWithMaxSizeWithinLimit()
		{
			// Create a large one, which should prevent the next larger one from taking the same chunk.
			List<DiscordEmbed> testEmbeds = new();
			DiscordEmbed singleDiscordEmbed = CreateTestEmbedWithCharacterCount(urlCharacterNumber, DiscordEmbed.DiscordTitleCharacterLimit, DiscordEmbed.DiscordDescriptionCharacterLimit);
			testEmbeds.Add(singleDiscordEmbed);

			// Create three equally large ones, which should fill a chunk.
			int equalDescriptionCharacterNumber = DiscordEmbed.DiscordEmbedMessageCharacterLimit / 3 - DiscordEmbed.DiscordTitleCharacterLimit;
			List<DiscordEmbed> equalEmbeds = new();
			for (int i = 0; i < 3; i++)
			{
				DiscordEmbed equalEmbed = CreateTestEmbedWithCharacterCount(urlCharacterNumber, DiscordEmbed.DiscordTitleCharacterLimit, equalDescriptionCharacterNumber);
				equalEmbeds.Add(equalEmbed);
				testEmbeds.Add(equalEmbed);
			}

			// Create DiscordEmbedMessageNumberLimit small ones, which should end up in the same chunk.
			List<DiscordEmbed> smallEmbeds = new();
			for (int i = 1; i <= DiscordEmbed.DiscordEmbedMessageNumberLimit; i++)
			{
				DiscordEmbed smallEmbed = CreateTestEmbedWithCharacterCount(urlCharacterNumber, i, i);
				smallEmbeds.Add(smallEmbed);
				testEmbeds.Add(smallEmbed);
			}

			// Create another small one, to check whether it properly lands in a new chunk.
			DiscordEmbed additionalSmallEmbed = CreateTestEmbedWithCharacterCount(urlCharacterNumber, 1, 1);
			testEmbeds.Add(additionalSmallEmbed);

			// Test chunks
			List<List<DiscordEmbed>> embedChunks = DiscordEmbed.SplitEmbedsIntoSendableChunks(testEmbeds);
			Assert.True(embedChunks.Count == 4);

			var firstChunk = embedChunks.First();
			Assert.True(firstChunk.Count == 1);
			Assert.Contains(singleDiscordEmbed, firstChunk);

			Assert.Equal(equalEmbeds, embedChunks[1]);
			Assert.Equal(smallEmbeds, embedChunks[2]);
			Assert.DoesNotContain(additionalSmallEmbed, embedChunks[2]);
			Assert.Contains(additionalSmallEmbed, embedChunks.Last());
			Assert.True(embedChunks.Last().Count == 1);
		}

		[Fact]
		public void SerializeObject_SingleEmbed_EqualsJsonString()
		{
			DiscordEmbed embed = new("https://testUrl.com", "TestTitle", "TestDescription", "https://testImageUrl.com");
			Assert.Equal(TestCaseUtilities.ExampleSingleEmbedJsonString, JsonConvert.SerializeObject(embed));
		}

		[Fact]
		public void SerializeObject_ListOfEmbeds_EqualsJsonString()
		{
			List<DiscordEmbed> embeds = new();
			embeds.Add(new DiscordEmbed("https://testUrl1.com", "FirstEmbed", "First is best", "https://testImageUrl1.com"));
			embeds.Add(new DiscordEmbed("https://testUrl2.com", "SecondEmbed", "Second is best", "https://testImageUrl2.com"));
			embeds.Add(new DiscordEmbed("https://testUrl3.com", "ThirdEmbed", "Third is best", "https://testImageUrl3.com"));
			Assert.Equal(TestCaseUtilities.ExampleListOfEmbedsJsonString, JsonConvert.SerializeObject(embeds));
		}
	}
}