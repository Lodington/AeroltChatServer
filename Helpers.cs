namespace AeroltChatServer
{
	public static class Helpers
	{
		private static readonly Censor Censor = new Censor(ProfanityBase._wordList);

		public static string FilterText(string textToFilter)
		{ 
			var censored = Censor.CensorText(textToFilter);
			return censored;
		}
	}
}