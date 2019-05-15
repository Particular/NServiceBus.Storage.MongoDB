namespace NServiceBus
{
    using System;

    public class MongoDBSagaConcurrentUpdateException : Exception
    {
        public int ExpectedVersion { get; set; }

        public MongoDBSagaConcurrentUpdateException() : base("Concurrency issue.")
        {}

        public MongoDBSagaConcurrentUpdateException(int expectedVersion)
            : base(String.Format("Concurrency issue.  Version expected = {0}", expectedVersion))
        {
            ExpectedVersion = expectedVersion;
        }
    }
}