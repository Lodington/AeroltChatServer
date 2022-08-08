#nullable enable
using System;
using System.Collections.Generic;

namespace AeroltChatServer
{
	public static class Commands
	{
		private static Dictionary<string, Func<UserMeta, UserMeta?, bool>> _commandMap = new Dictionary<string, Func<UserMeta, UserMeta?, bool>>
		{
			{ "ban", Ban },
			{ "unban", Unban }
		};

		private static bool Unban(UserMeta invoker, UserMeta? target)
		{
			if (!invoker.IsElevated || target is null || !target.IsBanned) return false;
			target.IsBanned = false;
			Message.BroadcastToAdmins($"<color=yellow><b>User UnBanned {target.Username}</b></color>");
			return true;
		}
		
		private static bool Ban(UserMeta invoker, UserMeta? target)
		{
			if (!invoker.IsElevated || target is null || target.IsBanned) return false;
			target.IsBanned = true;
			Message.BroadcastToAdmins($"<color=red><b>User Banned {target.Username}</b></color>");
			return true;
		}

		public static bool InvokeCommand(string command, UserMeta invoker, UserMeta? target) => _commandMap.TryGetValue(command, out var func) && func(invoker, target);
	}
}