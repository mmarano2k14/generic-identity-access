import { AdminIcon } from "../../components/AdminIcon";
import { AdminSessionRecovery } from "../../components/AdminSessionRecovery";
import { IdentityAccessHostSessionService } from "../../server/IdentityAccessHostSessionService";
import { LoginForm } from "./LoginForm";

export default async function LoginPage({ searchParams }: { readonly searchParams: Promise<{ readonly recovered?: string; readonly session?: string }> }) {
  const { recovered, session } = await searchParams;
  const hostSession = await IdentityAccessHostSessionService.fromCurrentRequest();
  const canRecoverSession = session === "refresh" && hostSession.hasRefreshCredential();

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
          <h1 id="identity-login-heading">Security controls with a deliberately small trust surface.</h1>
          <p>Credentials are exchanged server-side through the registered public OIDC client. Browser code never receives access, refresh, or local-session tokens.</p>
          <div className="ia-auth-trust-list" aria-label="Security architecture">
            <span><AdminIcon name="check" />Server-owned token exchange</span>
            <span><AdminIcon name="check" />Session assurance preserved</span>
            <span><AdminIcon name="check" />RBAC revalidated on protected calls</span>
          </div>
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
          <p>Use an Identity Access account registered for this administration client.</p>
        </div>
        {recovered === "1" ? (
          <div className="ia-auth-success" role="status"><AdminIcon name="check" /><span>Your password was replaced and existing sessions were revoked. Sign in with the new password.</span></div>
        ) : null}
        {session === "expired" ? (
          <div className="ia-auth-notice" role="status"><AdminIcon name="shield" /><span>Your administrative session is no longer valid. Sign in again to continue with a fresh server-authorized context.</span></div>
        ) : null}
        {canRecoverSession ? <AdminSessionRecovery /> : <LoginForm />}
        <p className="ia-login-footnote"><AdminIcon name="shield" /> Authentication remains server-only from password entry through OIDC token exchange.</p>
      </section>
    </main>
  );
}
