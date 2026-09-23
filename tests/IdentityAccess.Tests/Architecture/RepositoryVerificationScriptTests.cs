namespace IdentityAccess.Tests.Architecture
{
    public sealed class RepositoryVerificationScriptTests
    {
        [Fact]
        public void PowerShell_source_consistency_gates_do_not_require_native_last_exit_code()
        {
            var source = ReadVerifyScript();

            Assert.Contains(
                "if (-not $?) { throw \"Authorization source consistency validation failed.\" }",
                source,
                StringComparison.Ordinal);
            Assert.Contains(
                "if (-not $?) { throw \"OIDC source consistency validation failed.\" }",
                source,
                StringComparison.Ordinal);
            Assert.Contains(
                "if (-not $?) { throw \"MFA source consistency validation failed.\" }",
                source,
                StringComparison.Ordinal);
            Assert.Contains(
                "if (-not $?) { throw \"TOTP source consistency validation failed.\" }",
                source,
                StringComparison.Ordinal);
            Assert.Contains(
                "if (-not $?) { throw \"Recovery source consistency validation failed.\" }",
                source,
                StringComparison.Ordinal);
            Assert.Contains(
                "if (-not $?) { throw \"TypeScript source consistency validation failed.\" }",
                source,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Native_process_gates_continue_to_check_last_exit_code()
        {
            var source = ReadVerifyScript();

            Assert.Contains(
                "if ($LASTEXITCODE -ne 0) { throw \"TypeScript tests failed.\" }",
                source,
                StringComparison.Ordinal);
            Assert.Contains(
                "if ($LASTEXITCODE -ne 0) { throw \"SDK check failed.\" }",
                source,
                StringComparison.Ordinal);
        }

        [Fact]
        public void TypeScript_dependencies_are_restored_when_local_compiler_is_missing()
        {
            var source = ReadVerifyScript();

            Assert.Contains(
                "node_modules/.bin/tsc.cmd",
                source,
                StringComparison.Ordinal);
            Assert.Contains(
                "node_modules/.bin/tsc",
                source,
                StringComparison.Ordinal);
            Assert.Contains(
                "npm install --ignore-scripts --no-audit --no-fund --package-lock=false",
                source,
                StringComparison.Ordinal);
            Assert.Contains(
                "if ($LASTEXITCODE -ne 0) { throw \"TypeScript dependency restore failed.\" }",
                source,
                StringComparison.Ordinal);
        }

        private static string ReadVerifyScript()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "IdentityAccess.sln")))
                {
                    return File.ReadAllText(
                        Path.Combine(
                            current.FullName,
                            "scripts",
                            "verify.ps1"));
                }

                current = current.Parent;
            }

            throw new InvalidOperationException(
                "Repository root could not be located.");
        }
    }
}
