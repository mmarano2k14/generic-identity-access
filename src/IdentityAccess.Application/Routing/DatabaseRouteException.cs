

namespace IdentityAccess.Application.Routing
{

    /// <summary>Represents a failure raised by database route.</summary>
    public sealed class DatabaseRouteException : Exception
    {
        /// <summary>Gets the code.</summary>
        public DatabaseRouteFailure Code { get; }

        /// <summary>Initializes a new instance of <see cref="DatabaseRouteException"/>.</summary>
        public DatabaseRouteException(DatabaseRouteFailure code)
            : base($"Database routing failed ({code}).")
        {
            if (!Enum.IsDefined(code))
                throw new ArgumentOutOfRangeException(nameof(code));
            Code = code;
        }
    }
}
