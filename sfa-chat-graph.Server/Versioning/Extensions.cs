using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace sfa_chat_graph.Server.Versioning
{
	public static class Extensions
	{
		private static string MakeVersionName(string collection, int version) => $"{collection}_v{version}";

		public static IMongoCollection<T> GetCollectionVersion<T>(this IMongoDatabase db, string collectionName, int version) => db.GetCollection<T>(MakeVersionName(collectionName, version));
		public static Task DropCollectionVersionAsync(this IMongoDatabase db, string collectionName, int version) => db.DropCollectionAsync(MakeVersionName(collectionName, version));

		public static Task DropBucketVersionAsync(this IMongoDatabase db, string name, int version) => DropBucketAsync(db, MakeVersionName(name, version));

		public static async Task DropBucketAsync(this IMongoDatabase db, string name)
		{
			await db.DropCollectionAsync(name + ".files");
			await db.DropCollectionAsync(name + ".chunks");
		}

		public static GridFSBucket GetBucketVersionAsync(this IMongoDatabase db, string name, int version, Action<GridFSBucketOptions> configure = null)
		{
			var options = new GridFSBucketOptions();
			configure?.Invoke(options);
			options.BucketName = MakeVersionName(name, version);
			return new GridFSBucket(db, options);

		}

	}
}
