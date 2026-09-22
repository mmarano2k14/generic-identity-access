

namespace IdentityAccess.Rbac
{

    /// <summary>Defines the possible decisions returned by RBAC authorization.</summary>
    public enum RbacAuthorizationDecision
    {
        /// <summary>Indicates that authorization succeeded.</summary>
        Allowed,
        /// <summary>Indicates that authorization was explicitly denied.</summary>
        Denied,
        /// <summary>Indicates that authorization could not be completed because of a technical failure.</summary>
        TechnicalFailure
    }
}
