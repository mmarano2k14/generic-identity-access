import {
  createIdentityClient,
  type IdentityAdministrationContext,
  type IdentityTenantAdministrationContext,
} from "@generic-identity/auth";

const client = createIdentityClient({ baseUrl: "https://identity.example.test/" });

declare const administrationContext: IdentityAdministrationContext;
declare const tenantContext: IdentityTenantAdministrationContext;

void client.administration.context.get(administrationContext);
void client.administration.users.list(administrationContext);
void client.administration.tenants.list(administrationContext);
void client.administration.tenantUsers.list(tenantContext);
void client.administration.groups.list(tenantContext);
void client.administration.managedPolicies.list(administrationContext);
void client.administration.mfa.getPolicy(administrationContext);
