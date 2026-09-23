import { AdminIcon } from "../../components/AdminIcon";
import { LoginForm } from "./LoginForm";

export default function LoginPage() {
  return (
    <main className="ia-login-shell">
      <section className="ia-login-panel" aria-labelledby="identity-login-heading">
        <div className="ia-login-brand">
          <span className="ia-login-brand-icon"><AdminIcon name="shield" /></span>
          <div>
            <p className="ia-eyebrow">Identity Access</p>
            <strong>Administration Console</strong>
          </div>
        </div>
        <div className="ia-login-copy">
          <span className="ia-login-security-mark"><AdminIcon name="lock" /></span>
          <p className="ia-eyebrow">Protected workspace</p>
          <h1 id="identity-login-heading">Control identity without exposing trust.</h1>
          <p>Credentials are exchanged server-side through the registered public OIDC client. Browser code never receives access, refresh, or local-session tokens.</p>
        </div>
        <div className="ia-login-protocols" aria-label="Authentication properties">
          <span>Authorization Code</span>
          <span>PKCE S256</span>
          <span>HTTP-only cookies</span>
          <span>RBAC enforced</span>
        </div>
      </section>
      <section className="ia-login-card">
        <div className="ia-login-card-header">
          <p className="ia-eyebrow">Administrator sign-in</p>
          <h2>Welcome back</h2>
          <p>Use a local Identity Access account registered for this administration client.</p>
        </div>
        <LoginForm />
        <p className="ia-login-footnote"><AdminIcon name="shield" /> Authentication remains server-only from password entry through OIDC token exchange.</p>
      </section>
    </main>
  );
}
