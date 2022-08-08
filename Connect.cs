using System;
using WebSocketSharp;

namespace AeroltChatServer
{
	public class Connect : BaseBehaviour<Connect>
	{
		protected override void OnOpen()
		{
			UserMeta.GetOrMakeUser(Context.UserEndPoint.Address).ConnectId = ID;
		}

		protected override void OnMessage(MessageEventArgs e)
		{
			if (e.Data.IsNullOrEmpty())
			{
				UserMeta.GetOrMakeUser(Context.UserEndPoint.Address).Kill();
				return;
			}
			var user = UserMeta.GetOrMakeUser(Context.UserEndPoint.Address);
			if (!Guid.TryParse(e.Data, out var guid))
			{
				var rootName = Helpers.FilterText(e.Data);
				var userName = rootName;
				var rand = new Random();
				while (Database.ContainsUsername(userName))
				{
					userName = rootName + "#" + rand.Next(1000, 9999);
				}

				guid = Guid.NewGuid();
				user.Username = userName;
			}
			user.Id = guid;
			//if (!user.IsElevated && user.IsBanned) user.Kill(); disallow banned people to connect?
			Send(guid.ToString());
		}
	}
}