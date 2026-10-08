import type { ReactNode } from "react";
import type {
  IdentityOrganizationMembershipRecord,
  IdentityOrganizationRecord,
  IdentityOrganizationResourceScopeLinkRecord,
} from "@generic-identity/contracts/organizations";
import { IdentityEmptyState, IdentityPageFrame, IdentityPanel, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface OrganizationDetailsPageProps {
  readonly organization: IdentityOrganizationRecord;
  readonly children?: readonly IdentityOrganizationRecord[];
  readonly memberships?: readonly IdentityOrganizationMembershipRecord[];
  readonly resourceScopeLink?: IdentityOrganizationResourceScopeLinkRecord | null;
  readonly actions?: ReactNode;
  readonly membershipActions?: ReactNode;
  readonly renderMembershipActions?: (membership: IdentityOrganizationMembershipRecord) => ReactNode;
  readonly resourceScopeActions?: ReactNode;
}

export function OrganizationDetailsPage({ organization, children = [], memberships = [], resourceScopeLink = null, actions, membershipActions, renderMembershipActions, resourceScopeActions }: OrganizationDetailsPageProps) {
  return (
    <IdentityPageFrame title={organization.displayName} description={`Organization ${organization.organizationKey}`} actions={actions}>
      <IdentityPanel title="Organization identity">
        <dl className="gi-definition-list">
          <div><dt>Organization ID</dt><dd><code>{organization.organizationId}</code></dd></div>
          <div><dt>Tenant ID</dt><dd><code>{organization.tenantId}</code></dd></div>
          <div><dt>Type</dt><dd>{organization.organizationType}</dd></div>
          <div><dt>Parent</dt><dd>{organization.parentOrganizationId ? <code>{organization.parentOrganizationId}</code> : "None"}</dd></div>
          <div><dt>Status</dt><dd>{activeStatus(organization.status)}</dd></div>
          <div><dt>Version</dt><dd>{organization.rowVersion}</dd></div>
        </dl>
      </IdentityPanel>
      <IdentityPanel title="Children">
        {children.length === 0 ? <IdentityEmptyState title="No child organizations" /> : <IdentityTable caption="Child organizations"><thead><tr><th scope="col">Name</th><th scope="col">Key</th><th scope="col">Type</th><th scope="col">Status</th></tr></thead><tbody>{children.map((child) => <tr key={child.organizationId}><td>{child.displayName}</td><td><code>{child.organizationKey}</code></td><td>{child.organizationType}</td><td>{activeStatus(child.status)}</td></tr>)}</tbody></IdentityTable>}
      </IdentityPanel>
      <IdentityPanel title="Memberships">
        {membershipActions ? <div className="gi-panel-actions">{membershipActions}</div> : null}
        {memberships.length === 0 ? <IdentityEmptyState title="No organization memberships" /> : <IdentityTable caption="Organization memberships"><thead><tr><th scope="col">Tenant membership ID</th><th scope="col">Status</th><th scope="col">Version</th>{renderMembershipActions ? <th scope="col">Actions</th> : null}</tr></thead><tbody>{memberships.map((membership) => <tr key={membership.tenantMembershipId}><td><code>{membership.tenantMembershipId}</code></td><td>{activeStatus(membership.status)}</td><td>{membership.rowVersion}</td>{renderMembershipActions ? <td>{renderMembershipActions(membership)}</td> : null}</tr>)}</tbody></IdentityTable>}
      </IdentityPanel>
      <IdentityPanel title="Resource scope">
        {resourceScopeActions ? <div className="gi-panel-actions">{resourceScopeActions}</div> : null}
        {resourceScopeLink ? <dl className="gi-definition-list"><div><dt>Resource scope ID</dt><dd><code>{resourceScopeLink.resourceScopeId}</code></dd></div><div><dt>Scope type</dt><dd>{resourceScopeLink.scopeType}</dd></div><div><dt>Application</dt><dd><code>{resourceScopeLink.applicationKey}</code></dd></div><div><dt>Model version</dt><dd>{resourceScopeLink.modelVersion}</dd></div><div><dt>Status</dt><dd>{activeStatus(resourceScopeLink.status)}</dd></div><div><dt>Version</dt><dd>{resourceScopeLink.rowVersion}</dd></div></dl> : <IdentityEmptyState title="No resource scope linked" description="This Organization has no application ResourceScope link in the current context." />}
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
