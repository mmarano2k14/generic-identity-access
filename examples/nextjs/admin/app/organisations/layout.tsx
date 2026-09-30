import type { ReactNode } from "react";
import Link from "next/link";
import { redirect } from "next/navigation";
import { AdminIcon } from "../../components/AdminIcon";
import { AdminSessionRefresh } from "../../components/AdminSessionRefresh";
import { IdentityAccessHostSessionService } from "../../server/IdentityAccessHostSessionService";
import { logoutAction } from "../login/actions";

/**
 * Reference consuming-application shell for Organization semantic configuration.
 * It is deliberately outside the /identity administration navigation.
 */
export default async function OrganisationsLayout({
  children,
}: {
  readonly children: ReactNode;
}) {
  const hostSession = await IdentityAccessHostSessionService.fromCurrentRequest();
  if (!hostSession.hasBearerCredential()) {
    redirect(hostSession.hasRefreshCredential() ? "/login?session=refresh" : "/login");
  }

  return (
    <div className="ia-workspace">
      <AdminSessionRefresh />
      <a className="ia-skip-link" href="#organisation-main">Skip to profile administration</a>
      <header className="ia-topbar">
        <div className="ia-topbar-leading">
          <div className="ia-topbar-context">
            <span className="ia-topbar-icon"><AdminIcon name="spark" /></span>
            <div>
              <strong>Organization semantic profile</strong>
              <small>Application-owned configuration · Organization identity remains external</small>
            </div>
          </div>
        </div>
        <div className="ia-topbar-actions">
          <Link className="ia-button ia-button-secondary" href="/">Application home</Link>
          <form action={logoutAction}>
            <button className="ia-button ia-button-secondary ia-signout-button" type="submit">Sign out</button>
          </form>
        </div>
      </header>
      <main className="ia-main" id="organisation-main" tabIndex={-1}>{children}</main>
    </div>
  );
}
