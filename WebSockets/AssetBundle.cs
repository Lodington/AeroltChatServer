using System;
using System.IO;
using WebSocketSharp;

namespace AeroltChatServer
{
    public class AssetBundle : BaseBehaviour<AssetBundle>
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
            if (e.Data.IsNullOrEmpty()) return;
            FileInfo fi = new FileInfo("aeroltbundle");
            if(Database.GetUsername(Guid.Parse(e.Data)) != null)
                Send($"aerolt.lodington.dev:5000/{e.Data}");
        }

        private void IsComplete(bool complete)
        {
            if(complete)
                Close();
        }
    }
}