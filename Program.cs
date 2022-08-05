using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace AeroltChatServer
{
    
// State object for reading client data asynchronously  
    class Server
    {
        private static IMongoCollection<BsonDocument> bannedUsers;

        // add array to store current users
        private partial class UserList
        {
            public static Dictionary<IPEndPoint, string> UsernameMap = new Dictionary<IPEndPoint, string>();
            public static Dictionary<IPEndPoint, WebSocket> EndpointMap = new Dictionary<IPEndPoint, WebSocket>();

            public static void CleanDeadUsers()
            {
                foreach (var endPoint in from pair in UserList.EndpointMap
                    let endpoint = pair.Key
                    let socket = pair.Value
                    where !socket.IsAlive
                    select endpoint) RemoveUser(endPoint);
            }
            public static void RemoveUser(IPEndPoint x)
            {
                UsernameMap.Remove(x);
                EndpointMap.Remove(x);
            }
        }

        private class Connect : WebSocketBehavior
        {
            private string _name;

            protected override void OnMessage(MessageEventArgs e)
            {
                Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + e.Data + " connected");
                _name = e.Data;
                Sessions.Broadcast("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + "Welcome " + e.Data +" to the server!");
            }

            protected override void OnClose(CloseEventArgs e)
            {
                //Sessions.Broadcast("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + _name + " has left.");
            }
        }

        private class Usernames : WebSocketBehavior
        {

            protected override void OnMessage(MessageEventArgs e)
            {
                var usernameCensored = FilterText(e.Data);
                UserList.UsernameMap[Context.UserEndPoint] = usernameCensored;
                UserList.EndpointMap[Context.UserEndPoint] = Context.WebSocket;
                Sessions.Broadcast(string.Join("\n", UserList.UsernameMap.Values));
            }

            protected override void OnClose(CloseEventArgs e)
            {
                UserList.CleanDeadUsers();
                Sessions.Broadcast(string.Join("\n", UserList.UsernameMap.Values));
            }
        }

        private class Disconnect : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs e)
            {
                Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + e.Data + " disconnected");
                UserList.CleanDeadUsers();
                Sessions.Broadcast(string.Join("\n", UserList.UsernameMap.Values));
            }
        }
        private class UserCount : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs e)
            {
                Sessions.Broadcast($"{UserList.UsernameMap.Count + 1}");
            }
            protected override void OnClose(CloseEventArgs e)
            {
                Sessions.Broadcast($"{UserList.UsernameMap.Count + 1}");
            }
        }

        private class SendMessage : WebSocketBehavior
        {
            public static Regex LinkRegex = new Regex(@"(#\d+)");

            protected override void OnMessage(MessageEventArgs e)
            {
                Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + e.Data);
                if (e.Data != null)
                {
                    var escapeBullshit = e.Data.Replace("<noparse>", "").Replace("</noparse>", "");
                    var cleaned = $"<noparse>{FilterText(escapeBullshit)}</noparse>";
                    cleaned = LinkRegex.Replace(cleaned,
                        match =>
                            $"</noparse><#7f7fe5><u><link=\"{match.Value.Substring(1)}\">Join My Lobby!</link></u></color><noparse>");
                    Sessions.Broadcast(cleaned);
                }
            }
        }

        private static string FilterText(string textToFilter)
        {
            var censor = new Censor(ProfanityBase._wordList);
            var censored = censor.CensorText(textToFilter);
            return censored;
        }

        public static bool IsBanned(IPEndPoint endpoint) => bannedUsers.Find(new BsonDocument("ip", endpoint.ToString())).Any();
        public static void Ban(IPEndPoint endpoint) => bannedUsers.InsertOne(new BsonDocument("ip", endpoint.ToString()));

        private static void RunServer()
        {
            var ip = IPAddress.Any;
            var port = 5000;

            var connectionString = File.ReadAllText("../../mongoconnectionstring.txt");
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase("AeroltChatServer");
            bannedUsers = db.GetCollection<BsonDocument>("BannedUsers");

            var server = new WebSocketServer($"ws://{ip}:{port}");
            server.AddWebSocketService<SendMessage>("/Message");
            server.AddWebSocketService<Usernames>("/Usernames");
            server.AddWebSocketService<Disconnect>("/Disconnect");
            server.AddWebSocketService<UserCount>("/UserCount");
            server.AddWebSocketService<Connect>("/Connect");
            
            server.Start();
            Console.WriteLine($"Server started on {ip} listening on port {port}...");
            Console.WriteLine("Waiting for connections...");
            
            Console.ReadKey(true);
        }

        public static void Main(string[] args)
        {
            RunServer();
        }
    }
}
