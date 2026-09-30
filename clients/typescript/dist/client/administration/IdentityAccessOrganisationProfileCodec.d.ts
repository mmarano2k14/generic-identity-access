import type { IdentityEffectiveOrganisationProfileRecord, IdentityOrganisationProfileDomainOverrideRecord, IdentityOrganisationProfileDomainSelectionRecord, IdentityOrganisationProfileRecord, IdentityOrganisationProfileTemplateRecord, IdentityOrganisationProfileTemplateVersionRecord } from "../../admin-contracts.js";
/** Focused OrganisationProfile administration DTO decoder. */
export declare class IdentityAccessOrganisationProfileCodec {
    static profileRecord(value: unknown): IdentityOrganisationProfileRecord;
    static templateRecord(value: unknown): IdentityOrganisationProfileTemplateRecord;
    static templateVersionRecord(value: unknown): IdentityOrganisationProfileTemplateVersionRecord;
    static domainOverride(value: unknown): IdentityOrganisationProfileDomainOverrideRecord;
    static effectiveProfileRecord(value: unknown): IdentityEffectiveOrganisationProfileRecord;
    static domainSelection(value: unknown): IdentityOrganisationProfileDomainSelectionRecord;
    private static optionalTemplatePin;
    private static profileStatus;
    private static templateStatus;
    private static templateVersionStatus;
    private static overrideOperation;
    private static oneOf;
}
