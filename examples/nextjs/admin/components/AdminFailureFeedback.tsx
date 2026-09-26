"use client";

import type { AdminActionFailure } from "../contracts/AdminActionState";
import { AdminIcon } from "./AdminIcon";

/** Presents only server-classified failure guidance; it never decides authorization or mutation outcome. */
export function AdminFailureFeedback({ failure }: { readonly failure: AdminActionFailure }) {
  return (
    <div className={`ia-failure-feedback ia-failure-${failure.kind}`} role="alert">
      <span className="ia-failure-feedback-icon"><AdminIcon name="shield" /></span>
      <div className="ia-failure-feedback-copy">
        <strong>{failure.title}</strong>
        <p>{failure.message}</p>
        {failure.recovery === "sign-in" ? (
          <a className="ia-auth-inline-link" href="/login?session=expired">Sign in again</a>
        ) : null}
        {failure.recovery === "reload" ? (
          <button className="ia-button ia-button-secondary ia-button-compact" type="button" onClick={() => window.location.reload()}>
            Reload current state
          </button>
        ) : null}
        {failure.recovery === "retry" ? <span className="ia-failure-recovery">Retry only after the dependency or connection is available.</span> : null}
        {failure.recovery === "edit" ? <span className="ia-failure-recovery">Review the form values before submitting again.</span> : null}
        {failure.recovery === "operator" ? <span className="ia-failure-recovery">Escalate to the deployment operator if the problem persists.</span> : null}
      </div>
    </div>
  );
}
