## NServiceBus.Storage.MongoDB

This package includes [MongoDB](https://www.mongodb.com/) persistence implementations for NServiceBus v7:

- Sagas
- Outbox

Transactions

## Install ##
Add the `NServiceBus.Storage.MongoDB` package to your NServiceBus project.

 ```Install-Package NServiceBus.Storage.MongoDB```   



## Configuration ##

**1** Set the `EndpointConfiguration` object to use `MongoDbPersistence`

```csharp
using NServiceBus;

class Program
{
    public async Task Main()
    {
        var endpointConfiguration = new EndpointConfiguration("Endpoint Name");
        endpointConfiguration.UsePersistence<MongoPersistence>();
    }
}
```

**2** Hit F5. Assuming your MongoDB server is at `mongourl://localhost:27017` it will just work.



## Customizing the MongoDB connection ##

Provide a custom `MongoClient` by calling the ```.Client(client)``` extension method.

```csharp
endpointConfiguration
	.UsePersistence<MongoDBPersistence>()
	.MongoClient(new MongoClient("Custom Mongo URL"));
```



## Customizing the MongoDB connection

By default the persistence will use a [sanitized](https://docs.mongodb.com/manual/reference/limits/#Restrictions-on-Database-Names-for-Windows) version of the endpoint name as the database to store NServiceBus objects. Provide a custom database name for by calling the ```.DatabaseName(name)``` extension method:

```csharp
endpointConfiguration
	.UsePersistence<MongoDBPersistence>()
	.DatabaseName("MyCustomName");
```



## Transactions

By default the persistence will use session [transactions](https://docs.mongodb.com/manual/core/transactions/) for making changes to Saga data. This allows atomic guarantees when multiple sagas are invoked by a single message. To support older MongoDB servers (< 4) and MongoDB sharded clusters you can disable transactions by calling the `.UseTransactions(false)` extension method:



```csharp
endpointConfiguration
	.UsePersistence<MongoDBPersistence>()
	.UseTransactions(false);
```



You can join the existing MongoDB session transaction in your handlers by obtaining a reference to a database collection from the `IMessageHandlerContext`:

```c#
public Task Handle(MyMessage message, IMessageHandlerContext context)
{
    var collection = context.SynchronizedStorageSession().GetCollection<MyBusinessObject>("collectionname");    
}
```

The transaction and session will be automatically completed by NServiceBus when handler processing is complete.



## Running Tests Locally

By default both the AcceptanceTests and Tests projects will connect to any MongoDB server running at the default address of `mongodb://localhost:27017`

To use a different Mongo URl you can set the `NServiceBusStorageMongoDB_ConnectionString` environment variable.

Instructions for installing MongoDB can be found on the [MongoDB website](https://docs.mongodb.com/manual/installation/).

For developers using Docker containers the following docker command will quickly setup a container configured to use the default port:

`docker run -d -p 27017:27017 --name="Test MongoDB" mongo:latest`



## Full Documentation

Full documentation and samples can be found at http://docs.particular.net/persistence/mongodb
