#nullable enable
using System;
using System.Linq;
using System.Text.RegularExpressions;
using AeroltChatServer.Data;
using WebSocketSharp;
using static AeroltChatServer.Helpers;

namespace AeroltChatServer.WebSockets
{
	public class Message : BaseBehaviour<Message>
	{
		public static void BroadcastToAdmins(string message)
		{
			var admins = UserMeta.AdminsEnumerator.Select(x => x.Id);
			foreach (var userMeta in Instance.GetSessions().Sessions.Where(x => admins.Contains(x.Context.WebSocket.guid))) userMeta.Context.WebSocket.Send(message);
		}
		
		public static Regex LinkRegex = new Regex(@"(#\d+)");
		public static Regex CommandRegex = new Regex(@"\$\$(\w+) ?(.*)"); // Commands cannot have non-word characters in them, like @
		protected override void OnMessage(MessageEventArgs e)
		{
			base.OnMessage(e);
			if (e.Data == null) return;
			var user = UserMeta.GetOrCreateUserFromGuid(Context.WebSocket.guid);
			var isBanned = user?.IsBanned ?? false ? " (Banned)" : "";
			var isElevated = user?.IsElevated ?? false ? " *" : "";
			Console.WriteLine($"[{DateTime.Now:HH:mm:ss}]{isBanned}{isElevated} {user?.Username ?? "Unknown"} -> {e.Data}");
			if (user is null || !user.IsElevated && user.IsBanned) return;

			var command = CommandRegex.Match(e.Data);
			if (command.Success)
			{
				UserMeta? who = null;
				if (command.Groups.Count > 2)
					who = UserMeta.UsersEnumerator.FirstOrDefault(x => x.Username == command.Groups[2].ToString());
				Commands.InvokeCommand(command.Groups[1].Value.ToLower(), user, who);
				return;
			}
                
			var text = e.Data;
			if (!user.IsElevated) text = $"<noparse>{FilterText(text.Replace("<noparse>", "").Replace("</noparse>", ""))}</noparse>";
			text = LinkRegex.Replace(text, match => user.IsElevated ? $"<#7f7fe5><u><link=\"{match.Value.Substring(1)}\">Join My Lobby!</link></u></color>" : $"</noparse><#7f7fe5><u><link=\"{match.Value.Substring(1)}\">Join My Lobby!</link></u></color><noparse>");
                
			var prefix = $"[{user.Username}]";
			if (user.IsAdmin) prefix = $"<color=#FFAA00>{prefix}</color>";
			if (user.IsElevated) prefix = $"<color=#08a2f7>{prefix}</color>";
			
			Sessions.Broadcast(prefix + " -> " + text);
		}
	}
}