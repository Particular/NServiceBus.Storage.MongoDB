namespace NServiceBus.Storage.MongoDB;

using System.Diagnostics.CodeAnalysis;
using global::MongoDB.Driver;

sealed class DefaultMongoClientProvider : IMongoClientProvider
{
    [field: AllowNull, MaybeNull]
    public IMongoClient Client
    {
        get
        {
            field ??= new MongoClient();
            return field;
        }
    }
}