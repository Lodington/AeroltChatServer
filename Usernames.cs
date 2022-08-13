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
			var message = string.Join("\n", users.OrderBy(x => x.IsElevated && x.IsAdmin ? 2 : x.IsElevated ? 1 : 0).Select(x =>
			{
				var prefix = $"<link={x.Username}>{x.Username}</link>";
				if (x.IsAdmin) prefix = $"<color=#FFAA00>{prefix}</color>";
				if (x.IsElevated) prefix = $"<color=#08a2f7>{prefix}</color>";
				return prefix;
			}));
			Broadcast(message);
		}
	}
}