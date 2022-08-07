using System;

namespace AeroltChatServer
{
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