using System;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace AeroltChatServer
{
	public class Connect : WebSocketBehavior
	{
		private static Connect _instance;

		public Connect()
		{
			_instance = this;
		}

		public static void Broadcast(string message)
		{
			_instance.Sessions.Broadcast(message);
		}
		
		protected override void OnOpen()
		{
			UserMeta.GetOrMakeUser(Context.UserEndPoint.Address).ConnectContext = Context;
		}

		protected override void OnClose(CloseEventArgs e)
		{
			UserMeta.GetOrMakeUser(Context.UserEndPoint.Address).Kill();
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
			Send(guid.ToString());
		}
	}
}