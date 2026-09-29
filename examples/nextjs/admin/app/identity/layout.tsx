import type { ReactNode } from "react";
import { IdentityAccessClientError, type IdentityAccessAdminUiDefinition } from "@identity-access/client";
import { redirect } from "next/navigation";
import { AdminCurrentSection } from "../../components/AdminCurrentSection";
import { AdminIcon } from "../../components/AdminIcon";
import { AdminMobileNavigation } from "../../components/AdminMobileNavigation";
import { AdminSessionRefresh } from "../../components/AdminSessionRefresh";
import { AdminNavigation } from "../../components/AdminNavigation";
import { IdentityAccessAdminRequest } from "../../server/IdentityAccessAdminRequest";
import { IdentityAccessHostSessionService } from "../../server/IdentityAccessHostSessionService";
import { logoutAction } from "../login/actions";

export default async function IdentityLayout({ children }: { readonly children: ReactNode }) {
  const hostSession = await IdentityAccessHostSessionService.fromCurrentRequest();
  if (!hostSession.hasBearerCredential()) {
    redirect(hostSession.hasRefreshCredential() ? "/login?session=refresh" : "/login");
  }

  let navigation: IdentityAccessAdminUiDefinition;
  try {
    const request = await IdentityAccessAdminRequest.fromCurrentRequest();
    navigation = await request.adminUiBuilder().buildVisible();
  } catch (error) {
    if (error instanceof IdentityAccessClientError && error.code === "unauthenticated") {
      redirect("/login?session=expired");
    }
    throw error;
  }

  return (
    <div className="ia-shell">
      <AdminSessionRefresh />
      <a className="ia-skip-link" href="#identity-main">Skip to administration content</a>
      <AdminNavigation entries={navigation.entries} />
      <div className="ia-workspace">
        <header className="ia-topbar">
          <div className="ia-topbar-leading">
            <AdminMobileNavigation>
              <AdminNavigation entries={navigation.entries} mobile />
            </AdminMobileNavigation>
            <div className="ia-topbar-context">
              <span className="ia-topbar-icon"><AdminIcon name="shield" /></span>
              <AdminCurrentSection entries={navigation.entries} />
            </div>
          </div>
          <div className="ia-topbar-actions">
            <div className="ia-topbar-status"><span className="ia-live-dot" aria-hidden="true" />Secure session active</div>
            <form action={logoutAction}>
              <button className="ia-button ia-button-secondary ia-signout-button" type="submit">Sign out</button>
            </form>
          </div>
        </header>
        <main className="ia-main" id="identity-main" tabIndex={-1}>{children}</main>
      </div>
    </div>
  );
}
