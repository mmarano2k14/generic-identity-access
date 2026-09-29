"use client";

import { useEffect, useMemo, useState } from "react";
import type { IdentityScopeTypeRecord } from "@identity-access/client";
import { AdminField, AdminSelectField } from "./AdminField";

type Props = {
  readonly initialModelVersion?: number;
  readonly initialScopeType?: string;
};

type ScopeTypeLookupFailure = {
  readonly message?: string;
};

/**
 * Couples the security-model version to its registered scope-type catalogue.
 * The browser only calls the local protected admin route; Identity Access
 * credentials and authorization decisions remain server-side.
 */
export function AdminResourceScopeTypeFields({ initialModelVersion, initialScopeType }: Props) {
  const initialModelValue = initialModelVersion === undefined ? "" : String(initialModelVersion);
  const [modelVersion, setModelVersion] = useState(initialModelValue);
  const [scopeType, setScopeType] = useState(initialScopeType ?? "");
  const [scopeTypes, setScopeTypes] = useState<readonly IdentityScopeTypeRecord[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | undefined>();

  const parsedModelVersion = useMemo(() => {
    if (!/^\d+$/.test(modelVersion)) return undefined;
    const value = Number(modelVersion);
    return Number.isSafeInteger(value) && value > 0 ? value : undefined;
  }, [modelVersion]);

  useEffect(() => {
    if (parsedModelVersion === undefined) {
      setScopeTypes([]);
      setScopeType("");
      setLoading(false);
      setError(undefined);
      return;
    }

    const controller = new AbortController();
    const preserveInitial = parsedModelVersion === initialModelVersion ? initialScopeType ?? "" : "";
    setLoading(true);
    setError(undefined);
    setScopeTypes([]);
    setScopeType(preserveInitial);

    void (async () => {
      try {
        const response = await fetch(`/api/identity/scope-types?modelVersion=${encodeURIComponent(String(parsedModelVersion))}`, {
          method: "GET",
          headers: { Accept: "application/json" },
          cache: "no-store",
          signal: controller.signal,
        });
        const payload = await response.json() as readonly IdentityScopeTypeRecord[] | ScopeTypeLookupFailure;
        if (!response.ok) {
          const failure = payload as ScopeTypeLookupFailure;
          throw new Error(failure.message ?? "Registered scope types could not be loaded.");
        }

        if (!Array.isArray(payload)) throw new Error("Scope-type lookup returned an invalid response.");
        const records = payload as readonly IdentityScopeTypeRecord[];
        setScopeTypes(records);
        setScopeType((current) => records.some((item) => item.key === current) ? current : "");
      } catch (lookupError) {
        if (controller.signal.aborted) return;
        setScopeTypes([]);
        setError(lookupError instanceof Error ? lookupError.message : "Registered scope types could not be loaded.");
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    })();

    return () => controller.abort();
  }, [initialModelVersion, initialScopeType, parsedModelVersion]);

  const preserveExistingValue = error !== undefined
    && initialScopeType !== undefined
    && parsedModelVersion === initialModelVersion;

  const scopeTypeHint = error
    ? preserveExistingValue
      ? `${error} The existing scope type is preserved for this edit.`
      : error
    : (parsedModelVersion === undefined
      ? "Enter a registered security-model version first."
      : loading
        ? "Loading the registered scope types for this security-model version."
        : scopeTypes.length === 0
          ? "No scope types are registered for this security-model version. Register them under Security models before creating a resource scope."
          : "Only scope types registered for the selected immutable security-model version are available.");

  return (
    <>
      <AdminField
        label="Security model version"
        name="modelVersion"
        type="number"
        min={1}
        step={1}
        value={modelVersion}
        onChange={(event) => setModelVersion(event.currentTarget.value)}
        required
      />
      {preserveExistingValue ? <input type="hidden" name="scopeType" value={initialScopeType} /> : null}
      <AdminSelectField
        label="Scope type"
        name={preserveExistingValue ? undefined : "scopeType"}
        value={scopeType}
        onChange={(event) => setScopeType(event.currentTarget.value)}
        required
        disabled={parsedModelVersion === undefined || loading || error !== undefined}
        hint={scopeTypeHint}
      >
        <option value="">{loading ? "Loading scope types…" : "Select a registered scope type"}</option>
        {scopeTypes.map((item) => (
          <option key={item.key} value={item.key}>
            {item.displayName} ({item.key})
          </option>
        ))}
      </AdminSelectField>
    </>
  );
}
