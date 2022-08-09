using System;
using System.Linq;
using WebSocketSharp;

namespace AeroltChatServer
{
	public class Usernames : BaseBehaviour<Usernames>
	{
		protected override void OnOpen()
		{
			UserMeta.GetOrMakeUser(Context.UserEndPoint.Address).UsernameId = ID;
			BroadcastUserList(); // I worry about the users username not being added yet when broadcasting here.
		}
		
		protected override void OnClose(CloseEventArgs e)
		{
			base.OnClose(e);
			BroadcastUserList();
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