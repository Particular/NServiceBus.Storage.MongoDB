## NServiceBus.Storage.MongoDB

This package includes [MongoDB](https://www.mongodb.com/) persistence implementations for NServiceBus:

- Sagas
- Outbox
- Transactions


## Documentation

Documentation, including configuration, usage, and samples can be found at http://docs.particular.net/persistence/mongodb


## Developing

### Prerequisites

- Projects in this solution require compatible SDKs for the following targets:
   - .NET Framework 4.5.2
   - .NET Standard 2.0
- Projects in this solution use the new .NET csproj project format which requires .NET Core 2 or greater, which is included in Visual Studio versions 2017 and greater.
- The projects also rely on NuGet for 3rd party dependencies.


### Running tests

Both test projects utilize NUnit. The test projects can be executed using the test runner included in Visual Studio or using the [`dotnet test` command](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test) from the command line.

The tests in the AcceptanceTesting project and many of the tests in the Testing project require an active MongoDB server in order for the test to pass.


#### MongoDB

By default, both the AcceptanceTests and Tests projects will connect to any MongoDB server running at the default address of `mongodb://localhost:27017`.

To use a different Mongo URL, you can set the `NServiceBusStorageMongoDB_ConnectionString` environment variable.

Instructions for installing MongoDB can be found on the [MongoDB website](https://docs.mongodb.com/manual/installation/).

##### Docker

For developers using Docker containers, the following docker command will quickly setup a container configured to use the default port:

`docker run -d -p 27017:27017 --name TestMongoDB mongo:latest --replSet tr0`

Once started, initialize the replication set (required for transaction support) by connecting to the database using a mongo shell. You can use the following docker command to start a mongo shell inside the container and initialize the replication set:

`docker exec -it TestMongoDB mongosh --eval 'rs.initiate()'`

##### Local installation

- Install the MongoDB server using the installer from the [MongoDB website](https://docs.mongodb.com/manual/installation/).
- Install the MongoDB shell (`mongosh`) from the [MongoDB website](https://www.mongodb.com/try/download/shell?jmp=docs)
- In the server configuration file (`mongod.cfg`) add followinng lines to enable replica set mode:

```
replication:
  replSetName: rs0
```

- Restart the MongoDB Windows service (if installed as a service) or start the MongoDB server from the command line
- Open the `mongosh` and type in `rs.initiate()`
