using System;
using System.IO;
using System.Net;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace AeroltChatServer
{
	public static class ServerProgram
	{
		public static void Main(string[] args)
		{
			var ip = IPAddress.Any;
			var WebSocketServerPort = 5001;
			var httpServerPort = 5000;

			var connectionString = File.ReadAllText("mongoconnectionstring.txt");
			Database.Init(connectionString);

			var server = new WebSocketServer(WebSocketServerPort) {KeepClean = true};

			server.Log.Level = LogLevel.Trace;
			
			server.AddWebSocketService<Connect>("/Connect");
			server.AddWebSocketService<Message>("/Message");
			server.AddWebSocketService<AssetBundle>("/AssetBundle");

			server.Start();
			
			new HttpServer().Start(httpServerPort);

			Console.WriteLine($"Server Websocket server started on {ip} listening on port {WebSocketServerPort}...");
			Console.WriteLine($"Server HTTP server started on {ip} listening on port {httpServerPort}...");
			Console.WriteLine("Waiting for connections...");
			Console.ReadKey(true);
			
			server.Stop();
		}
		
		
	}
}