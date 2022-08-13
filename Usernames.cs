using System;
using System.Linq;
using WebSocketSharp;

namespace AeroltChatServer
{
	public class Usernames : BaseBehaviour<Usernames>
	{
		protected override void OnOpen()
		{
			UserMeta.AddUsernamesId(Context.UserEndPoint.Address, ID);
		}

		public static void BroadcastUserList()
		{
			if (!UserMeta.UsersEnumerator.Any()) return;
			var users = UserMeta.UsersEnumerator.ToArray();
			var message = string.Join("\n", users.Select(x => x.Username));
			Broadcast(message);
		}
	}
}