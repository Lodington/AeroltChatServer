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
            public static Dictionary<IPAddress, string> UsernameMap = new Dictionary<IPAddress, string>();
            public static Dictionary<IPAddress, WebSocket> EndpointMap = new Dictionary<IPAddress, WebSocket>();
            public static List<IPAddress> AdminList = new List<IPAddress>();

            public static void CleanDeadUsers()
            {
                foreach (var endPoint in from pair in EndpointMap
                    let endpoint = pair.Key
                    let socket = pair.Value
                    where !socket.IsAlive
                    select endpoint) RemoveUser(endPoint.MapToIPv4());
            }
            public static void RemoveUser(IPAddress x)
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
                UserList.UsernameMap[Context.UserEndPoint.Address] = usernameCensored;
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
        
        private class Admin : WebSocketBehavior
        {
            private string adminKey = File.ReadAllText("elevatedkey.txt");
            
            public static void SendToAdmins(string s)
            {
                foreach (var pair in UserList.EndpointMap.Where(x => UserList.AdminList.Contains(x.Key)))
                {
                    pair.Value.Send(s);
                }
            }
            
            protected override void OnMessage(MessageEventArgs e)
            {
                if (e.Data.Contains(adminKey))
                {
                    UserList.AdminList.Add(Context.UserEndPoint.Address);
                }
            }
        }

        private class SendMessage : WebSocketBehavior
        {
            public static Regex LinkRegex = new Regex(@"(#\d+)");
            public static Regex CommandRegex = new Regex(@"\$\$(.*) (.*)");
            private static Dictionary<string, Action<IPAddress>> CommandMap = new Dictionary<string, Action<IPAddress>>()
            {
                {
                    "ban", endpoint =>
                    {
                        Ban(endpoint);
                        Admin.SendToAdmins($"<color=red><b>User Banned {UserList.UsernameMap[endpoint]}</b></color>");
                    }
                },
                {
                    "unban", endpoint =>
                    {
                        UnBan(endpoint);
                        Admin.SendToAdmins($"<color=yellow><b>User UnBanned {UserList.UsernameMap[endpoint]}</b></color>");
                    }
                }
            };

            

            protected override void OnOpen()
            {
                UserList.EndpointMap[Context.UserEndPoint.Address] = Context.WebSocket;
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                var who = Context.UserEndPoint.Address;
                var elevated = IsElevatedUser(who);
                if (!elevated && IsBanned(who))
                {
                    Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "](Banned: " + Context.UserEndPoint + ")" + e.Data);
                    return;
                }
                
                Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + e.Data);

                if (elevated)
                {
                    var command = CommandRegex.Match(e.Data);
                    if (command.Success && CommandMap.TryGetValue(command.Groups[1].Value, out Action<IPAddress> action))
                    {
                        action(UserList.UsernameMap.FirstOrDefault(x => x.Value.Equals(command.Groups[2].Value)).Key);
                        return;
                    }
                }

                if (e.Data == null) return;
                var text = e.Data;
  
                if (!elevated) text = $"<noparse>{FilterText(text.Replace("<noparse>", "").Replace("</noparse>", ""))}</noparse>";
                text = LinkRegex.Replace(text, match => elevated ? $"<#7f7fe5><u><link=\"{match.Value.Substring(1)}\">Join My Lobby!</link></u></color>" : $"</noparse><#7f7fe5><u><link=\"{match.Value.Substring(1)}\">Join My Lobby!</link></u></color><noparse>");

                if (!UserList.UsernameMap.TryGetValue(who, out var name)) return;
                var prefix = $"[{name}]";
                if (elevated) prefix = $"<color=#FFAA00>{prefix}</color>";
                Sessions.Broadcast(prefix + " -> " + text);
            }
            
        }

        private static string FilterText(string textToFilter)
        {
            var censor = new Censor(ProfanityBase._wordList);
            var censored = censor.CensorText(textToFilter);
            return censored;
        }

        public static bool IsElevatedUser(IPAddress endpoint) => UserList.AdminList.Contains(endpoint);
        public static bool IsBanned(IPAddress endpoint) => bannedUsers.Find(new BsonDocument("ip", endpoint.ToString())).Any();
        public static void Ban(IPAddress endpoint) => bannedUsers.InsertOne(new BsonDocument("ip", endpoint.ToString()));
        public static void UnBan(IPAddress endpoint) => bannedUsers.DeleteOne(new BsonDocument("ip", endpoint.ToString()));


        private static void RunServer()
        {
            var ip = IPAddress.Any;
            var port = 5000;

            var connectionString = File.ReadAllText("mongoconnectionstring.txt");
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase("AeroltChatServer");
            bannedUsers = db.GetCollection<BsonDocument>("BannedUsers");

            var server = new WebSocketServer($"ws://{ip}:{port}");
            
            server.AddWebSocketService<Admin>("/Admin");
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
