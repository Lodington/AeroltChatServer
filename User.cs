#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using MongoDB.Bson;

namespace AeroltChatServer
{
    public class UserMeta
    {
        private static readonly ConcurrentDictionary<IPAddress, UserMeta> Users = new ConcurrentDictionary<IPAddress, UserMeta>();
        private static readonly ConcurrentDictionary<Guid, UserMeta> _connectedUsers = new ConcurrentDictionary<Guid, UserMeta>();
        private static readonly ConcurrentDictionary<string, UserMeta> IdMap = new ConcurrentDictionary<string, UserMeta>();
        public static IEnumerable<UserMeta> UsersEnumerator => _connectedUsers.Values;

        public static IEnumerable<UserMeta> AdminsEnumerator => UsersEnumerator.Where(x => x.IsElevated);

        public static UserMeta GetOrMakeUser(IPAddress address)
        {
            if (!Users.ContainsKey(address)) Users[address] = new UserMeta(address);
            var val = Users[address];
            return val;
        }
        
        public static UserMeta? PopUserFromId(string id)
        {
            
                IdMap.TryRemove(id, out var val);
                return val;
        }
        
        private string? _usernameId;
        private string? _connectId;
        private string? _messageId;
        
        public IPAddress Address;
        private bool _wasKilled;
        private Guid _id;
        
        private string? _username;
        private bool? _banned;
        private bool? _elevated;

        private UserMeta(IPAddress ipAddress)
        {
            Address = ipAddress;
        }

        public object _messageLock = new object();
        public object _connectLock = new object();
        public object _usernameLock = new object();
        public string? MessageId
        {
            get => _messageId;
            set
            {
                lock (_messageLock)
                {
                    
                    if (!string.IsNullOrWhiteSpace(_messageId))
                    {
                        Usernames.Close(_messageId);
                        IdMap.TryRemove(_messageId!, out _);
                    }

                    _messageId = value;
                    if (!string.IsNullOrWhiteSpace(_messageId))
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
                        Usernames.Close(_connectId);
                        IdMap.TryRemove(_connectId!, out _);
                    }

                    _connectId = value;
                    if (!string.IsNullOrWhiteSpace(_connectId))
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
                    if (!string.IsNullOrWhiteSpace(_usernameId))
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
                
                if (_connectedUsers.ContainsKey(_id)) _connectedUsers[_id].Kill();
                _connectedUsers[_id] = this;
                Database.CreateNewGuid(_id);
            }
        }

        public string Username
        {
            get => _username ??= Database.GetUsername(Id);
            set
            {
                _username = value;
                Database.UpdateUsername(Id, value);
            }
        }

        public bool IsElevated
        {
            get => _elevated ??= Database.IsElevated(Id);
            set
            {
                _elevated = value;
                Database.UpdateElevated(Id, value);
            }
        }
        public bool IsBanned
        {
            get => _banned ??= Database.IsBanned(this);
            set
            {
                _banned = value;
                if (value)
                    Database.Ban(this);
                else
                    Database.UnBan(this);
            }
        }

        public void Kill() // Called when any socket is closed;
        {   // this is exactly what Sessions.CloseSession does
            if (_wasKilled) return;
            
            UsernameId = null;
            MessageId = null;
            ConnectId = null;
            _connectedUsers.TryRemove(Id, out _);
            
            _wasKilled = true;
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
        public bool IsElevated { get; set; }
        public bool IsBanned { get; set; }
    }
}