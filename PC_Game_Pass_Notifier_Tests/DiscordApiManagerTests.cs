namespace PC_Game_Pass_Notifier_Tests
{
	//Setup - will be executed only once, instead of for every test case
	public class DiscordApiManagerTestFixture
	{
		public DiscordApiManager ApiManager { get; set; }
		public DiscordApiManagerTestFixture()
		{
			ApiManager = new DiscordApiManager();
		}
	}

	public class DiscordApiManagerTests : IClassFixture<DiscordApiManagerTestFixture>
	{
		private readonly DiscordApiManagerTestFixture fixture;

		public DiscordApiManagerTests(DiscordApiManagerTestFixture fixture)
		{
			this.fixture = fixture;
		}

		//TODO: Works for GitHub Actions, but not yet on local machine. Maybe try something like: https://stackoverflow.com/a/43951218
		[Fact]
		public void SendMessage_WithContentAndEmbeds_IsSuccessful()
		{
			List<DiscordEmbed> embeds = new();
			embeds.Add(new DiscordEmbed(
				"https://www.xbox.com/de-de/games/store/gamepass/9NPP17LHJ3MK",
				"1. Yakuza 0",
				"Yakuza 0 lässt Glanz, Glitzer und die hemmungslose Dekadenz der 80er wieder aufleben. Kämpfe dich mit Protagonist Kazuma Kiryu und dem wiederkehrenden Charakter Goro Majima quer durch Tokio und Osaka.",
				"https://store-images.s-microsoft.com/image/apps.59845.13785223586843168.612c6166-3afd-413c-9b13-549ae975f01e.c0a021d0-4b94-4ad8-9571-fad1dccc66d1"));
			embeds.Add(new DiscordEmbed(
				"https://www.xbox.com/de-de/games/store/gamepass/9NBJ51BD0LTH",
				"2. Yakuza Kiwami",
				"Für seinen Freund nimmt Kazuma Kiryu die Schuld für ein Verbrechen auf sich, das er nicht beging, und wird zu zehn Jahren Haft verurteilt. Nach seiner Freilassung kehrt er, verstoßen von seiner Yakuza-Familie, in eine für ihn unbekannte Welt zurück.",
				"https://store-images.s-microsoft.com/image/apps.5293.13512592555926242.4f764cb5-1ca8-4601-9f0f-fd3d82976ea7.cfad000f-cfd8-4a50-8262-0b562e09ff87"));
			embeds.Add(new DiscordEmbed(
				"https://www.xbox.com/de-de/games/store/gamepass/9PBJL0NLFMK9",
				"3. Yakuza Kiwami 2",
				"Der Tojo-Clan und die Omi-Allianz stehen kurz vor einem Krieg. Kazuma Kiryu, der Drache von Dojima, tritt als Vermittler zwischen den Clans auf. Doch Ryuji Goda, der Drache von Kansai, will den Krieg. In dieser Welt kann es nur einen Drachen geben. In dieser Welt kann es nur einen Drachen geben.",
				"https://store-images.s-microsoft.com/image/apps.2712.14117812508694764.cd76a3cb-9c02-4790-93a0-eae298c80bb7.2951e38f-1495-49a2-8fa7-394ab6b75d86"));
			Assert.True(fixture.ApiManager.SendMessage(embeds, "3 neue Spiele wurden soeben dem PC Game Pass hinzugefügt:"), "Failed to send message via Discord Webhook");
		}

		[Fact]
		public void SendMessage_GamePassGameWithoutShortDescription_IsSuccessful()
		{
			//TODO: Add test case for test_gameWithoutShortDescription.json
			//GamePassApiManager.CreateGamePassGamesFromJsonFile();
		}
	}

}