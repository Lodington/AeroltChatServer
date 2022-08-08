using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using WebSocketSharp.Net.WebSockets;

namespace AeroltChatServer
{
    public class UserMeta
    {
        private static readonly Dictionary<IPAddress, UserMeta> Users = new Dictionary<IPAddress, UserMeta>();
        private static Dictionary<Guid, UserMeta> _connectedUsers = new Dictionary<Guid, UserMeta>();
        public static IEnumerable<UserMeta> UsersEnumerator => _connectedUsers.Values;
        public static IEnumerable<UserMeta> AdminsEnumerator => _connectedUsers.Values.Where(x => x.IsElevated);

        public static UserMeta GetOrMakeUser(IPAddress address)
        {
            if (!Users.ContainsKey(address)) Users[address] = new UserMeta();
            return Users[address];
        }
        private UserMeta(){}

        private WebSocketContext _usernameContext;
        public WebSocketContext UsernameContext
        {
            get => _usernameContext;
            set
            {
                _usernameContext?.WebSocket.Close();
                _usernameContext = value;
            }
        }
        
        private WebSocketContext _messageContext;
        public WebSocketContext MessageContext
        {
            get => _messageContext;
            set
            {
                _messageContext?.WebSocket.Close();
                _messageContext = value;
            }
        }
        
        private WebSocketContext _connectContext;
        public WebSocketContext ConnectContext
        {
            get => _connectContext;
            set
            {
                _connectContext?.WebSocket.Close();
                _connectContext = value;
            }
        }

        private Guid _id;

        // TODO interface with db
        public Guid Id
        {
            get => _id;
            set
            {
                _id = value;
                if (_connectedUsers.ContainsKey(_id)) _connectedUsers[_id].Kill();
                _connectedUsers[_id] = this;
            }
        }
        public string Username { get; set; }
        public bool IsElevated { get; set; }

        public void Kill() // Called when any socket is closed;
        {   // this is exactly what Sessions.CloseSession does
            ConnectContext?.WebSocket.Close();
            UsernameContext?.WebSocket.Close();
            MessageContext?.WebSocket.Close();
            _connectedUsers.Remove(Id);
        }
    }

    public class User
    {
        public string UUID { get; set; }
        public string UserName { get; set; }
        public string IpAddress { get; set; }
        public DateTime LastRequest { get; set; }
        public DateTime CoolDownTime { get; set; }
        public bool IsElevated { get; set; }
        public bool IsBanned { get; set; }
    }
}