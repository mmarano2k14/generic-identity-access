import type { ReactNode } from "react";
import type { IdentityEffectiveAdministrationContext } from "@generic-identity/contracts/application-security";
import { IdentityPageFrame, IdentityPanel, IdentityTable, IdentityEmptyState } from "../components/index";

export interface ApplicationSecurityContextPageProps {
  readonly context: IdentityEffectiveAdministrationContext;
  readonly actions?: ReactNode;
}

export function ApplicationSecurityContextPage({ context, actions }: ApplicationSecurityContextPageProps) {
  return (
    <IdentityPageFrame title="Administration context" description="Server-trusted effective administration boundary for the current credential." actions={actions}>
      <IdentityPanel title="Effective context">
        <dl className="gi-definition-list">
          <div><dt>Identity scope</dt><dd><code>{context.identityScopeId}</code></dd></div>
          <div><dt>User</dt><dd><code>{context.userId}</code></dd></div>
          <div><dt>Application</dt><dd><code>{context.applicationKey}</code></dd></div>
          <div><dt>Tenant visibility</dt><dd>{context.tenantVisibility}</dd></div>
        </dl>
      </IdentityPanel>
      <IdentityPanel title="Active tenant memberships">
        {context.activeTenantMemberships.length === 0 ? <IdentityEmptyState title="No active tenant memberships" /> : (
          <IdentityTable caption="Active tenant memberships"><thead><tr><th scope="col">Membership ID</th><th scope="col">Tenant ID</th></tr></thead><tbody>{context.activeTenantMemberships.map((membership) => <tr key={membership.membershipId}><td><code>{membership.membershipId}</code></td><td><code>{membership.tenantId}</code></td></tr>)}</tbody></IdentityTable>
        )}
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
