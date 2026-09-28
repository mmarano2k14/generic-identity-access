"use client";

import { useState } from "react";
import type { IdentityTenantMembershipCandidateRecord } from "@identity-access/client";

type Props = {
  readonly tenantId: string;
};

/** Exact-login lookup for tenant-scoped member addition; it never performs partial directory search. */
export function AdminExactMemberLookup({ tenantId }: Props) {
  const [loginIdentifier, setLoginIdentifier] = useState("");
  const [candidate, setCandidate] = useState<IdentityTenantMembershipCandidateRecord | null>(null);
  const [status, setStatus] = useState<string>("Enter the complete login or email, then resolve exactly one account.");
  const [loading, setLoading] = useState(false);

  const usableCandidate = candidate !== null
    && candidate.userStatus === 1
    && candidate.existingMembershipId === undefined;

  const lookup = async () => {
    const login = loginIdentifier.trim();
    setCandidate(null);
    if (!login) {
      setStatus("Enter the complete login or email.");
      return;
    }

    setLoading(true);
    setStatus("Resolving exact account…");
    try {
      const query = new URLSearchParams({ tenantId, loginIdentifier: login });
      const response = await fetch(`/api/identity/membership-candidates?${query.toString()}`, {
        method: "GET",
        cache: "no-store",
        headers: { Accept: "application/json" },
      });
      if (response.status === 404) {
        setStatus("No eligible account found for that exact login.");
        return;
      }
      if (!response.ok) {
        setStatus(response.status === 403
          ? "You are not allowed to resolve membership candidates for this tenant."
          : "Account lookup is temporarily unavailable.");
        return;
      }

      const result = await response.json() as IdentityTenantMembershipCandidateRecord;
      setCandidate(result);
      if (result.existingMembershipId) {
        setStatus(`Already a member of this tenant (${result.existingMembershipStatus === 1 ? "active" : "inactive"}).`);
      } else if (result.userStatus !== 1) {
        setStatus("The account exists but is not active and cannot be added.");
      } else {
        setStatus(`${result.displayName} is eligible to be added to this tenant.`);
      }
    } catch {
      setStatus("Account lookup is temporarily unavailable.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="ia-exact-member-lookup">
      <input type="hidden" name="userId" value={usableCandidate ? candidate.userId : ""} />
      <label className="ia-field">
        <span className="ia-field-label">Login or email</span>
        <div className="ia-inline-input-action">
          <input
            className="ia-input"
            type="text"
            name="loginIdentifier"
            value={loginIdentifier}
            maxLength={320}
            autoComplete="off"
            placeholder="Complete login or email"
            onChange={(event) => {
              setLoginIdentifier(event.currentTarget.value);
              setCandidate(null);
              setStatus("Resolve the exact account before adding it.");
            }}
          />
          <button className="ia-button ia-button-secondary" type="button" onClick={lookup} disabled={loading || !loginIdentifier.trim()}>
            {loading ? "Resolving…" : "Find account"}
          </button>
        </div>
      </label>
      {candidate ? (
        <div className={`ia-candidate-card ${usableCandidate ? "ia-candidate-card-ready" : ""}`}>
          <strong>{candidate.displayName}</strong>
          <span>{candidate.userStatus === 1 ? "Active account" : "Inactive account"}</span>
          <code>{candidate.userId}</code>
        </div>
      ) : null}
      <small className="ia-field-hint" role="status" aria-live="polite">{status}</small>
    </div>
  );
}
