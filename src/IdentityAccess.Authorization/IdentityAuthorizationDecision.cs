

namespace IdentityAccess.Authorization
{

    /// <summary>Defines the possible decisions returned by identity authorization.</summary>
    public enum IdentityAuthorizationDecision
    {
        /// <summary>Indicates that authorization succeeded.</summary>
        Allowed,
        /// <summary>Indicates that authorization was explicitly denied.</summary>
        Denied,
        /// <summary>Indicates that authorization could not be completed because of a technical failure.</summary>
        TechnicalFailure
    }
}
