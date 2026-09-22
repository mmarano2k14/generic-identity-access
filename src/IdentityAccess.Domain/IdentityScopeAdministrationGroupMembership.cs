namespace IdentityAccess.Domain
{
    /// <summary>Direct membership of an identity-scope user in an administration group.</summary>
    public sealed record IdentityScopeAdministrationGroupMembership
    {
        /// <summary>Gets the administration group.</summary>
        public IdentityScopeAdministrationGroupReference Group { get; }
        /// <summary>Gets the member subject.</summary>
        public SubjectReference Subject { get; }

        /// <summary>Initializes a scope-administration group membership.</summary>
        public IdentityScopeAdministrationGroupMembership(
            IdentityScopeAdministrationGroupReference group,
            SubjectReference subject)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(subject);

            if (group.IdentityScopeId != subject.IdentityScopeId)
                throw new ArgumentException(
                    "Administration group and subject must belong to the same identity scope.",
                    nameof(subject));

            Group = group;
            Subject = subject;
        }
    }
}
