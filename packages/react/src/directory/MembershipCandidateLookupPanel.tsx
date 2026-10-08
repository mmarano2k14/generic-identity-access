"use client";

import { useActionState } from "react";
import type { IdentityTenantMembershipCandidateRecord } from "@generic-identity/contracts";
import { IdentityButton, IdentityInput } from "../components/index";
import { MembershipCandidatePanel } from "../pages/MembershipCandidatePanel";

/** Non-secret browser state for a delegated exact-login membership lookup. */
export interface MembershipCandidateLookupState {
  readonly kind: "idle" | "found" | "not-found" | "error";
  readonly loginIdentifier: string;
  readonly candidate?: IdentityTenantMembershipCandidateRecord | null;
  readonly message?: string;
}

export interface MembershipCandidateLookupPanelProps {
  readonly tenantId: string;
  readonly lookupAction: (
    previousState: MembershipCandidateLookupState,
    formData: FormData,
  ) => Promise<MembershipCandidateLookupState>;
  readonly createAction: (formData: FormData) => void | Promise<void>;
}

/**
 * GOLDEN-style find → verify → add. The server revalidates eligibility when
 * creating the membership; this panel is presentation, never a grant.
 */
export function MembershipCandidateLookupPanel({
  tenantId, lookupAction, createAction,
}: MembershipCandidateLookupPanelProps) {
  const [state, lookup, pending] = useActionState(lookupAction, {
    kind: "idle", loginIdentifier: "",
  } satisfies MembershipCandidateLookupState);
  const candidate = state.kind === "found" ? state.candidate ?? null : null;
  const canAdd = candidate !== null && candidate.userStatus === 1 && !candidate.existingMembershipId;

  return (
    <div className="gi-form" data-gi-component="membership-candidate-lookup">
      <form className="gi-form" action={lookup}>
        <input type="hidden" name="tenantId" value={tenantId} />
        <label className="gi-field">
          <span className="gi-field-label">Exact login identifier</span>
          <IdentityInput name="loginIdentifier" defaultValue={state.loginIdentifier} autoComplete="username" maxLength={320} required />
        </label>
        <IdentityButton variant="secondary" type="submit" disabled={pending}>Find identity</IdentityButton>
      </form>

      {state.kind === "error" ? <p className="gi-form-error" role="alert">{state.message ?? "Candidate lookup failed."}</p> : null}
      {state.kind === "not-found" ? <p role="status">No eligible identity matches this exact login.</p> : null}

      {candidate ? (
        <MembershipCandidatePanel candidate={candidate} actions={canAdd ? (
          <form className="gi-form" action={createAction}>
            <input type="hidden" name="tenantId" value={tenantId} />
            <input type="hidden" name="loginIdentifier" value={state.loginIdentifier} />
            <label className="gi-field">
              <span className="gi-field-label">Membership status</span>
              <select className="gi-input" name="status" defaultValue="1">
                <option value="1">Active</option>
                <option value="2">Inactive</option>
              </select>
            </label>
            <IdentityButton variant="primary" type="submit">Confirm membership</IdentityButton>
          </form>
        ) : (
          <p className="gi-form-error" role="status">The account is inactive or already belongs to this tenant.</p>
        )} />
      ) : null}
    </div>
  );
}
