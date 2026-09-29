using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Application.Administration
{
    /// <summary>Represents one organization and its children in a tenant-local hierarchy.</summary>
    public sealed class OrganizationTreeNode
    {
        /// <summary>Gets the organization represented by this node.</summary>
        public Organization Organization { get; }

        /// <summary>Gets the deterministic child nodes.</summary>
        public IReadOnlyList<OrganizationTreeNode> Children { get; }

        /// <summary>Initializes one immutable organization tree node.</summary>
        public OrganizationTreeNode(
            Organization organization,
            IReadOnlyList<OrganizationTreeNode> children)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(children);
            Organization = organization;
            Children = children;
        }
    }
}
