

namespace IdentityAccess.Infrastructure.ConfigurationRouting
{

    /// <summary>Represents a failure raised by routing configuration.</summary>
    public sealed class RoutingConfigurationException : Exception
    {
        /// <summary>Gets the code.</summary>
        public RoutingConfigurationFailure Code { get; }

        /// <summary>Initializes a new instance of <see cref="RoutingConfigurationException"/>.</summary>
        public RoutingConfigurationException(RoutingConfigurationFailure code)
            : base($"Routing configuration rejected ({code}).")
        {
            if (!Enum.IsDefined(code))
                throw new ArgumentOutOfRangeException(nameof(code));
            Code = code;
        }
    }
}
