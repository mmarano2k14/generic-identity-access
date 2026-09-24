import { AdminIcon } from "../../components/AdminIcon";
import { RecoveryForm } from "./RecoveryForm";

export default function RecoveryPage() {
  return (
    <main className="ia-login-shell ia-recovery-shell">
      <section className="ia-login-panel ia-recovery-panel" aria-labelledby="identity-recovery-heading">
        <div className="ia-login-brand">
          <span className="ia-login-brand-icon"><AdminIcon name="shield" /></span>
          <div><p className="ia-eyebrow">Identity Access</p><strong>Account recovery</strong></div>
        </div>
        <div className="ia-login-copy">
          <span className="ia-login-security-mark"><AdminIcon name="key" /></span>
          <p className="ia-eyebrow">Recovery-code proof</p>
          <h1 id="identity-recovery-heading">Replace a password without creating a weaker recovery path.</h1>
          <p>The server resolves the active recovery authenticator, consumes one valid code, replaces the credential, clears lockout state, and revokes existing local and refresh-token sessions.</p>
          <div className="ia-auth-trust-list" aria-label="Recovery protections">
            <span><AdminIcon name="check" />Recovery proof never enters the URL</span>
            <span><AdminIcon name="check" />One-time code consumption</span>
            <span><AdminIcon name="check" />Existing sessions invalidated</span>
          </div>
        </div>
        <div className="ia-login-protocols" aria-label="Recovery properties">
          <span>Hash-only code storage</span><span>Atomic mutation</span><span>Session revocation</span>
        </div>
      </section>
      <section className="ia-login-card ia-recovery-card">
        <div className="ia-login-card-header">
          <p className="ia-eyebrow">Recover access</p>
          <h2>Set a new password</h2>
          <p>Use a recovery code issued for your account. Invalid account, authenticator, and code states intentionally receive the same public rejection.</p>
        </div>
        <div className="ia-auth-security-note"><AdminIcon name="shield" /><span>Successful recovery revokes existing sessions. You will sign in again with the replacement password.</span></div>
        <RecoveryForm />
      </section>
    </main>
  );
}
