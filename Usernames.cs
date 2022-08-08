using System.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace AeroltChatServer
{
	public class Usernames : WebSocketBehavior
	{
		private static Usernames _instance;

		public Usernames()
		{
			_instance = this;
		}

		public static void Broadcast(string message)
		{
			_instance.Sessions.Broadcast(message);
		}

		protected override void OnOpen()
		{
			UserMeta.GetOrMakeUser(Context.UserEndPoint.Address).UsernameContext = Context;
		}
		
		protected override void OnClose(CloseEventArgs e)
		{
			UserMeta.GetOrMakeUser(Context.UserEndPoint.Address).Kill();
		}

		public static void BroadcastUserlist()
		{
			var message = string.Join("\n", UserMeta.UsersEnumerator.Select(x => x.Username));
			_instance.Sessions.Broadcast(message);
		}
	}
}