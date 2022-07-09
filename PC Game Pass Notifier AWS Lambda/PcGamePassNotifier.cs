using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using System.Diagnostics.CodeAnalysis;
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
	private readonly string _bucketName;

	public static HttpClient HttpClient => s_httpClient;

	[MemberNotNull(nameof(s_lambdaContext))]
	public static async Task InitializeLambdaCall(Dictionary<string, string> inputJsonDictionary, ILambdaContext context)
	{
		s_lambdaContext = context;
		await new PcGamePassNotifier(inputJsonDictionary).InitializeAndUpdateGamePassGames();
		await DeleteEmptyLogStreams();
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

		var pcGamePassAllGamesCollectionUrl = inputJsonDictionary.GetValueForKey("allGamesUrl");
		var pcGamePassConsoleGamesCollectionUrl = inputJsonDictionary.GetValueForKey("consoleGamesUrl");
		var pcGamePassDetailsUrlPattern = inputJsonDictionary.GetValueForKey("detailUrlPattern");

		_pcGamePassApiManager = new GamePassApiManager(pcGamePassAllGamesCollectionUrl, pcGamePassConsoleGamesCollectionUrl, pcGamePassDetailsUrlPattern);
		_discordApiManager = new DiscordApiManager(inputJsonDictionary.GetValueForKey("discordWebhookUrl"));
		_gamePassGames = new Dictionary<string, GamePassGame>();
		_s3Client = new AmazonS3Client(Amazon.RegionEndpoint.EUCentral1);
		_bucketName = inputJsonDictionary.GetValueForKey("bucketName");
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

	private static async Task DeleteEmptyLogStreams()
	{
		if (s_lambdaContext is null)
		{
			return;
		}
		var cloudWatchLogsClient = new AmazonCloudWatchLogsClient(Amazon.RegionEndpoint.EUCentral1);
		var logGroupsRequest = new DescribeLogGroupsRequest()
		{
			LogGroupNamePrefix = s_lambdaContext.LogGroupName
		};
		DescribeLogGroupsResponse logGroupsResponse = await cloudWatchLogsClient.DescribeLogGroupsAsync(logGroupsRequest);
		if (logGroupsResponse.LogGroups != null && logGroupsResponse.LogGroups.Count > 0)
		{
			LogGroup logGroup = logGroupsResponse.LogGroups.First();
			var describeLogStreamsRequest = new DescribeLogStreamsRequest(s_lambdaContext.LogGroupName);
			DescribeLogStreamsResponse response = await cloudWatchLogsClient.DescribeLogStreamsAsync(describeLogStreamsRequest);
			List<LogStream> logStreamsToDelete = new();
			foreach (LogStream logStream in response.LogStreams)
			{
				if ((DateTime.UtcNow - logStream.LastIngestionTime).TotalMilliseconds > (logGroup.RetentionInDays * 86400000)) // 1 day in milliseconds
				{
					logStreamsToDelete.Add(logStream);
					LogInformation($"Added LogStream {logStream.LogStreamName} to deletion list");
				}
			}
			LogInformation($"Start deletion of {logStreamsToDelete.Count} LogStreams");
			int numberOfFailedDeletions = 0;
			try
			{
				List<Task> deleteTasks = new();
				foreach (LogStream logStream in logStreamsToDelete)
				{
					var deleteLogStreamRequest = new DeleteLogStreamRequest()
					{
						LogGroupName = s_lambdaContext.LogGroupName,
						LogStreamName = logStream.LogStreamName
					};
					deleteTasks.Add(cloudWatchLogsClient.DeleteLogStreamAsync(deleteLogStreamRequest));
				}
				Task.WaitAll(deleteTasks.ToArray());
			} catch (AggregateException exception)
			{
				foreach (var innerException in exception.InnerExceptions)
				{
					LogError($"Caught exception during deletion: " + innerException.Message);
					numberOfFailedDeletions++;
				}
			}
			LogInformation($"{logStreamsToDelete.Count - numberOfFailedDeletions} LogStreams deleted");
		}
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
		bool hasUpdates = FindAddedAndRemovedGamesFromGameList(gameIds, out List<string> addedGameIds, out List<string> removedGameIds);
		NotifyForNewAndRemovedGameIds(addedGameIds, removedGameIds);
		return hasUpdates;
	}

	public bool FindAddedAndRemovedGamesFromGameList(List<string> newIds, out List<string> addedGameIds, out List<string> removedGameIds)
	{
		List<string> gamePassGameIds = _gamePassGames.Keys.ToList();
		addedGameIds = newIds.Except(gamePassGameIds).ToList();
		removedGameIds = gamePassGameIds.Except(newIds).ToList();
		LogInformation($"Found {addedGameIds.Count} new {(addedGameIds.Count == 1 ? "game" : "games")} and {removedGameIds.Count} removed {(removedGameIds.Count == 1 ? "game" : "games")}.");
		if (addedGameIds.Count > 0)
		{
			LogInformation("New games: " + JsonConvert.SerializeObject(addedGameIds));
		}
		if (removedGameIds.Count > 0)
		{
			LogInformation("Removed games: " + JsonConvert.SerializeObject(removedGameIds));
		}
		return addedGameIds.Count + removedGameIds.Count > 0;
	}

	public void NotifyForNewAndRemovedGameIds(List<string> newGameIds, List<string> removedGameIds)
	{
		if (newGameIds.Count > 0)
		{
			string gameListDetailsJsonString = _pcGamePassApiManager.GetDetailsForGameIdList(newGameIds);
			List<GamePassGame> newGamePassGames = _pcGamePassApiManager.CreateGamePassGamesFromJsonString(gameListDetailsJsonString);
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