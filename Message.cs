using WebSocketSharp;
using WebSocketSharp.Server;

namespace AeroltChatServer
{
	public class Message : WebSocketBehavior
	{
		private static Message _instance;

		public Message()
		{
			_instance = this;
		}
		
		public static void Broadcast(string message)
		{
			_instance.Sessions.Broadcast(message);
		}
		
		public static void BroadcastToAdmins(string message)
		{
			foreach (var userMeta in UserMeta.AdminsEnumerator) userMeta.MessageContext.WebSocket.Send(message);
		}
		
		protected override void OnOpen()
		{
			UserMeta.GetOrMakeUser(Context.UserEndPoint.Address).MessageContext = Context;
		}
		
		protected override void OnClose(CloseEventArgs e)
		{
			UserMeta.GetOrMakeUser(Context.UserEndPoint.Address).Kill();
		}
	}
}