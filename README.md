# NServiceBus.Storage.MongoDB

NServiceBus.Storage.MongoDB is the official NServiceBus persistence implementation for [MongoDB](https://www.mongodb.com/).

It is part of the [Particular Service Platform](https://particular.net/service-platform), which includes [NServiceBus](https://particular.net/nservicebus) and tools to build, monitor, and debug distributed systems.

See the [MongoDB Persistence documentation](https://docs.particular.net/persistence/mongodb/) for more details on how to use it.

## Running tests locally

Both test projects utilize NUnit. The test projects can be executed using the test runner included in Visual Studio or using the [`dotnet test` command](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test) from the command line.

The tests in the AcceptanceTesting project and many of the tests in the Testing project require an active MongoDB server in order for the test to pass.

### MongoDB

By default, both the AcceptanceTests and Tests projects will connect to any MongoDB server running at the default address of `mongodb://localhost:27017`.

To use a different Mongo URL, you can set the `NServiceBusStorageMongoDB_ConnectionString` environment variable.

Instructions for installing MongoDB can be found on the [MongoDB website](https://docs.mongodb.com/manual/installation/).

#### Docker

For developers using Docker containers, the following docker command will quickly setup a container configured to use the default port:

`docker run -d -p 27017:27017 --name TestMongoDB mongo:latest --replSet tr0`

Once started, initialize the replication set (required for transaction support) by connecting to the database using a mongo shell. You can use the following docker command to start a mongo shell inside the container and initialize the replication set:

`docker exec -it TestMongoDB mongosh --eval 'rs.initiate()'`

#### Local installation

- Install the MongoDB server using the installer from the [MongoDB website](https://docs.mongodb.com/manual/installation/).
- Install the MongoDB shell (`mongosh`) from the [MongoDB website](https://www.mongodb.com/try/download/shell?jmp=docs)
- In the server configuration file (`mongod.cfg`) add followinng lines to enable replica set mode:

```
replication:
  replSetName: rs0
```

- Restart the MongoDB Windows service (if installed as a service) or start the MongoDB server from the command line
- Open the `mongosh` and type in `rs.initiate()`
