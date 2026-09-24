using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Public non-secret authentication assurance metadata for one validated local session.</summary>
    public sealed record AuthenticationAssuranceResponse(
        string Level,
        IReadOnlyList<string> Methods,
        DateTimeOffset VerifiedAt,
        string Acr)
    {
        /// <summary>Maps trusted application assurance into the public API contract.</summary>
        public static AuthenticationAssuranceResponse From(AuthenticationAssurance assurance)
        {
            ArgumentNullException.ThrowIfNull(assurance);

            return new AuthenticationAssuranceResponse(
                assurance.Level == AuthenticationAssuranceLevel.MultiFactor
                    ? "mfa"
                    : "password",
                assurance.Methods.ToArray(),
                assurance.VerifiedAt,
                assurance.Acr);
        }
    }
}
