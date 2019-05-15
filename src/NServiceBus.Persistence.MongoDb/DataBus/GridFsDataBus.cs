using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace NServiceBus.Persistence.MongoDB.DataBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.DataBus;

    public class GridFsDataBus : IDataBus
    {
        private readonly IGridFSBucket _fs;

        public GridFsDataBus(IMongoDatabase database)
        {
            _fs = new GridFSBucket(database);
        }

        public async Task<Stream> Get(string key)
        {
            var stream = new MemoryStream();
            await _fs.DownloadToStreamAsync(ObjectId.Parse(key), stream).ConfigureAwait(false);
            stream.Position = 0;
            return stream;
        }

        public async Task<string> Put(Stream stream, TimeSpan timeToBeReceived)
        {
            var key = await _fs.UploadFromStreamAsync(Guid.NewGuid().ToString(), stream).ConfigureAwait(false);
            return key.ToString();
        }

        Task IDataBus.Start()
        {
            return TaskEx.CompletedTask;
        }
    }
}