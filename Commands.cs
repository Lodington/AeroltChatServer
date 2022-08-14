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
			{ "unban", Unban },
			{ "elevate", Elevate },
			{ "UU", UpdateUsers },
			{ "admin", Admin }
		};

		private static bool UpdateUsers(UserMeta invoker, UserMeta? target)
		{
			Usernames.Broadcast("Test");
			Message.BroadcastToAdmins($"<color=green>[ Server ]</color> => <color=yellow><b>Sending Test to usernames</b></color>");
			return true;
		}

		private static bool Admin(UserMeta invoker, UserMeta? target)
		{
			if (!invoker.IsAdmin || target is null) return false;
			target.IsAdmin = !target.IsAdmin;
			target.IsElevated |= target.IsAdmin;
			Message.BroadcastToAdmins($"<color=green>[ Server ]</color> => <color=yellow><b>User {target.Username}s admin status has changed to {target.IsAdmin}</b></color>");
			return true;
		}

	
		
		private static bool Elevate(UserMeta invoker, UserMeta? target)
		{
			if (!invoker.IsAdmin || target is null) return false;
			target.IsElevated = !target.IsElevated;
			Message.BroadcastToAdmins($"<color=green>[ Server ]</color> => <color=yellow><b>User {target.Username}s elevation has changed to {target.IsElevated}</b></color>");
			return true;
		}
		
		private static bool Unban(UserMeta invoker, UserMeta? target)
		{
			if (!invoker.IsElevated || target is null || !target.IsBanned) return false;
			target.IsBanned = false;
			Message.BroadcastToAdmins($"<color=green>[ Server ]</color> => <color=yellow><b>User UnBanned {target.Username}</b></color>");
			return true;
		}
		
		private static bool Ban(UserMeta invoker, UserMeta? target)
		{
			if (!invoker.IsElevated || target is null || target.IsBanned) return false;
			target.IsBanned = true;
			Message.BroadcastToAdmins($"<color=green>[ Server ]</color> => <color=red><b>User Banned {target.Username}</b></color>");
			return true;
		}

		public static bool InvokeCommand(string command, UserMeta invoker, UserMeta? target) => _commandMap.TryGetValue(command, out var func) && func(invoker, target);
	}
}