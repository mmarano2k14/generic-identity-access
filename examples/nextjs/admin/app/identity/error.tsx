"use client";

export default function IdentityError({ reset }: { readonly reset: () => void }) {
  return (
    <div className="ia-page ia-error-state" role="alert">
      <p className="ia-eyebrow">Identity Access</p>
      <h1>Administration could not be loaded</h1>
      <p>The protected request failed. No mutation has been assumed successful.</p>
      <button className="ia-button ia-button-primary" type="button" onClick={reset}>Try again</button>
    </div>
  );
}
