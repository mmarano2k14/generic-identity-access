import type { IdentityAdministrationListOptions, IdentityEffectiveOrganisationProfileRecord, IdentityResolveOrganisationProfileRequest, IdentityTenantAdministrationContext } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Immutable OrganisationProfile semantic-version query and resolution client. */
export declare class IdentityAccessOrganisationProfileEffectiveVersionsClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityTenantAdministrationContext, organisationProfileIdValue: string, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityEffectiveOrganisationProfileRecord[]>;
    get(context: IdentityTenantAdministrationContext, organisationProfileIdValue: string, version: number, signal?: AbortSignal): Promise<IdentityEffectiveOrganisationProfileRecord | null>;
    resolve(context: IdentityTenantAdministrationContext, organisationProfileIdValue: string, request: IdentityResolveOrganisationProfileRequest, signal?: AbortSignal): Promise<IdentityEffectiveOrganisationProfileRecord>;
}
