#nullable enable
using System;
using System.Linq;
using System.Text.RegularExpressions;
using WebSocketSharp;
using static AeroltChatServer.Helpers;

namespace AeroltChatServer
{
	public class Message : BaseBehaviour<Message>
	{
		public static void BroadcastToAdmins(string message)
		{
			var admins = UserMeta.AdminsEnumerator.Select(x => x.MessageId); ;
			foreach (var userMeta in Instance.GetSessions().Sessions.Where(x => admins.Contains(x.ID))) userMeta.Context.WebSocket.Send(message);
		}
		
		public static Regex LinkRegex = new Regex(@"(#\d+)");
		public static Regex CommandRegex = new Regex(@"\$\$(\w+) ?(.*)"); // Commands cannot have non-word characters in them, like @
		protected override void OnMessage(MessageEventArgs e)
		{
			if (e.Data == null) return;
			var user = UserMeta.GetUserFromSocketId(ID);
			if (!user.IsElevated && user.IsBanned) return;
                
			Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + user.Username + " -> " + e.Data);

			var command = CommandRegex.Match(e.Data);
			if (command.Success)
			{
				UserMeta? who = null;
				if (command.Groups.Count > 2)
					who = UserMeta.UsersEnumerator.FirstOrDefault(x => x.Username == command.Groups[2].ToString());
				Commands.InvokeCommand(command.Groups[1].Value, user, who);
				return;
			}
                
			var text = e.Data;
			if (!user.IsElevated) text = $"<noparse>{FilterText(text.Replace("<noparse>", "").Replace("</noparse>", ""))}</noparse>";
			text = LinkRegex.Replace(text, match => user.IsElevated ? $"<#7f7fe5><u><link=\"{match.Value.Substring(1)}\">Join My Lobby!</link></u></color>" : $"</noparse><#7f7fe5><u><link=\"{match.Value.Substring(1)}\">Join My Lobby!</link></u></color><noparse>");
                
			var prefix = $"[{user.Username}]";
			if (user.IsElevated) prefix = $"<color=#FFAA00>{prefix}</color>";
			Sessions.Broadcast(prefix + " -> " + text);
		}
		
		protected override void OnOpen()
		{
			UserMeta.AddMessageId(Context.UserEndPoint.Address, ID);
		}
	}
}