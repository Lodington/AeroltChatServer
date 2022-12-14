using AeroltChatServer.Censorship;

namespace AeroltChatServer
{
	public static class Helpers
	{
		private static readonly Censor Censor = new Censor(ProfanityBase._wordList);

		public static string FilterText(string textToFilter) => Censor.CensorText(textToFilter);
	}
}