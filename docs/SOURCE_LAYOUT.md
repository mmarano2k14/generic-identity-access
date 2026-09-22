# C# Source Layout

The repository enforces the following C# conventions:

1. One declared top-level type per `.cs` file.
2. The file name matches the declared type name.
3. Namespace declarations use block-scoped syntax.
4. Production and test source follow the same type/file rule.
5. Shared test doubles and helpers live in dedicated files rather than being nested into unrelated tests.

Example:

```csharp
namespace IdentityAccess.Domain
{
    public sealed record SubjectReference(...);
}
```

The following form is not accepted:

```csharp
namespace IdentityAccess.Domain;
```

Source-layout architecture tests enforce these rules during the .NET test suite.

The repository `.editorconfig` also configures block-scoped namespaces as an error-level style requirement.
