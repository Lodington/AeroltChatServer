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
			var message = string.Join("\n", users.OrderByDescending(x => x.IsElevated && x.IsAdmin ? 2 : x.IsElevated ? 1 : 0).Select(x => $"<link={x.Username.StripTextMeshProFormatting()}>{x.GetDressedUsername()}</link>").Distinct());
			Broadcast(message);
		}

		protected override void OnOpen()
		{
			base.OnOpen();
			BroadcastUserList();
		}
	}
}