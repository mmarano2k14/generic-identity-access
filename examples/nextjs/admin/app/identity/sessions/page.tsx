import { AdminField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { revokeClientSessionsAction, revokeUserSessionsAction } from "../actions";

export default function SessionsPage() {
  return (
    <section className="ia-page">
      <AdminPageHeader title="Sessions" description="Security-sensitive revocation operations are confirmed and executed server-side." />
      <div className="ia-card-grid">
        <section className="ia-card">
          <h2>Revoke by user</h2>
          <p>Invalidate every active local session owned by one user.</p>
          <AdminMutationDialog title="Revoke user sessions" description="This immediately affects sessions linked to the selected user. Type REVOKE to confirm." triggerLabel="Revoke user sessions" submitLabel="Revoke sessions" action={revokeUserSessionsAction} dangerous>
            <AdminField label="User ID" name="userId" required autoComplete="off" />
            <AdminField label="Confirmation" name="confirmation" required autoComplete="off" placeholder="REVOKE" />
          </AdminMutationDialog>
        </section>
        <section className="ia-card">
          <h2>Revoke by client</h2>
          <p>Invalidate active sessions issued to one registered authentication client.</p>
          <AdminMutationDialog title="Revoke client sessions" description="This can sign out many users at once. Type REVOKE to confirm." triggerLabel="Revoke client sessions" submitLabel="Revoke sessions" action={revokeClientSessionsAction} dangerous>
            <AdminField label="Client ID" name="clientId" required autoComplete="off" />
            <AdminField label="Confirmation" name="confirmation" required autoComplete="off" placeholder="REVOKE" />
          </AdminMutationDialog>
        </section>
      </div>
    </section>
  );
}
