﻿#nullable enable
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
			var isBanned = user?.IsBanned ?? false ? " (Banned)" : "";
			var isElevated = user?.IsElevated ?? false ? " *" : "";
			Console.WriteLine($"[{DateTime.Now:HH:mm:ss}]{isBanned}{isElevated} {user?.Username ?? "Unknown"} -> {e.Data}");
			if (user is null || !user.IsElevated && user.IsBanned) return;

			var command = CommandRegex.Match(e.Data);
			if (command.Success)
			{
				UserMeta? who = null;
				if (command.Groups.Count > 2)
					who = UserMeta.UsersEnumerator.FirstOrDefault(x =>
					{
						var target = command.Groups[2].ToString();
						return !target.IsNullOrEmpty() && x.Username.Contains(target);
					});
				Commands.InvokeCommand(command.Groups[1].Value.ToLower(), user, who);
				return;
			}
                
			var text = e.Data;
			if (!user.IsElevated) text = FilterText(text).StripTextMeshProFormatting();
			text = LinkRegex.Replace(text, match => match.Value.Substring(1).MarkLink("Join My Lobby!", user.IsElevated));

			Sessions.Broadcast($"[{user.GetDressedUsername()}] -> " + text);
		}
		
		protected override void OnOpen()
		{
			UserMeta.AddMessageId(Context.UserEndPoint.Address, ID);
		}
	}
}