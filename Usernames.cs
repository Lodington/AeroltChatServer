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
			BroadcastUserList();
		}

		protected override void OnClose(CloseEventArgs e)
		{
			var user = UserMeta.PopUserFromId(ID);
			UserMeta.Users.Remove(user);
			BroadcastUserList();
		}

		public static void BroadcastUserList()
		{
			if (!UserMeta.UsersEnumerator.Any()) return;
			var users = UserMeta.UsersEnumerator.ToArray();
			var message = string.Join("\n", users.OrderByDescending(x => x.IsElevated && x.IsAdmin ? 2 : x.IsElevated ? 1 : 0).Select(x => $"<link={x.Username.StripTextMeshProFormatting()}>{x.GetDressedUsername()}</link>").Distinct());
			Broadcast(message);
		}
	}
}