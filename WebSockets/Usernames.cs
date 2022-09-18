using System.Linq;

namespace AeroltChatServer
{
	public class Usernames : BaseBehaviour<Usernames>
	{
		public static void BroadcastUserList()
		{
			if (!UserMeta.UsersEnumerator.Any()) return;
			UserMeta.CleanDeadUsers();
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

		protected override void OnOpen()
		{
			base.OnOpen();
			BroadcastUserList();
		}
	}
}