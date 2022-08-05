using System;
using System.Collections.Generic;
using System.Net;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace AeroltChatServer
{
    
// State object for reading client data asynchronously  
    class Server
    {
        // add array to store current users
        private partial class UserList
        {
            public static List<string> Usernames = new List<string>();
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
                UserList.Usernames.Add(usernameCensored);
                Sessions.Broadcast(string.Join("\n", UserList.Usernames));
            }

            protected override void OnClose(CloseEventArgs e)
            {
                Sessions.Broadcast(string.Join("\n", UserList.Usernames));
            }
        }

        private class Disconnect : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs e)
            {
                Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + e.Data + " disconnected");
                //Sessions.Broadcast("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + e.Data + " has left.");

                UserList.Usernames.Remove(e.Data);
            }
        }
        private class UserCount : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs e)
            {
                Sessions.Broadcast($"{UserList.Usernames.Count}");
            }
            protected override void OnClose(CloseEventArgs e)
            {
                Sessions.Broadcast($"{UserList.Usernames.Count}");
            }
        }

        private class SendMessage : WebSocketBehavior
        {
            
            protected override void OnMessage(MessageEventArgs e)
            {
                Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + e.Data);
                if (e.Data != null)
                {
                    if (e.Data.Contains("#"))
                    {
                        string[] SplitMsg = e.Data.Split('#');
                       
                        string joinLobbyLink =  string.Format("<#7f7fe5><u><link=\"{0}\">{1}</link></u></color>", SplitMsg[1], $"{SplitMsg[0]} Join My Lobby!");
                        Sessions.Broadcast(joinLobbyLink);
                        return;
                    }
                    var censoredMsg = FilterText(e.Data);
                    string msg= $"<noparse>{censoredMsg}</noparse>";
                    Sessions.Broadcast(msg);
                    
                }
            }
        }

        private static string FilterText(string textToFilter)
        {
            var censor = new Censor(ProfanityBase._wordList);
            var censored = censor.CensorText(textToFilter);
            return censored;
        }
        
        private static void RunServer()
        {
            var ip = IPAddress.Any;
            var port = 5000;
            
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
