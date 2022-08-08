using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using WebSocketSharp.Server;

namespace AeroltChatServer
{
	public static class ServerProgram
	{
		public static void Main(string[] args)
		{
			var ip = IPAddress.Any;
			var port = 5000;

			var connectionString = File.ReadAllText("mongoconnectionstring.txt");
			Database.Init(connectionString);

			var server = new WebSocketServer($"ws://{ip}:{port}");

			server.AddWebSocketService<Message>("/Message");
			server.AddWebSocketService<Usernames>("/Usernames");
			server.AddWebSocketService<Connect>("/Connect");
            
			server.Start();
			Console.WriteLine($"Server started on {ip} listening on port {port}...");
			Console.WriteLine("Waiting for connections...");
            
			Console.ReadKey(true);
		}
	}
}