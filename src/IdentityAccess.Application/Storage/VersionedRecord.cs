

namespace IdentityAccess.Application.Storage
{

    /// <summary>A persisted value with the optimistic-concurrency version observed by the caller.</summary>
    public sealed record VersionedRecord<T>(T Value, long Version) where T : class;
}
