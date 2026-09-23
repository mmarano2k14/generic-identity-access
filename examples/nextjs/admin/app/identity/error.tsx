"use client";

import { AdminIcon } from "../../components/AdminIcon";

export default function IdentityError({ reset }: { readonly reset: () => void }) {
  return (
    <div className="ia-page ia-error-state" role="alert">
      <section className="ia-error-card">
        <span className="ia-error-icon"><AdminIcon name="shield" /></span>
        <p className="ia-eyebrow">Protected administration</p>
        <h1>Administration could not be loaded</h1>
        <p>The protected server request failed. No mutation has been assumed successful and no authorization decision was inferred from the failure.</p>
        <div className="ia-error-actions"><button className="ia-button ia-button-primary" type="button" onClick={reset}>Try again</button><a className="ia-button ia-button-secondary" href="/login">Sign in again</a></div>
      </section>
    </div>
  );
}
