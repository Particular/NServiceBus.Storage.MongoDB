namespace NServiceBus.TransactionalSession;

using Features;
using NServiceBus.Configuration.AdvancedExtensibility;

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
        persistenceExtensions.GetSettings().EnableFeatureByDefault(typeof(TransactionalSession));

        return persistenceExtensions;
    }
}