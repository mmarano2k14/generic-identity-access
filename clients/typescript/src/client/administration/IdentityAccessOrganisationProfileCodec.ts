import type {
  IdentityEffectiveOrganisationProfileRecord,
  IdentityOrganisationProfileDomainOverrideRecord,
  IdentityOrganisationProfileDomainSelectionRecord,
  IdentityOrganisationProfileRecord,
  IdentityOrganisationProfileTemplatePinRecord,
  IdentityOrganisationProfileTemplateRecord,
  IdentityOrganisationProfileTemplateVersionRecord,
} from "../../admin-contracts.js";
import { IdentityAccessClientError } from "../../errors.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";

/** Focused OrganisationProfile administration DTO decoder. */
export class IdentityAccessOrganisationProfileCodec {
  public static profileRecord(value: unknown): IdentityOrganisationProfileRecord {
    const data = IdentityAccessValueCodec.object(value);
    const templatePin = IdentityAccessOrganisationProfileCodec.optionalTemplatePin(data.templatePin);

    return {
      organisationProfileId: IdentityAccessValueCodec.uuid(
        IdentityAccessValueCodec.text(data.organisationProfileId),
      ),
      identityScopeId: IdentityAccessValueCodec.uuid(
        IdentityAccessValueCodec.text(data.identityScopeId),
      ),
      tenantId: IdentityAccessValueCodec.uuid(
        IdentityAccessValueCodec.text(data.tenantId),
      ),
      organizationId: IdentityAccessValueCodec.uuid(
        IdentityAccessValueCodec.text(data.organizationId),
      ),
      ...(templatePin === undefined ? {} : { templatePin }),
      status: IdentityAccessOrganisationProfileCodec.profileStatus(data.status),
      rowVersion: IdentityAccessValueCodec.version(data.rowVersion),
      createdAt: IdentityAccessValueCodec.timestamp(data.createdAt),
      updatedAt: IdentityAccessValueCodec.timestamp(data.updatedAt),
    };
  }

  public static templateRecord(value: unknown): IdentityOrganisationProfileTemplateRecord {
    const data = IdentityAccessValueCodec.object(value);

    return {
      templateKey: IdentityAccessValueCodec.slug(
        IdentityAccessValueCodec.text(data.templateKey),
      ),
      displayName: IdentityAccessValueCodec.text(data.displayName),
      status: IdentityAccessOrganisationProfileCodec.templateStatus(data.status),
      rowVersion: IdentityAccessValueCodec.version(data.rowVersion),
      createdAt: IdentityAccessValueCodec.timestamp(data.createdAt),
      updatedAt: IdentityAccessValueCodec.timestamp(data.updatedAt),
    };
  }

  public static templateVersionRecord(
    value: unknown,
  ): IdentityOrganisationProfileTemplateVersionRecord {
    const data = IdentityAccessValueCodec.object(value);
    const contentHash = data.contentHash === null || data.contentHash === undefined
      ? undefined
      : IdentityAccessValueCodec.sha256(data.contentHash);
    const publishedAt = data.publishedAt === null || data.publishedAt === undefined
      ? undefined
      : IdentityAccessValueCodec.timestamp(data.publishedAt);
    const retiredAt = data.retiredAt === null || data.retiredAt === undefined
      ? undefined
      : IdentityAccessValueCodec.timestamp(data.retiredAt);

    return {
      templateKey: IdentityAccessValueCodec.slug(
        IdentityAccessValueCodec.text(data.templateKey),
      ),
      templateVersion: IdentityAccessValueCodec.positiveInteger(data.templateVersion),
      status: IdentityAccessOrganisationProfileCodec.templateVersionStatus(data.status),
      domains: IdentityAccessValueCodec.array(
        data.domains,
        IdentityAccessOrganisationProfileCodec.domainSelection,
      ),
      ...(contentHash === undefined ? {} : { contentHash }),
      rowVersion: IdentityAccessValueCodec.version(data.rowVersion),
      createdAt: IdentityAccessValueCodec.timestamp(data.createdAt),
      updatedAt: IdentityAccessValueCodec.timestamp(data.updatedAt),
      ...(publishedAt === undefined ? {} : { publishedAt }),
      ...(retiredAt === undefined ? {} : { retiredAt }),
    };
  }

  public static domainOverride(value: unknown): IdentityOrganisationProfileDomainOverrideRecord {
    const data = IdentityAccessValueCodec.object(value);
    const operation = IdentityAccessOrganisationProfileCodec.overrideOperation(data.operation);
    const domainVersion = data.domainVersion === null || data.domainVersion === undefined
      ? undefined
      : IdentityAccessValueCodec.positiveInteger(data.domainVersion);

    if (operation === 1 && domainVersion === undefined) {
      throw new IdentityAccessClientError("protocol");
    }

    if (operation === 2 && domainVersion !== undefined) {
      throw new IdentityAccessClientError("protocol");
    }

    return {
      domainKey: IdentityAccessValueCodec.slug(
        IdentityAccessValueCodec.text(data.domainKey),
      ),
      ...(domainVersion === undefined ? {} : { domainVersion }),
      operation,
    };
  }

  public static effectiveProfileRecord(value: unknown): IdentityEffectiveOrganisationProfileRecord {
    const data = IdentityAccessValueCodec.object(value);
    const templatePin = IdentityAccessOrganisationProfileCodec.optionalTemplatePin(data.templatePin);

    return {
      organisationProfileId: IdentityAccessValueCodec.uuid(
        IdentityAccessValueCodec.text(data.organisationProfileId),
      ),
      identityScopeId: IdentityAccessValueCodec.uuid(
        IdentityAccessValueCodec.text(data.identityScopeId),
      ),
      tenantId: IdentityAccessValueCodec.uuid(
        IdentityAccessValueCodec.text(data.tenantId),
      ),
      organizationId: IdentityAccessValueCodec.uuid(
        IdentityAccessValueCodec.text(data.organizationId),
      ),
      version: IdentityAccessValueCodec.positiveInteger(data.version),
      ...(templatePin === undefined ? {} : { templatePin }),
      domains: IdentityAccessValueCodec.array(
        data.domains,
        IdentityAccessOrganisationProfileCodec.domainSelection,
      ),
      contentHash: IdentityAccessValueCodec.sha256(data.contentHash),
      resolvedAt: IdentityAccessValueCodec.timestamp(data.resolvedAt),
    };
  }

  public static domainSelection(value: unknown): IdentityOrganisationProfileDomainSelectionRecord {
    const data = IdentityAccessValueCodec.object(value);

    return {
      domainKey: IdentityAccessValueCodec.slug(
        IdentityAccessValueCodec.text(data.domainKey),
      ),
      domainVersion: IdentityAccessValueCodec.positiveInteger(data.domainVersion),
    };
  }

  private static optionalTemplatePin(
    value: unknown,
  ): IdentityOrganisationProfileTemplatePinRecord | undefined {
    if (value === null || value === undefined) return undefined;

    const data = IdentityAccessValueCodec.object(value);
    return {
      templateKey: IdentityAccessValueCodec.slug(
        IdentityAccessValueCodec.text(data.templateKey),
      ),
      templateVersion: IdentityAccessValueCodec.positiveInteger(data.templateVersion),
    };
  }

  private static profileStatus(value: unknown): 1 | 2 {
    return IdentityAccessOrganisationProfileCodec.oneOf(value, [1, 2]);
  }

  private static templateStatus(value: unknown): 1 | 2 {
    return IdentityAccessOrganisationProfileCodec.oneOf(value, [1, 2]);
  }

  private static templateVersionStatus(value: unknown): 1 | 2 | 3 {
    return IdentityAccessOrganisationProfileCodec.oneOf(value, [1, 2, 3]);
  }

  private static overrideOperation(value: unknown): 1 | 2 {
    return IdentityAccessOrganisationProfileCodec.oneOf(value, [1, 2]);
  }

  private static oneOf<const T extends readonly number[]>(
    value: unknown,
    allowed: T,
  ): T[number] {
    if (typeof value !== "number" || !allowed.includes(value)) {
      throw new IdentityAccessClientError("protocol");
    }

    return value as T[number];
  }
}
