

namespace IdentityAccess.Rbac.MultiplexedAdapter
{

    /// <summary>Defines configuration options for multiplexed RBAC adapter.</summary>
    public sealed record MultiplexedRbacAdapterOptions
    {
        /// <summary>Gets the reference directory.</summary>
        public string ReferenceDirectory { get; }

        /// <summary>Initializes a new instance of <see cref="MultiplexedRbacAdapterOptions"/>.</summary>
        public MultiplexedRbacAdapterOptions(string referenceDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(referenceDirectory);
            ReferenceDirectory = Path.GetFullPath(referenceDirectory);
        }
    }
}
