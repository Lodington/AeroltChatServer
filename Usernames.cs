using System;
using System.Linq;
using WebSocketSharp;

namespace AeroltChatServer
{
	public class Usernames : BaseBehaviour<Usernames>
	{
		protected override void OnOpen()
		{
			//todo Dispose of endpoint properly? https://stackoverflow.com/questions/29944233/system-objectdisposedexception-throwed-on-websocket-communication
			//todo this too https://stackoverflow.com/questions/4812686/closing-websocket-correctly-html5-javascript
			UserMeta.AddUsernamesId(Context.UserEndPoint.Address, ID);
		}

		public static void BroadcastUserList()
		{
			if (!UserMeta.UsersEnumerator.Any()) return;
			var users = UserMeta.UsersEnumerator.ToArray();
			var message = string.Join("\n", users.OrderByDescending(x => x.IsElevated && x.IsAdmin ? 2 : x.IsElevated ? 1 : 0).Select(x =>
			{
				var prefix = $"<link={x.Username}>{x.Username}</link>";
				if (x.IsAdmin) prefix = $"<color=#FFAA00>{prefix}</color>";
				if (x.IsElevated) prefix = $"<color=#08a2f7>{prefix}</color>";
				return prefix;
			}).Distinct());
			Broadcast(message);
		}
	}
}