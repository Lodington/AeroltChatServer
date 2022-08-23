using System;
using System.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace AeroltChatServer
{
	public class BaseBehaviour : WebSocketBehavior
	{
		public WebSocketSessionManager GetSessions() => Sessions; // Dumbass workaround for a protected property?
	}
	public class BaseBehaviour<T> : BaseBehaviour where T : BaseBehaviour
	{
		public static BaseBehaviour<T> Instance;

		protected override void OnMessage(MessageEventArgs e)
		{
			base.OnMessage(e);
			if (e.IsPong && Guid.TryParse(e.Data, out var id)) Context.WebSocket.guid = id;
		}

		public BaseBehaviour()
		{
			Instance = this;
			EmitOnPong = true;
		}

		public static bool IsAlive(Guid id)
		{
			var sessions = Instance.GetSessions();
			return sessions.ActiveIDs.Any(x => sessions.TryGetSession(x, out var session) && session.Context.WebSocket.guid == id);
		}

		[Obsolete]
		public static void SendTo(string id, string message)
		{
			if (Instance.GetSessions().TryGetSession(id, out var inst))
			{
				inst.Context.WebSocket.Send(message);
			}
		}
		public static void Broadcast(string message)
		{
			Instance.GetSessions().Broadcast(message);
		}
		[Obsolete]
		public static void Close(string id)
		{
			var sessions = Instance.GetSessions();
			if (sessions.ActiveIDs.Any(x => x == id))
				sessions.CloseSession(id);
		}
	}
}