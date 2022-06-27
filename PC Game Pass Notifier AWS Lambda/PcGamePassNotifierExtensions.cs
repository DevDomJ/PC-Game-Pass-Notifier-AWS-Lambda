using Newtonsoft.Json;

namespace PC_Game_Pass_Notifier_AWS_Lambda
{
	internal static class PcGamePassNotifierExtensions
	{
		/// <summary>
		/// Returns the value for key <paramref name="key"/>, just like Dictionary&lt;<typeparamref name="TKey"/>, <typeparamref name="TValue"/>&gt;[<paramref name="key"/>],
		/// however, actually throws a useful KeyNotFoundException, which prints the key and content of the dictionary in the message as well.
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="dictionary"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		/// <exception cref="KeyNotFoundException"></exception>
		public static TValue GetValueForKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull // C# is fucking weird...
		{
			if (!dictionary.TryGetValue(key, out var value))
			{
				throw new KeyNotFoundException($"Key '{key}' not found in input json: " + JsonConvert.SerializeObject(dictionary));
			};
			return value;
		}
	}
}
