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
        private static IMongoCollection<BsonDocument> _bannedUsers;
        private static IMongoCollection<BsonDocument> _users;

        // add array to store current users
        private partial class UserList
        {
            public static List<string> usernames = new List<string>();
            public static List<string> AdminList = new List<string>();
            public static Dictionary<IPAddress, string> UsernameMap = new Dictionary<IPAddress, string>();
            public static Dictionary<IPAddress, WebSocket> EndpointMap = new Dictionary<IPAddress, WebSocket>();

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
                var nameExist = GetUserFromUserName(e.Data) != null;
                if (nameExist) _name = $"{e.Data}#{new Random().Next(1000, 9999)}";
                var newUser = SetNewUser(NewUUID().ToString(), e.Data, Context.UserEndPoint.Address.ToString(), DateTime.Now,
                    TimeSpan.FromMinutes(5), false, false);
                Send(newUser.UUID);
                
                User user;
                if (Guid.TryParse(e.Data, out Guid result)) user = GetUserFromUUID(result);
            }
        }

        private class Usernames : WebSocketBehavior
        {

            protected override void OnMessage(MessageEventArgs e)
            {
                var user = GetUserFromUUID(Guid.Parse(e.Data));
                var usernameCensored = FilterText(user.UserName);
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
            public static void SendToAdmins(string s)
            {
                var us = _users.Database.GetCollection<User>("Users").Find(x => x.IsElevated).ToList();
                foreach (var pair in us.Select(x => UserList.EndpointMap[IPAddress.Parse(x.IpAddress)]))
                    pair.Send(s);
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                var user = GetUserFromUUID(Guid.Parse(e.Data));
                if (user.UUID.Equals(e.Data) && user.IsElevated) UserList.AdminList.Add(user.UUID);
            }
        }

        private class SendMessage : WebSocketBehavior
        {
            public static Regex LinkRegex = new Regex(@"(#\d+)");
            public static Regex CommandRegex = new Regex(@"\$\$(.*) (.*)");
            private static Dictionary<string, Action<IPAddress>> CommandMap = new Dictionary<string, Action<IPAddress>>()
            {
                {
                    //TODO getuser command to check ban status?
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
                var user = GetUserFromIp(Context.UserEndPoint.Address);
                if (user == null)
                    return;
                if (e.Data == null) return;
                if (!user.IsElevated && user.IsBanned)
                    return;
                
                Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + e.Data);

                if (user.IsElevated)
                {
                    var command = CommandRegex.Match(e.Data);
                    if (command.Success && CommandMap.TryGetValue(command.Groups[1].Value, out Action<IPAddress> action))
                    {
                        var users = GetUserFromUserName(command.Groups[2].ToString());
                        //todo thats fucked
                        action(IPAddress.Parse(user.IpAddress));
                        return;
                    }
                }
                
                var text = e.Data;
                if (!user.IsElevated) text = $"<noparse>{FilterText(text.Replace("<noparse>", "").Replace("</noparse>", ""))}</noparse>";
                text = LinkRegex.Replace(text, match => user.IsElevated ? $"<#7f7fe5><u><link=\"{match.Value.Substring(1)}\">Join My Lobby!</link></u></color>" : $"</noparse><#7f7fe5><u><link=\"{match.Value.Substring(1)}\">Join My Lobby!</link></u></color><noparse>");
                
                var prefix = $"[{user.UserName}]";
                if (user.IsElevated) prefix = $"<color=#FFAA00>{prefix}</color>";
                Sessions.Broadcast(prefix + " -> " + text);
            }
            
        }

        //public static bool IncomingRequest(IPAddress user, string msg, out string message)
        //{
        //}
        
        private static string FilterText(string textToFilter)
        {
            var censor = new Censor(ProfanityBase._wordList);
            var censored = censor.CensorText(textToFilter);
            return censored;
        }
        public static bool IsBanned(IPAddress endpoint) => _bannedUsers.Find(new BsonDocument("ip", endpoint.ToString())).Any();
        
        public static void Ban(IPAddress endpoint) => _bannedUsers.InsertOne(new BsonDocument("ip", endpoint.ToString()));
        public static void UnBan(IPAddress endpoint) => _bannedUsers.DeleteOne(new BsonDocument("ip", endpoint.ToString()));
        
        
        public static User GetUserFromUserName(string username)
        {
            var usersCollection = _users.Database.GetCollection<User>("Users").Find(x => x.UserName == username).SingleAsync();
            return usersCollection?.Result;
        }
        public static User GetUserFromUUID(Guid uuid)
        {
            var usersCollection = _users.Database.GetCollection<User>("Users").Find(x => Guid.Parse(x.UUID) == uuid).SingleAsync();
            return usersCollection?.Result;
        }
        public static User GetUserFromIp(IPAddress ipAddress)
        {
            var usersCollection = _users.Database.GetCollection<User>("Users").Find(x => x.IpAddress == ipAddress.ToString()).SingleAsync();
            return usersCollection?.Result;
        }
        
        public static Guid NewUUID()
        {
            var myuuid = Guid.NewGuid();
            return myuuid;
        }
        
        
        public static User SetNewUser(string uuid, string username, string ipAddress, DateTime lastRequest, TimeSpan cooldownTime,bool isElevated, bool isBanned)
        {
            var elevatedUserCard = new BsonDocument { 
                { "UUID", uuid },
                { "UserName", username },
                { "IpAddress", ipAddress },
                { "LastRequest", lastRequest },
                { "CoolDownTime", cooldownTime.ToString() },
                { "IsElevated", isElevated },
                { "IsBanned", isBanned }
            };
            _users.InsertOne(elevatedUserCard);
            return GetUserFromUserName(username);
        }

        private static void RunServer()
        {
            var ip = IPAddress.Any;
            var port = 5000;

            var connectionString = File.ReadAllText("mongoconnectionstring.txt");
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase("AeroltChatServer");
            _bannedUsers = db.GetCollection<BsonDocument>("BannedUsers");
            _users = db.GetCollection<BsonDocument>("Users");

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
