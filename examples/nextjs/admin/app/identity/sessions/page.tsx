import { AdminField } from "../../../components/AdminField";
import { AdminIcon } from "../../../components/AdminIcon";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { revokeClientSessionsAction, revokeUserSessionsAction } from "../actions";

export default function SessionsPage() {
  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Security" badge="Destructive operations" title="Sessions" description="Revoke active authentication sessions deliberately. Every operation is confirmed server-side and never assumes optimistic success." />
      <AdminSecurityBanner title="Revocation takes effect through current session validation." description="Bearer administration requests revalidate the referenced local session and active user, so revoked sessions stop authorizing protected calls." />
      <div className="ia-security-action-grid">
        <section className="ia-security-action-card">
          <span className="ia-security-action-icon"><AdminIcon name="users" /></span>
          <div className="ia-security-action-copy">
            <p className="ia-card-kicker">User containment</p>
            <h2>Revoke by user</h2>
            <p>Invalidate every active local session owned by one user without changing the identity record itself.</p>
          </div>
          <AdminMutationDialog title="Revoke user sessions" description="This immediately affects sessions linked to the selected user. Type REVOKE to confirm." triggerLabel="Revoke user sessions" submitLabel="Revoke sessions" action={revokeUserSessionsAction} dangerous>
            <AdminField label="User ID" name="userId" required autoComplete="off" />
            <AdminField label="Confirmation" name="confirmation" required autoComplete="off" placeholder="REVOKE" />
          </AdminMutationDialog>
        </section>
        <section className="ia-security-action-card ia-security-action-card-danger">
          <span className="ia-security-action-icon"><AdminIcon name="lock" /></span>
          <div className="ia-security-action-copy">
            <p className="ia-card-kicker">Client containment</p>
            <h2>Revoke by client</h2>
            <p>Invalidate active sessions issued to a registered authentication client. This may sign out many users at once.</p>
          </div>
          <AdminMutationDialog title="Revoke client sessions" description="This can sign out many users at once. Type REVOKE to confirm." triggerLabel="Revoke client sessions" submitLabel="Revoke sessions" action={revokeClientSessionsAction} dangerous>
            <AdminField label="Client ID" name="clientId" required autoComplete="off" />
            <AdminField label="Confirmation" name="confirmation" required autoComplete="off" placeholder="REVOKE" />
          </AdminMutationDialog>
        </section>
      </div>
    </section>
  );
}
