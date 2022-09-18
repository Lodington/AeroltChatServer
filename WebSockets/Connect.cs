using System;
using AeroltChatServer.Data;
using WebSocketSharp;

namespace AeroltChatServer.WebSockets
{
	public class Connect : BaseBehaviour<Connect>
	{
		protected override void OnMessage(MessageEventArgs e)
		{
			base.OnMessage(e);
			if (e.Data.IsNullOrEmpty())
			{
				Console.WriteLine("Users connected but with null UUID or Name");
				return;
			}

			var userName = "";
			if (!Guid.TryParse(e.Data, out var guid))
			{
				var rootName = Helpers.FilterText(e.Data).Trim();
				userName = rootName;
				var rand = new Random();
				while (Database.ContainsUsername(userName))
				{
					userName = rootName + "#" + rand.Next(1000, 9999);
				}

				guid = Guid.NewGuid();
			}

			var user = UserMeta.GetOrCreateUserFromGuid(guid);
			if (!string.IsNullOrWhiteSpace(userName)) user.Username = userName;
			//if (!user.IsElevated && user.IsBanned) user.Kill(); disallow banned people to connect?
			Send(guid.ToString());
		}
	}
}