import type { ReactNode } from "react";
import { redirect } from "next/navigation";
import { AdminIcon } from "../../components/AdminIcon";
import { AdminNavigation } from "../../components/AdminNavigation";
import { IdentityAccessAdminRequest } from "../../server/IdentityAccessAdminRequest";
import { IdentityAccessHostSessionService } from "../../server/IdentityAccessHostSessionService";
import { logoutAction } from "../login/actions";

export default async function IdentityLayout({ children }: { readonly children: ReactNode }) {
  const hostSession = await IdentityAccessHostSessionService.fromCurrentRequest();
  if (!hostSession.hasBearerCredential()) redirect("/login");

  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const navigation = await request.adminUiBuilder().buildVisible();
  return (
    <div className="ia-shell">
      <AdminNavigation entries={navigation.entries} />
      <div className="ia-workspace">
        <header className="ia-topbar">
          <div className="ia-topbar-context">
            <span className="ia-topbar-icon"><AdminIcon name="shield" /></span>
            <div><strong>Protected administration</strong><small>Server Components · trusted Bearer context · RBAC enforced</small></div>
          </div>
          <div className="ia-topbar-actions">
            <div className="ia-topbar-status"><span className="ia-live-dot" aria-hidden="true" />Secure session active</div>
            <form action={logoutAction}>
              <button className="ia-button ia-button-secondary ia-signout-button" type="submit">Sign out</button>
            </form>
          </div>
        </header>
        <main className="ia-main">{children}</main>
      </div>
    </div>
  );
}
