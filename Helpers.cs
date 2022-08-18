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

		public static string StripTextMeshProFormatting(this string text)
		{
			return $"<noparse>{text.Replace("<noparse>", "").Replace("</noparse>", "")}</noparse>";
		}

		public static string MarkLink(this string link, string message = null, bool skipNoParse = false)
		{
			message ??= link;
			link = $"<#7f7fe5><u><link=\"{link}\">{message}</link></u></color>";
			if (!skipNoParse) link = link.StripTextMeshProFormatting();
			return link;
		}
	}
}