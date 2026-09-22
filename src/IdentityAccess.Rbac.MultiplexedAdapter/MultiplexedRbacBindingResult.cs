namespace IdentityAccess.Rbac.MultiplexedAdapter
{
    /// <summary>Pairs a validated reflection binding with its compatibility report.</summary>
    internal sealed record MultiplexedRbacBindingResult(
        MultiplexedRbacBinding? Binding,
        MultiplexedRbacCompatibilityReport Report);
}
