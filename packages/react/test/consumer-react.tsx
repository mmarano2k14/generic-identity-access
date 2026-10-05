import type { GenericIdentityAuthorizationContext, GenericIdentityClient } from "@generic-identity/auth";
import type { IdentityCapabilityRequirement } from "@generic-identity/contracts";
import {
  IdentityProvider,
  RequireCapability,
  useAuthorization,
  useCapability,
  useIdentityClient,
} from "@generic-identity/react";

const requirement: IdentityCapabilityRequirement = {
  resource: "billing",
  feature: "invoice",
  action: "read",
};

function ConsumerProbe() {
  const client: GenericIdentityClient = useIdentityClient();
  const authorization: GenericIdentityAuthorizationContext = useAuthorization();
  const capability = useCapability(requirement, { enabled: true });

  void client;
  void authorization;
  void capability.refresh;

  return (
    <RequireCapability
      requirement={requirement}
      loadingFallback={<span>Checking</span>}
      deniedFallback={<span>Denied</span>}
      errorFallback={(error: unknown) => <span>{String(error)}</span>}
    >
      <button type="button">Read invoice</button>
    </RequireCapability>
  );
}

export function ReactConsumerProbe(props: {
  readonly client: GenericIdentityClient;
  readonly authorizationContext: GenericIdentityAuthorizationContext;
}) {
  return (
    <IdentityProvider client={props.client} authorizationContext={props.authorizationContext}>
      <ConsumerProbe />
    </IdentityProvider>
  );
}
