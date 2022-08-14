using System;
using System.Linq;
using System.Net;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AeroltChatServer
{
	public static class Database
	{
		private static IMongoCollection<BsonDocument> _bannedUsers;
		private static IMongoCollection<User> _users;

		public static void Init(string connectionString)
		{
			var client = new MongoClient(connectionString);
			var db = client.GetDatabase("AeroltChatServer");
			_bannedUsers = db.GetCollection<BsonDocument>("BannedUsers");
			
			// Make uuid a unique key???
			var options = new CreateIndexOptions {Unique = true};
			var field = new StringFieldDefinition<User>("UUID");
			var indexDefinition = new IndexKeysDefinitionBuilder<User>().Ascending(field);
			_users = db.GetCollection<User>("Users");
			var indexModel = new CreateIndexModel<User>(indexDefinition, options);
			var indexes = _users.Indexes.List();
			while (indexes.MoveNext())
				if (indexes.Current.SelectMany(doc => doc).Any(elem => elem.Name == "UUID"))
					return;

			_users.Indexes.CreateOne(indexModel);
		}

		public static bool ContainsUsername(string userName)
		{
			return GetUserFromUserName(userName) != null;
		}
		
		public static User GetUserFromUserName(string username)
		{
			var targets = _users.Find(x => x.UserName == username);
			return targets.Any() ? targets.First() : null;
		}

		public static bool IsBanned(UserMeta endpoint) => _bannedUsers.Find(new BsonDocument("ip", endpoint.Address.ToString())).Any();
		// TODO test for existing ips banned
		public static void Ban(UserMeta endpoint) => _bannedUsers.InsertOne(new BsonDocument("ip", endpoint.Address.ToString()));
		public static void UnBan(UserMeta endpoint) => _bannedUsers.DeleteOne(new BsonDocument("ip", endpoint.Address.ToString()));

		public static bool EnsureNewGuid(Guid guid, IPAddress currentIp)
		{
			if (guid == default) return true;
			var model = new User { UUID = guid.ToString() };
			var succeeded = false;
			if (_users.CountDocuments(x => x.UUID == guid.ToString(), new CountOptions() {Limit = 1}) == 0)
			{
				_users.InsertOne(model);
				succeeded = true;
			}
			_users.UpdateOne(x => x.UUID == guid.ToString(), Builders<User>.Update.Set(x => x.IpAddress, currentIp.ToString()).Set(x => x.LastRequest, DateTime.Now));
			return succeeded;
		}

		public static void UpdateUsername(Guid guid, string username) => _users.UpdateOne(x => x.UUID == guid.ToString(), Builders<User>.Update.Set(x => x.UserName, username));
		public static string GetUsername(Guid id) => _users.FindSync(x => x.UUID == id.ToString()).FirstOrDefault()?.UserName;
		public static bool IsElevated(Guid id) => _users.FindSync(x => x.UUID == id.ToString()).FirstOrDefault()?.IsElevated ?? false;
		public static bool IsAdmin(Guid id) => _users.FindSync(x => x.UUID == id.ToString()).FirstOrDefault()?.IsAdmin ?? false;
		public static void UpdateElevated(Guid id, bool value) => _users.UpdateOne(x => x.UUID == id.ToString(), Builders<User>.Update.Set(x => x.IsElevated, value));
		public static void UpdateAdmin(Guid id, bool value) => _users.UpdateOne(x => x.UUID == id.ToString(), Builders<User>.Update.Set(x => x.IsAdmin, value));

		public static void DropGuid(Guid id)
		{
			if (_users.CountDocuments(x => x.UUID == id.ToString(), new CountOptions {Limit = 1}) != 0)
				_users.DeleteOne(x => x.UUID == id.ToString());
		}
	}
}