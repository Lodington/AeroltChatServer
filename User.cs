#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using MongoDB.Bson;
using WebSocketSharp;

namespace AeroltChatServer
{
    public class UserMeta
    {
        private static readonly List<UserMeta> Users = new List<UserMeta>();

        private static readonly ConcurrentDictionary<IPAddress, string> messageIDQueue = new ConcurrentDictionary<IPAddress, string>();
        private static readonly ConcurrentDictionary<IPAddress, string> usernamesIDQueue = new ConcurrentDictionary<IPAddress, string>();
        
        private static readonly ConcurrentDictionary<string, UserMeta> IdMap = new ConcurrentDictionary<string, UserMeta>();

        public static IEnumerable<UserMeta> UsersEnumerator => Users.ToArray();
        public static IEnumerable<UserMeta> AdminsEnumerator => UsersEnumerator.Where(x => x.IsElevated);

        // Used to pair a connection together, in any ws connection order. Sockets can get mixed up if multiple connections happen too fast from the same ip.
        #region UserCreation
        public static void CreateUser(Guid guid, IPAddress address, string connectId, string userName)
        {
            var user = new UserMeta(guid, address, connectId, userName);
            if (messageIDQueue.ContainsKey(address) && messageIDQueue.TryRemove(address, out var id)) user.MessageId = id;
            if (usernamesIDQueue.ContainsKey(address) && usernamesIDQueue.TryRemove(address, out var id2)) user.UsernameId = id2;
            if (user.KillInvalidUser()) return;
            PruneDuplicateGuids(guid, address);
            if (!user.UsernameId.IsNullOrEmpty()) Usernames.BroadcastUserList();
        }

        private bool KillInvalidUser()
        {
            if (string.IsNullOrEmpty(Username))
            { 
                Connect.SendTo(ConnectId, "Invalid Username");
                Kill(); // Kill connections that have a valid guid but didnt supply a username.
                Database.DropGuid(_id);
                return true;
            }

            return false;
        }

        public static void AddMessageId(IPAddress address, string messageId)
        {
            var user = Users.FirstOrDefault(x => Equals(x.Address, address) && x.MessageId == null);
            if (user != null)
                user.MessageId = messageId;
            else
                messageIDQueue.TryAdd(address, messageId);
        }
        public static void AddUsernamesId(IPAddress address, string usernameId)
        {
            var user = Users.FirstOrDefault(x => Equals(x.Address, address) && x.UsernameId == null);
            if (user != null)
            {
                user.UsernameId = usernameId;
                Usernames.BroadcastUserList();
            }
            else
                usernamesIDQueue.TryAdd(address, usernameId);
        }
        #endregion

        public static UserMeta? GetUserFromSocketId(string id)
        {
            IdMap.TryGetValue(id, out var val);
            return val;
        }
        public static void PruneDuplicateGuids(Guid guid, IPAddress address)
        {
            return;
            //TODO fix this shit: throws collection was modified
            foreach (var userMeta in Users.Where(x => x.Id == guid && !Equals(x.Address, address))) userMeta.Kill();
        }
        public static UserMeta? PopUserFromId(string id)
        {
            IdMap.TryRemove(id, out var val);
            return val;
        }

        public IPAddress Address;
        private Guid _id;

        private string? _username;
        private bool? _banned;
        private bool? _elevated;
        private bool? _admin;

        private UserMeta(Guid address, IPAddress ipAddress, string connectId, string userName)
        {
            Users.Add(this);
            Address = ipAddress;
            Id = address;
            ConnectId = connectId;
            if (!string.IsNullOrWhiteSpace(userName))
                Username = userName;
        }
        
        public int connectedSockets;
        private string? _usernameId;
        private string? _connectId;
        private string? _messageId;
        public object _messageLock = new object();
        public object _connectLock = new object();
        public object _usernameLock = new object();

        public string? MessageId
        {
            get { 
                lock (_messageLock)
                {
                    return _messageId;
                }
            }
            set
            {
                lock (_messageLock)
                {
                    if (!string.IsNullOrWhiteSpace(_messageId))
                    {
                        Message.Close(_messageId);
                        IdMap.TryRemove(_messageId!, out _);
                    }

                    _messageId = value;
                    if (string.IsNullOrWhiteSpace(_messageId)) return;
                    Interlocked.Increment (ref connectedSockets);
                    IdMap.TryAdd(_messageId!, this);
                }
            }
        }
        public string? ConnectId
        {
            get => _connectId;
            set
            {
                lock (_connectLock)
                {
                    
                    if (!string.IsNullOrWhiteSpace(_connectId))
                    {
                        Connect.Close(_connectId);
                        IdMap.TryRemove(_connectId!, out _);
                    }

                    _connectId = value;
                    if (string.IsNullOrWhiteSpace(_connectId)) return;
                    Interlocked.Increment (ref connectedSockets);
                    IdMap.TryAdd(_connectId!, this);
                }
            }
        }
        public string? UsernameId
        {
            get => _usernameId;
            set
            {
                lock (_usernameLock)
                {
                    if (!string.IsNullOrWhiteSpace(_usernameId))
                    {
                        Usernames.Close(_usernameId);
                        IdMap.TryRemove(_usernameId!, out _);
                    }

                    _usernameId = value;
                    if (string.IsNullOrWhiteSpace(_usernameId)) return;
                    Interlocked.Increment (ref connectedSockets);
                    IdMap.TryAdd(_usernameId!, this);
                }
                
            }
        }
        
        public Guid Id
        {
            get => _id;
            set
            {
                _id = value;
                Database.EnsureNewGuid(_id, Address);
            }
        }

        public string Username
        {
            get
            {
                _username ??= Database.GetUsername(Id);
                _username = _username.StripTextMeshProFormatting();
                return _username;
            }
            set
            {
                foreach (var userMeta in Users.Where(x => x.Id == Id)) userMeta._username = value;
                Database.UpdateUsername(Id, value);
            }
        }

        public bool IsElevated
        {
            get => _elevated ??= Database.IsElevated(Id);
            set
            {
                foreach (var userMeta in Users.Where(x => x.Id == Id)) userMeta._elevated = value;
                Database.UpdateElevated(Id, value);
            }
        }
        public bool IsAdmin
        {
            get => _admin ??= Database.IsAdmin(Id);
            set
            {
                foreach (var userMeta in Users.Where(x => x.Id == Id)) userMeta._admin = value;
                Database.UpdateAdmin(Id, value);
            }
        }
        
        public bool IsBanned
        {
            get => _banned ??= Database.IsBanned(this);
            set
            {
                foreach (var userMeta in Users.Where(x => x.Id == Id)) userMeta._banned = value;
                if (value)
                    Database.Ban(this);
                else
                    Database.UnBan(this);
            }
        }

        public void Kill()
        {
            if (Interlocked.Decrement(ref connectedSockets) > 1) return;
                
            Users.Remove(this);
            Usernames.BroadcastUserList();
        }

        public string GetDressedUsername()
        {
            var prefix = Username;
            if (IsAdmin) prefix = $"<color=#FFAA00>{prefix}</color>";
            if (IsElevated) prefix = $"<color=#08a2f7>{prefix}</color>";
            return prefix;
        }
    }

    public class User
    {
        public ObjectId _id; // required??
        public string UUID { get; set; }
        public string UserName { get; set; }
        public string IpAddress { get; set; }
        public DateTime LastRequest { get; set; }
        public DateTime CoolDownTime { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsElevated { get; set; }
        public bool IsBanned { get; set; }
       
    }
}