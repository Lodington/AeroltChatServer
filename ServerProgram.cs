using System;
using System.IO;
using System.Net;
using AeroltChatServer.Data;
using AeroltChatServer.WebSockets;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace AeroltChatServer
{
	public static class ServerProgram
	{
		public static void Main(string[] args)
		{
			var ip = IPAddress.Any;
			var httpServerPort = 5000;
			var WebSocketServerPort = 5001;
			
			Database.Init(InitDatabase("mongoconnectionstring.txt"));

			var server = new WebSocketServer(WebSocketServerPort) {KeepClean = true};
			server.AddWebSocketService<Connect>("/Connect");
			server.AddWebSocketService<Message>("/Message");
			server.AddWebSocketService<AssetBundle>("/AssetBundle");
			server.Log.Level = LogLevel.Info;
			server.Start();
			
			new HttpServer().Start("localhost","httpServerPort");

			Console.WriteLine($"Server Websocket server started on {ip} listening on port {WebSocketServerPort}...");
			Console.WriteLine($"Server HTTP server started on {ip} listening on port {httpServerPort}...");
			Console.WriteLine("Waiting for connections...");
			Console.ReadKey(true);
			
			server.Stop();
		}

		public static string InitDatabase(string path)
		{
			if (!File.Exists(path))
			{
				Console.WriteLine("Please input your Mongo Connection string");
				var input = Console.ReadLine();
				if (string.IsNullOrEmpty(input))
					throw new NullReferenceException("Cant have a null or empty connection string");
				return input;
			}
			return File.ReadAllText(path);
		}
		
	}
}