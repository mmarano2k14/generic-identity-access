namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines one exact domain/version selection for a template Draft.</summary>
    public sealed class OrganisationProfileDomainSelectionRequest
    {
        /// <summary>Gets or sets the stable domain key.</summary>
        public string DomainKey { get; set; } = string.Empty;

        /// <summary>Gets or sets the exact positive domain version.</summary>
        public int DomainVersion { get; set; }
    }
}
