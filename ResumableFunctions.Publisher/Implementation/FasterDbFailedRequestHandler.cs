using FASTER.core;
using MessagePack.Resolvers;
using MessagePack;
using ResumableFunctions.Publisher.Abstraction;
using ResumableFunctions.Publisher.InOuts;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ResumableFunctions.Publisher.Implementation
{
    /// <summary>
    /// https://microsoft.github.io/FASTER/docs/fasterlog-basics/
    /// https://microsoft.github.io/FASTER/docs/fasterkv-basics/
    /// </summary>
    internal class FasterDbFailedRequestHandler : IFailedRequestRepo
    {
        readonly IDevice log;
        readonly FasterKV<byte[], byte[]> store;
        readonly ClientSession<byte[], byte[], byte[], byte[], Empty, IFunctions<byte[], byte[], byte[], byte[], Empty>> session;
        public FasterDbFailedRequestHandler()
        {
            log = Devices.CreateLogDevice("hlog.log", recoverDevice: true);
            store = new FasterKV<byte[], byte[]>(
                size: 1L << 6,
                logSettings: new LogSettings { LogDevice = log, MemorySizeBits = 5, PageSizeBits = 4 });
            session = store.NewSession(new SimpleFunctions<byte[], byte[]>());
        }

        public async Task Add(FailedRequest failedRequest)
        {
            session.Upsert(failedRequest.Key.ToByteArray(), MessagePackSerializer.Serialize(failedRequest, ContractlessStandardResolver.Options));
            await store.TakeHybridLogCheckpointAsync(CheckpointType.Snapshot, tryIncremental: true);
        }

        public IEnumerable<FailedRequest> GetRequests()
        {
            using (var iter = session.Iterate())
            {
                while (iter.GetNext(out var _))
                {
                    var value = new ReadOnlyMemory<byte>(iter.GetValue());
                    yield return MessagePackSerializer.Deserialize<FailedRequest>(value, ContractlessStandardResolver.Options);
                }
            }
        }


        public Task<bool> HasRequests()
        {
            using (var iter = session.Iterate())
            {
                return Task.FromResult(iter.GetNext(out var _));
            }
        }

        public async Task Remove(FailedRequest request)
        {
            var key = request.Key;
            await session.DeleteAsync(key);
        }

        public async Task Update(FailedRequest failedRequest)
        {
            await session.UpsertAsync(failedRequest.Key.ToByteArray(), MessagePackSerializer.Serialize(failedRequest, ContractlessStandardResolver.Options));
            await store.TakeHybridLogCheckpointAsync(CheckpointType.Snapshot, tryIncremental: true);
        }
    }
}
