## MongoDB Transactions

By default the Saga and Outbox implementations utilize [MongoDB transactions](https://docs.mongodb.com/manual/core/transactions/), which is only supported in MongoDB server version 4.0 or greater and cannot be used in all configurations of MongoDB server. MongoDB transactions rely on the MongoDB server sessions feature.

## MongoDB Server Sessions

The Saga and Outbox implementations are built around [MongoDB server sessions](https://docs.mongodb.com/manual/reference/server-sessions/). MongoDB sessions were introduced in MongoDB server version 3.6, however version 3.6 can only be used by this package when the user configures the package to disable use of transactions.

## Initialization

A default `MongoClient`, which by default targets `mongodb://localhost:27017/`, will be used unless a `IMongoClient` is provided by the user.

## Sagas

### Collection Naming

By default collections will be named after the saga data type `Name` property in lower case. To support the `NServiceBus.MongoDB` project, which uses the unaltered saga data types `Name` property, the collection naming convention can be overridden.

For example: a saga class named `SampleSaga` will result in a MongoDB collection named `samplesaga`.

### Version Element

MongoDB does not provide built-in concurrency control. An incrementing integer version number, combined with filters during document updates, is used to implement a concurrency control system. 

MongoDB stores documents in the BSON format. The version is stored as a BSON element that is added and updated on the objects BSON serialized document before operations are sent to the server.

By default the version element name is `_version`.

When a saga data is retrieved, the version element's current value is recorded in the `SynchronizedStorageSession`. When the saga data is later updated the element value is incremented by one, and a filter is used to perform the update which matches the original version value. If the document is not found and no update operation has occurred the persister will throw an exception indicating concurrency control has been triggered.

In order to support compatibility with the `NServiceBus.MongoDB` and `NServiceBus.Persistence.MongoDB` projects, the version element name can be overridden to match the appropriate version element name used by those projects.

## Serialization

For saga data types the BSON serializer has been configured to ignore missing properties on the target C# class. This allows saga data types to drop properties without errors, including allowing backwards compatibility with the `NServiceBus.MongoDB` and `NServiceBus.Persistence.MongoDB` community projects.

## Outbox

### Cleanup

Outbox cleanup is performed using a [time to live index](https://docs.mongodb.com/manual/core/index-ttl/). The index is created, or in the case of a changed TTL value recreated, on startup.

### Outbox Transaction

Outbox transactions are a wrapper around `StorageSession`, taking control of the session's commit and dispose actions.
