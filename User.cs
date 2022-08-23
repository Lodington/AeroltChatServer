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
        private static readonly Dictionary<Guid, UserMeta> Users = new Dictionary<Guid, UserMeta>();
        public static IEnumerable<UserMeta> UsersEnumerator => Users.Values.ToArray();
        public static IEnumerable<UserMeta> AdminsEnumerator => UsersEnumerator.Where(x => x.IsElevated);

        public static UserMeta GetOrCreateUserFromGuid(Guid webSocketGuid)
        {
            if (!Users.TryGetValue(webSocketGuid, out var user))
                user = new UserMeta(webSocketGuid);
            return user;
        }
        
        private Guid _id;

        private string? _username;
        private bool? _banned;
        private bool? _elevated;
        private bool? _admin;
        private UserMeta(Guid webSocketGuid)
        {
            Id = webSocketGuid;
            Users[Id] = this;
        }
        
        public Guid Id
        {
            get => _id;
            set
            {
                _id = value;
                Database.EnsureNewGuid(_id);
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
                foreach (var userMeta in Users.Where(x => x.Key == Id)) userMeta.Value._username = value;
                Database.UpdateUsername(Id, value);
            }
        }

        public bool IsElevated
        {
            get => _elevated ??= Database.IsElevated(Id);
            set
            {
                foreach (var userMeta in Users.Where(x => x.Key == Id)) userMeta.Value._elevated = value;
                Database.UpdateElevated(Id, value);
            }
        }
        public bool IsAdmin
        {
            get => _admin ??= Database.IsAdmin(Id);
            set
            {
                foreach (var userMeta in Users.Where(x => x.Key == Id)) userMeta.Value._admin = value;
                Database.UpdateAdmin(Id, value);
            }
        }

        public bool IsBanned => false;
        /* TODO fix this shit
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
        }*/

        public string GetDressedUsername()
        {
            var prefix = Username;
            if (IsAdmin) prefix = $"<color=#FFAA00>{prefix}</color>";
            if (IsElevated) prefix = $"<color=#08a2f7>{prefix}</color>";
            return prefix;
        }

        public static void CleanDeadUsers()
        {
            var killed = Users.Where(userMeta => !Connect.IsAlive(userMeta.Key) || !Message.IsAlive(userMeta.Key) || !Usernames.IsAlive(userMeta.Key)).ToArray();
            foreach (var meta in killed) Users.Remove(meta.Key);
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