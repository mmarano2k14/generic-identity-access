import type { ReactElement } from "react";
import {
  ApplicationCapabilitiesPage,
  ApplicationScopeTypesPage,
  ApplicationSecurityContextPage,
  ApplicationSecurityModelDetailsPage,
  ApplicationSecurityModelsPage,
} from "@generic-identity/react/application-security";
import type {
  IdentityApplicationSecurityModelRecord,
  IdentityEffectiveAdministrationContext,
  IdentityScopeTypeRecord,
} from "@generic-identity/contracts/application-security";

export function renderApplicationSecurity(
  model: IdentityApplicationSecurityModelRecord,
  scopeTypes: readonly IdentityScopeTypeRecord[],
  context: IdentityEffectiveAdministrationContext,
): readonly ReactElement[] {
  return [
    <ApplicationSecurityModelsPage models={[model]} />,
    <ApplicationSecurityModelDetailsPage model={model} />,
    <ApplicationCapabilitiesPage capabilities={model.capabilities} applicationKey={model.applicationKey} modelVersion={model.modelVersion} />,
    <ApplicationScopeTypesPage scopeTypes={scopeTypes} />,
    <ApplicationSecurityContextPage context={context} />,
  ];
}
