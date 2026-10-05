import type { ReactNode } from "react";
import type { IdentityApplicationSecurityCapabilityRecord } from "@generic-identity/contracts/application-security";
import { IdentityEmptyState, IdentityPageFrame, IdentityTable } from "../components/index";

export interface ApplicationCapabilitiesPageProps {
  readonly capabilities: readonly IdentityApplicationSecurityCapabilityRecord[];
  readonly applicationKey?: string;
  readonly modelVersion?: number;
  readonly actions?: ReactNode;
}

export function ApplicationCapabilitiesPage({ capabilities, applicationKey, modelVersion, actions }: ApplicationCapabilitiesPageProps) {
  const description = applicationKey
    ? `Capability catalog for ${applicationKey}${modelVersion ? ` security model v${modelVersion}` : ""}.`
    : "Capability catalog declared by the registered application security model.";

  return (
    <IdentityPageFrame title="Application capabilities" description={description} actions={actions}>
      {capabilities.length === 0 ? <IdentityEmptyState title="No capabilities" /> : (
        <IdentityTable caption="Application capabilities">
          <thead><tr><th scope="col">Resource</th><th scope="col">Feature</th><th scope="col">Action</th><th scope="col">Display name</th></tr></thead>
          <tbody>{capabilities.map((capability) => <tr key={`${capability.resource}:${capability.feature}:${capability.action}`}><td><code>{capability.resource}</code></td><td><code>{capability.feature}</code></td><td><code>{capability.action}</code></td><td>{capability.displayName}</td></tr>)}</tbody>
        </IdentityTable>
      )}
    </IdentityPageFrame>
  );
}
