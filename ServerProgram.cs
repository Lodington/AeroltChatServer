using System;
using System.Diagnostics;
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
		private static readonly IPAddress Ip = IPAddress.Any;
		private const string HttpServerPort = "5000";
		private const string WebSocketServerPort = "5001";
		private static readonly WebSocketServer WebSocketServer = new WebSocketServer(WebSocketServerPort) {KeepClean = true};
		private static readonly HttpServer HttpServer = new HttpServer();
		
		public static void Main(string[] args)
		{
			InitDatabase("mongoconnectionstring.txt");
			SetupWebSocketServer();
			HttpServer.Start("localhost",HttpServerPort);

			Console.WriteLine($"Server Websocket server started on {Ip} listening on port {WebSocketServerPort}...");
			Console.WriteLine($"Server HTTP server started on {Ip} listening on port {HttpServerPort}...");
			Console.WriteLine("Waiting for connections...");

			while (true)
			{
				var input = Console.ReadLine();
				CheckCommand(input);
			}
		}

		private static void CheckCommand(string command)
		{
			if(string.IsNullOrEmpty(command))
				return;
			switch (command.ToLowerInvariant())
			{
				case "stop" :
					WebSocketServer.Stop();
					HttpServer.Stop();
					break;
				case "start" :
					WebSocketServer.Start();
					HttpServer.Start("localhost",HttpServerPort);
					break;
			}
		}

		private static void SetupWebSocketServer()
		{
			WebSocketServer.AddWebSocketService<Connect>("/Connect");
			WebSocketServer.AddWebSocketService<Message>("/Message");
			WebSocketServer.AddWebSocketService<AssetBundle>("/AssetBundle");
			WebSocketServer.Log.Level = LogLevel.Info;
			WebSocketServer.Start();
		}
		
		private static void InitDatabase(string path)
		{
			if (!File.Exists(path))
			{
				Console.WriteLine("Please input your Mongo Connection string");
				var input = Console.ReadLine();
				if (string.IsNullOrEmpty(input))
					throw new NullReferenceException("Cant have a null or empty connection string");
				Database.Init(path);
			}
			Database.Init(path);
		}
		
	}
}