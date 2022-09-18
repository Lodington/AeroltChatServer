using System;
using System.IO;
using System.Net;
using System.Web;
using AeroltChatServer.Data;

namespace AeroltChatServer
{
    public class HttpServer
    {
        private HttpListener _listener;

        public void Start(int port)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port.ToString()}/");
            _listener.Start();
            Receive();
        }

        public void Stop() => _listener.Stop();
        private void Receive() => _listener.BeginGetContext(ListenerCallback, _listener);

        private void ListenerCallback(IAsyncResult result)
        {
            if (_listener.IsListening)
            {
                var context = _listener.EndGetContext(result);
                var request = context.Request;

                var guidFromUrl = request.Url.AbsolutePath.Split('/');
                Console.WriteLine(guidFromUrl[1]);
                if (Guid.TryParse(guidFromUrl[1], out var guid))
                { 
                    var username = Database.GetUsername(guid);
                    if (!string.IsNullOrEmpty(username)) SendBundle(context);
                }
                Receive();
            }
        }
        private void SendBundle(HttpListenerContext ctx)
        {
            using HttpListenerResponse resp = ctx.Response;
            
            byte[] buf = File.ReadAllBytes("public/aeroltbundle");
            resp.ContentLength64 = buf.Length;
            using Stream ros = resp.OutputStream;
            ros.Write(buf, 0, buf.Length);
        }
    }
}