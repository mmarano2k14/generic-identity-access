"use client";

import { useEffect, useMemo, useState } from "react";
import type { IdentityScopeTypeRecord } from "@generic-identity/contracts/application-security";
import { IdentityInput } from "../components/index";

export interface ResourceScopeTypeFieldsProps {
  readonly initialModelVersion?: number;
  readonly initialScopeType?: string;
  readonly disabled?: boolean;
  readonly scopeTypesEndpoint?: string;
}

/**
 * Couples one immutable security-model version to its registered scope-type catalog.
 * The browser calls only the consumer-owned same-origin endpoint.
 */
export function ResourceScopeTypeFields({
  initialModelVersion,
  initialScopeType,
  disabled = false,
  scopeTypesEndpoint = "/api/identity/scope-types",
}: ResourceScopeTypeFieldsProps) {
  const initialModelValue = initialModelVersion === undefined ? "" : String(initialModelVersion);
  const [modelVersion, setModelVersion] = useState(initialModelValue);
  const [scopeType, setScopeType] = useState(initialScopeType ?? "");
  const [scopeTypes, setScopeTypes] = useState<readonly IdentityScopeTypeRecord[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | undefined>();

  const parsedModelVersion = useMemo(() => {
    if (!/^[1-9][0-9]*$/u.test(modelVersion)) return undefined;
    const value = Number(modelVersion);
    return Number.isSafeInteger(value) ? value : undefined;
  }, [modelVersion]);

  useEffect(() => {
    if (disabled || parsedModelVersion === undefined) {
      setScopeTypes([]);
      setLoading(false);
      setError(undefined);
      if (parsedModelVersion === undefined) setScopeType("");
      return undefined;
    }

    const controller = new AbortController();
    const preserveInitial = parsedModelVersion === initialModelVersion ? initialScopeType ?? "" : "";
    setLoading(true);
    setError(undefined);
    setScopeTypes([]);
    setScopeType(preserveInitial);

    void (async () => {
      try {
        const parameters = new URLSearchParams({ modelVersion: String(parsedModelVersion) });
        const response = await fetch(`${scopeTypesEndpoint}?${parameters.toString()}`, {
          method: "GET",
          headers: { Accept: "application/json" },
          cache: "no-store",
          signal: controller.signal,
        });
        const payload: unknown = await response.json();
        if (!response.ok) {
          const message = typeof payload === "object" && payload !== null && "message" in payload
            ? String((payload as { message?: unknown }).message ?? "")
            : "";
          throw new Error(message || "Registered scope types could not be loaded.");
        }
        if (!Array.isArray(payload)) throw new Error("Scope-type lookup returned an invalid response.");
        const records = payload.filter(isScopeTypeRecord);
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
  }, [disabled, initialModelVersion, initialScopeType, parsedModelVersion, scopeTypesEndpoint]);

  const preserveExistingValue = error !== undefined
    && initialScopeType !== undefined
    && parsedModelVersion === initialModelVersion;

  const hint = error
    ? preserveExistingValue
      ? `${error} The existing scope type is preserved for this edit.`
      : error
    : parsedModelVersion === undefined
      ? "Enter a registered security-model version first."
      : loading
        ? "Loading registered scope types."
        : scopeTypes.length === 0
          ? "No scope types are registered for this security-model version."
          : "Only scope types registered for this immutable security-model version are available.";

  return (
    <>
      <label className="gi-field">
        <span className="gi-field-label">Security model version</span>
        <IdentityInput
          name="modelVersion"
          type="number"
          min={1}
          step={1}
          value={modelVersion}
          onChange={(event) => setModelVersion(event.currentTarget.value)}
          required
          disabled={disabled}
        />
      </label>

      {preserveExistingValue ? <input type="hidden" name="scopeType" value={initialScopeType} /> : null}

      <label className="gi-field">
        <span className="gi-field-label">Scope type</span>
        <select
          className="gi-input"
          name={preserveExistingValue ? undefined : "scopeType"}
          value={scopeType}
          onChange={(event) => setScopeType(event.currentTarget.value)}
          required
          disabled={disabled || parsedModelVersion === undefined || loading || error !== undefined}
        >
          <option value="">{loading ? "Loading scope types…" : "Select a registered scope type"}</option>
          {scopeTypes.map((item) => (
            <option key={item.key} value={item.key}>
              {item.displayName} ({item.key})
            </option>
          ))}
        </select>
        <small className="gi-field-hint">{hint}</small>
      </label>
    </>
  );
}

function isScopeTypeRecord(value: unknown): value is IdentityScopeTypeRecord {
  if (typeof value !== "object" || value === null) return false;
  const record = value as Record<string, unknown>;
  return typeof record.key === "string"
    && typeof record.displayName === "string"
    && typeof record.canAttachToTenant === "boolean"
    && (record.parentKey === undefined || typeof record.parentKey === "string");
}
