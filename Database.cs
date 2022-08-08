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
			
			// Make guid a unique key???
			var options = new CreateIndexOptions() {Unique = true};
			var field = new StringFieldDefinition<User>("Guid");
			var indexDefinition = new IndexKeysDefinitionBuilder<User>().Ascending(field);
			_users = db.GetCollection<User>("Users");
			var indexModel = new CreateIndexModel<User>(indexDefinition, options);
			var test2 = _users.Indexes.CreateOne(indexModel);
		}

		public static bool ContainsUsername(string userName)
		{
			return GetUserFromUserName(userName) != null;
		}
		
		public static User GetUserFromUserName(string username)
		{
			var usersCollection = _users.Database.GetCollection<User>("Users").Find(x => x.UserName == username).SingleAsync();
			return usersCollection?.Result;
		}
	}
}