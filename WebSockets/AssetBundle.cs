using System;
using System.IO;
using AeroltChatServer.Data;
using WebSocketSharp;

namespace AeroltChatServer.WebSockets
{
    public class AssetBundle : BaseBehaviour<AssetBundle>
    {
        private string urlToSend = "aerolt.lodington.dev:5000/";
        
        protected override void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
            if (e.Data.IsNullOrEmpty()) return;
            FileInfo fi = new FileInfo("aeroltbundle");
            if(Database.GetUsername(Guid.Parse(e.Data)) != null)
                Send($"{urlToSend}/{e.Data}");
        }
    }
}