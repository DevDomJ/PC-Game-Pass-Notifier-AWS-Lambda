using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PC_Game_Pass_Notifier_AWS_Lambda;

public class PcGamePassNotifier
{
	private static ILambdaContext? s_lambdaContext;
	private static readonly string s_gamePassGamesFileName = "pcGamePassGames.json";
	private static readonly HttpClient s_httpClient = new();

	private Dictionary<string, GamePassGame> _gamePassGames;
	private readonly GamePassApiManager _pcGamePassApiManager;
	private readonly DiscordApiManager _discordApiManager;
	private readonly AmazonS3Client _s3Client;
	private string _bucketName;

	public static HttpClient HttpClient => s_httpClient;

	public static async Task InitializeLambdaCall(Dictionary<string, string> inputJsonDictionary, ILambdaContext context)
	{
		s_lambdaContext = context;
		await new PcGamePassNotifier(inputJsonDictionary).InitializeAndUpdateGamePassGames();
	}

	public static void LogInformation(string logString)
	{
		if (s_lambdaContext == null)
			return;
		s_lambdaContext.Logger.LogInformation(logString);
	}

	public static void LogError(string logString)
	{
		if (s_lambdaContext == null)
			return;
		s_lambdaContext.Logger.LogError(logString);
	}

	public static void LogWarning(string logString)
	{
		if (s_lambdaContext == null)
			return;
		s_lambdaContext.Logger.LogWarning(logString);
	}

	/// <summary>
	/// Create a new instance of the PcGamePassNotifier from the a json dictionary input.
	/// <br></br>Expects the <paramref name="inputJsonDictionary"/> to have the following keys:
	/// <list type="table">
	///		<item>
	///			<term>bucketName</term>
	///			<description>Name of the AWS bucket the "pcGamePassGames.json" file is stored in.</description>
	///		</item>
	///		<item>
	///			<term>allGamesUrl</term>
	///			<description>PC game pass API URL to get the game ids for the all games collection.</description>
	///		</item>
	///		<item>
	///			<term>consoleGamesUrl</term>
	///			<description>PC game pass API URL to get the game ids for the console games collection.</description>
	///		</item>
	///		<item>
	///			<term>detailUrlPattern</term>
	///			<description>PC game pass API URL to get detailed information for a list of game ids in the format: "<![CDATA[protocol://hostname/path?queryParameterForIds={0}&otherQueryParameters]]>".</description>
	///		</item>
	///		<item>
	///			<term>discordWebhookUrl</term>
	///			<description>URL of the Discord webhook to send update messages to.</description>
	///		</item>
	/// </list>
	/// </summary>
	/// <param name="inputJsonDictionary"></param>
	public PcGamePassNotifier(Dictionary<string, string> inputJsonDictionary)
	{
		LogInformation("Received input JSON: " + JsonConvert.SerializeObject(inputJsonDictionary));

		var pcGamePassAllGamesCollectionUrl = GetKeyFromDictionary(inputJsonDictionary, "allGamesUrl");
		var pcGamePassConsoleGamesCollectionUrl = GetKeyFromDictionary(inputJsonDictionary, "consoleGamesUrl");
		var pcGamePassDetailsUrlPattern = GetKeyFromDictionary(inputJsonDictionary, "detailUrlPattern");

		_pcGamePassApiManager = new GamePassApiManager(pcGamePassAllGamesCollectionUrl, pcGamePassConsoleGamesCollectionUrl, pcGamePassDetailsUrlPattern);
		_discordApiManager = new DiscordApiManager(GetKeyFromDictionary(inputJsonDictionary, "discordWebhookUrl"));
		_gamePassGames = new Dictionary<string, GamePassGame>();
		_s3Client = new AmazonS3Client(Amazon.RegionEndpoint.EUCentral1);
		_bucketName = GetKeyFromDictionary(inputJsonDictionary, "bucketName");
	}

	private string GetKeyFromDictionary(Dictionary<string, string> dictionary, string key)
	{
		if (!dictionary.TryGetValue(key, out var value))
		{
			throw new KeyNotFoundException($"Key '{key}' not found in input json: " + JsonConvert.SerializeObject(dictionary));
		};
		return value;
	}

	public async Task InitializeAndUpdateGamePassGames()
	{
		await DeserializeGamePassGames();
		if (UpdateGamePassGames())
		{
			await SerializeGamePassGames();
		}
		_s3Client.Dispose();
		HttpClient.Dispose();
	}

	public bool UpdateGamePassGames()
	{
		var gameIds = _pcGamePassApiManager.GetCurrentPcGamePassGameList();
		return CompareGameListAndNotifyAboutUpdates(gameIds);
	}

	public bool CompareGameListAndNotifyAboutUpdates(List<string> newIds)
	{
		List<string> gamePassGameIds = _gamePassGames.Keys.ToList();
		List<string> addedGameIds = newIds.Except(gamePassGameIds).ToList();
		List<string> removedGameIds = gamePassGameIds.Except(newIds).ToList();
		LogInformation($"Found {addedGameIds.Count} new {(addedGameIds.Count == 1 ? "game" : "games")} and {removedGameIds.Count} removed {(removedGameIds.Count == 1 ? "game" : "games")}.");
		if (addedGameIds.Count > 0)
		{
			LogInformation("New games: " + JsonConvert.SerializeObject(addedGameIds));
		}
		if (removedGameIds.Count > 0)
		{
			LogInformation("Removed games: " + JsonConvert.SerializeObject(removedGameIds));
		}
		NotifyForNewAndRemovedGameIds(addedGameIds, removedGameIds);
		return addedGameIds.Count + removedGameIds.Count > 0;
	}

	public void NotifyForNewAndRemovedGameIds(List<string> newGameIds, List<string> removedGameIds)
	{
		if (newGameIds.Count > 0)
		{
			List<GamePassGame> newGamePassGames = _pcGamePassApiManager.GetDetailsForGameIdList(newGameIds);
			_discordApiManager.SendAddedGamesMessage(newGamePassGames);
			foreach (GamePassGame gamePassGame in newGamePassGames)
			{
				_gamePassGames.Add(gamePassGame.ProductId, gamePassGame);
			}
		}

		if (removedGameIds.Count > 0)
		{
			LogInformation("Removed games: " + JsonConvert.SerializeObject(newGameIds));
			List<GamePassGame> removedGamePassGames = new();
			foreach (string gameId in removedGameIds)
			{
				if (_gamePassGames.TryGetValue(gameId, out GamePassGame? gamePassGame))
				{
					removedGamePassGames.Add(gamePassGame);
				}
				_gamePassGames.Remove(gameId);
			}
			_discordApiManager.SendRemovedGamesMessage(removedGamePassGames);
		}
	}

	private async Task SerializeGamePassGames()
	{
		try
		{
			PutObjectRequest request = new()
			{
				BucketName = _bucketName,
				Key = s_gamePassGamesFileName,
				ContentBody = JsonConvert.SerializeObject(_gamePassGames, Formatting.Indented)
			};
			var response = await _s3Client.PutObjectAsync(request);
		} catch (Exception exception)
		{
			LogError("The following exception occured while uploading the game pass list to s3: " + exception.Message);
		}

	}

	private async Task DeserializeGamePassGames()
	{
		var response = await _s3Client.GetObjectAsync(_bucketName, s_gamePassGamesFileName);
		StreamReader reader = new(response.ResponseStream);
		string content = reader.ReadToEnd();
		reader.Close();
		var deserializedGamePassGames = JsonConvert.DeserializeObject<Dictionary<string, GamePassGame>>(content);
		if (deserializedGamePassGames != null)
		{
			_gamePassGames = deserializedGamePassGames;
		}
	}
}