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

		protected override void OnClose(CloseEventArgs e)
		{
			UserMeta.PopUserFromId(ID)?.Kill();
		}

		public BaseBehaviour()
		{
			Instance = this;
		}

		public static bool IsAlive(string id) => Instance.GetSessions().ActiveIDs.Contains(id);
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
		public static void Close(string id)
		{
			var sessions = Instance.GetSessions();
			if (sessions.ActiveIDs.Any(x => x == id))
				sessions.CloseSession(id);
		}
	}
}