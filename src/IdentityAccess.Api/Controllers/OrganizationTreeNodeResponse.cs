using OrganizationDirectory.Application.Administration;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one organization hierarchy node in an administration response.</summary>
    public sealed class OrganizationTreeNodeResponse
    {
        /// <summary>Gets the organization represented by this node.</summary>
        public OrganizationResponse Organization { get; init; } = new();

        /// <summary>Gets child hierarchy nodes.</summary>
        public IReadOnlyList<OrganizationTreeNodeResponse> Children { get; init; } =
            Array.Empty<OrganizationTreeNodeResponse>();

        /// <summary>Maps an application hierarchy node to the API response.</summary>
        public static OrganizationTreeNodeResponse From(OrganizationTreeNode node)
        {
            ArgumentNullException.ThrowIfNull(node);

            return new OrganizationTreeNodeResponse
            {
                Organization = OrganizationResponse.From(node.Organization),
                Children = node.Children.Select(From).ToArray()
            };
        }
    }
}
