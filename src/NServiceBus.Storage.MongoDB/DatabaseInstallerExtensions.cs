namespace NServiceBus.Storage.MongoDB;

using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Driver;

static class DatabaseInstallerExtensions
{
    public static async Task SafeCreateCollection(this IMongoDatabase database,
        string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            await database.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MongoCommandException ex) when (ex is { Code: 48, CodeName: "NamespaceExists" })
        {
            //Collection already exists, so swallow the exception
        }
    }
}