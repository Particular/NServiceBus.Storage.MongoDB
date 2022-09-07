namespace NServiceBus.TransactionalSession;

using System.Threading;
using System.Threading.Tasks;
using Features;
using Configuration.AdvancedExtensibility;

/// <summary>
/// MongoDB persistence extensions for <see cref="ITransactionalSession"/> support.
/// </summary>
public static class MongoTransactionalSessionExtensions
{
    /// <summary>
    /// Enables transactional session for this endpoint.
    /// </summary>
    public static PersistenceExtensions<MongoPersistence> EnableTransactionalSession(
        this PersistenceExtensions<MongoPersistence> persistenceExtensions)
    {
        persistenceExtensions.GetSettings().EnableFeatureByDefault<MongoTransactionalSession>();

        return persistenceExtensions;
    }

    /// <summary>
    /// Opens the transactional session.
    /// </summary>
    public static Task Open(this ITransactionalSession session, CancellationToken cancellationToken = default) =>
        session.Open(new MongoSessionOptions(), cancellationToken);
}