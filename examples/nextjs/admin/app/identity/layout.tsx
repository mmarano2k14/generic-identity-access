import type { ReactNode } from "react";
import { AdminNavigation } from "../../components/AdminNavigation";
import { IdentityAccessAdminRequest } from "../../server/IdentityAccessAdminRequest";
import "../../styles/identity-access-admin.css";

export default async function IdentityLayout({ children }: { readonly children: ReactNode }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const navigation = await request.adminUiBuilder().buildVisible();
  return (
    <div className="ia-shell">
      <AdminNavigation entries={navigation.entries} />
      <main className="ia-main">{children}</main>
    </div>
  );
}
