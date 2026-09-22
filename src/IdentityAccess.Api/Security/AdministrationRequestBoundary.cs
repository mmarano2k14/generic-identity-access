using IdentityAccess.Domain;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Verifies that caller-selected route scope and application remain inside the authenticated
    /// session boundary.
    /// </summary>
    internal static class AdministrationRequestBoundary
    {
        /// <summary>
        /// Returns <see langword="true"/> when route identity scope and application match the
        /// authenticated administration context.
        /// </summary>
        public static bool Matches(
            HttpContext httpContext,
            AdministrationRequestContext context)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            ArgumentNullException.ThrowIfNull(context);

            if (httpContext.Request.RouteValues.TryGetValue(
                    "identityScopeId",
                    out var scopeValue))
            {
                if (scopeValue is null ||
                    !Guid.TryParse(
                        Convert.ToString(
                            scopeValue,
                            System.Globalization.CultureInfo.InvariantCulture),
                        out var requestedScope) ||
                    requestedScope !=
                        context.Subject.IdentityScopeId)
                {
                    return false;
                }
            }

            if (httpContext.Request.RouteValues.TryGetValue(
                    "applicationKey",
                    out var applicationValue))
            {
                try
                {
                    var requestedApplication =
                        new ApplicationKey(
                            Convert.ToString(
                                applicationValue,
                                System.Globalization.CultureInfo.InvariantCulture)
                            ?? string.Empty);

                    if (requestedApplication !=
                        context.Application)
                    {
                        return false;
                    }
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
