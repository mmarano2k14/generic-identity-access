namespace IdentityAccess.Api.Oidc
{
    /// <summary>Rejects duplicate or unexpected OAuth/OIDC protocol parameters before orchestration.</summary>
    internal static class OidcRequestParameterGuard
    {
        /// <summary>Returns whether any named authorization query parameter occurs more than once.</summary>
        public static bool HasDuplicateQueryParameters(
            HttpRequest request,
            IReadOnlyCollection<string> names)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(names);

            return names.Any(
                name =>
                    request.Query.TryGetValue(
                        name,
                        out var values) &&
                    values.Count > 1);
        }

        /// <summary>Returns whether the form contains any protocol parameter outside the allowed set.</summary>
        public static async ValueTask<bool> HasUnexpectedFormParametersAsync(
            HttpRequest request,
            IReadOnlyCollection<string> names,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(names);

            if (!request.HasFormContentType)
            {
                return false;
            }

            var form =
                await request
                    .ReadFormAsync(cancellationToken)
                    .ConfigureAwait(false);

            return form.Keys.Any(
                key =>
                    !names.Any(
                        name =>
                            string.Equals(
                                name,
                                key,
                                StringComparison.Ordinal)));
        }

        /// <summary>Returns whether any named token form parameter occurs more than once.</summary>
        public static async ValueTask<bool> HasDuplicateFormParametersAsync(
            HttpRequest request,
            IReadOnlyCollection<string> names,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(names);

            if (!request.HasFormContentType)
            {
                return false;
            }

            var form =
                await request
                    .ReadFormAsync(cancellationToken)
                    .ConfigureAwait(false);

            return names.Any(
                name =>
                    form.TryGetValue(
                        name,
                        out var values) &&
                    values.Count > 1);
        }
    }
}
