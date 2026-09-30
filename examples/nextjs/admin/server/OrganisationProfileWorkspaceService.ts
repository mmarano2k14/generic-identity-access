import "server-only";
import type {
  IdentityEffectiveOrganisationProfileRecord,
  IdentityOrganisationProfileDomainOverrideRecord,
  IdentityOrganisationProfileRecord,
  IdentityOrganisationProfileTemplateRecord,
  IdentityOrganisationProfileTemplateVersionRecord,
  IdentityOrganizationRecord,
} from "@identity-access/client";
import { IdentityAccessClientError } from "@identity-access/client";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

export interface OrganisationProfileWorkspacePermissions {
  readonly canReadOrganization: boolean;
  readonly canReadProfile: boolean;
  readonly canManageProfile: boolean;
  readonly canReadTemplates: boolean;
  readonly canReadDomainOverrides: boolean;
  readonly canManageDomainOverrides: boolean;
  readonly canReadEffectiveVersions: boolean;
  readonly canResolveEffectiveVersions: boolean;
}

export interface OrganisationProfileTemplateChoice {
  readonly templateKey: string;
  readonly displayName: string;
  readonly templateVersion: number;
  readonly domains: readonly { readonly domainKey: string; readonly domainVersion: number }[];
}

export interface OrganisationProfileDomainChoice {
  readonly domainKey: string;
  readonly publishedVersions: readonly number[];
  readonly knownVersions: readonly number[];
}

export interface OrganisationProfileWorkspace {
  readonly tenantId: string;
  readonly organization: IdentityOrganizationRecord | null;
  readonly profile: IdentityOrganisationProfileRecord | null;
  readonly templates: readonly IdentityOrganisationProfileTemplateRecord[];
  readonly templateVersions: readonly IdentityOrganisationProfileTemplateVersionRecord[];
  readonly templateChoices: readonly OrganisationProfileTemplateChoice[];
  readonly domainChoices: readonly OrganisationProfileDomainChoice[];
  readonly overrides: readonly IdentityOrganisationProfileDomainOverrideRecord[];
  readonly effectiveVersions: readonly IdentityEffectiveOrganisationProfileRecord[];
  readonly permissions: OrganisationProfileWorkspacePermissions;
}

/**
 * Loads only the application-owned OrganisationProfile workspace for one existing Organization.
 * Organization identity stays read-only here and remains owned by Organization Directory.
 */
export class OrganisationProfileWorkspaceService {
  static readonly #templateLimit = 100;
  static readonly #effectiveVersionLimit = 50;

  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async load(
    tenantIdValue: string,
    organizationIdValue: string,
  ): Promise<OrganisationProfileWorkspace> {
    const tenantContext = this.#request.tenantContextFor(tenantIdValue);
    const tenantAuthorization = this.#request.tenantAuthorizationFor(tenantIdValue);
    const scopeAuthorization = this.#request.scopeAuthorization();

    const [
      canReadOrganization,
      canReadProfile,
      canManageProfile,
      canReadTemplates,
      canReadDomainOverrides,
      canManageDomainOverrides,
      canReadEffectiveVersions,
      canResolveEffectiveVersions,
    ] = await Promise.all([
      tenantAuthorization.isAllowed("identity-access", "organization", "read"),
      tenantAuthorization.isAllowed("identity-access", "organisation-profile", "read"),
      tenantAuthorization.isAllowed("identity-access", "organisation-profile", "write"),
      scopeAuthorization.isAllowed("identity-access", "organisation-profile-template", "read"),
      tenantAuthorization.isAllowed("identity-access", "organisation-profile-domain-override", "read"),
      tenantAuthorization.isAllowed("identity-access", "organisation-profile-domain-override", "write"),
      tenantAuthorization.isAllowed("identity-access", "organisation-profile-effective-version", "read"),
      tenantAuthorization.isAllowed("identity-access", "organisation-profile-effective-version", "write"),
    ]);

    const permissions: OrganisationProfileWorkspacePermissions = {
      canReadOrganization,
      canReadProfile,
      canManageProfile,
      canReadTemplates,
      canReadDomainOverrides,
      canManageDomainOverrides,
      canReadEffectiveVersions,
      canResolveEffectiveVersions,
    };

    if (!canReadOrganization) {
      return OrganisationProfileWorkspaceService.empty(
        tenantContext.tenantId,
        permissions,
      );
    }

    const organization =
      await this.#request.client.administration.organizations.get(
        tenantContext,
        organizationIdValue,
      );

    if (organization === null) {
      throw new IdentityAccessClientError("http", 404);
    }

    const profile = canReadProfile
      ? await this.#request.client.administration.organisationProfiles.getByOrganization(
          tenantContext,
          organization.organizationId,
        )
      : null;

    const templates = canReadTemplates
      ? await this.#request.client.administration.organisationProfileTemplates.list(
          this.#request.administrationContext,
          { limit: OrganisationProfileWorkspaceService.#templateLimit },
        )
      : [];

    const activeTemplates = templates.filter((template) => template.status === 1);
    const templateVersions = canReadTemplates
      ? (
          await Promise.all(
            activeTemplates.map((template) =>
              this.#request.client.administration.organisationProfileTemplateVersions.list(
                this.#request.administrationContext,
                template.templateKey,
              ),
            ),
          )
        ).flat()
      : [];

    const immutableTemplateVersions = templateVersions.filter(
      (version) => version.status === 2 || version.status === 3,
    );

    const publishedTemplateVersions = immutableTemplateVersions.filter(
      (version) => version.status === 2,
    );

    const templateByKey = new Map(
      activeTemplates.map((template) => [template.templateKey, template]),
    );

    const templateChoices: readonly OrganisationProfileTemplateChoice[] =
      publishedTemplateVersions
        .map((version) => ({
          templateKey: version.templateKey,
          displayName:
            templateByKey.get(version.templateKey)?.displayName ?? version.templateKey,
          templateVersion: version.templateVersion,
          domains: version.domains,
        }))
        .sort((left, right) =>
          left.displayName.localeCompare(right.displayName) ||
          right.templateVersion - left.templateVersion,
        );

    const domainVersions = new Map<
      string,
      { readonly published: Set<number>; readonly known: Set<number> }
    >();

    for (const version of immutableTemplateVersions) {
      for (const domain of version.domains) {
        const entry = domainVersions.get(domain.domainKey) ?? {
          published: new Set<number>(),
          known: new Set<number>(),
        };

        entry.known.add(domain.domainVersion);
        if (version.status === 2) entry.published.add(domain.domainVersion);
        domainVersions.set(domain.domainKey, entry);
      }
    }

    const domainChoices: readonly OrganisationProfileDomainChoice[] =
      Array.from(domainVersions.entries())
        .map(([domainKey, versions]) => ({
          domainKey,
          publishedVersions: Array.from(versions.published).sort((a, b) => b - a),
          knownVersions: Array.from(versions.known).sort((a, b) => b - a),
        }))
        .sort((left, right) => left.domainKey.localeCompare(right.domainKey));

    const [overrides, effectiveVersions] = profile === null
      ? [[], []] as const
      : await Promise.all([
          canReadDomainOverrides
            ? this.#request.client.administration.organisationProfileDomainOverrides.list(
                tenantContext,
                profile.organisationProfileId,
              )
            : Promise.resolve([]),
          canReadEffectiveVersions
            ? this.#request.client.administration.organisationProfileEffectiveVersions.list(
                tenantContext,
                profile.organisationProfileId,
                { limit: OrganisationProfileWorkspaceService.#effectiveVersionLimit },
              )
            : Promise.resolve([]),
        ]);

    return {
      tenantId: tenantContext.tenantId,
      organization,
      profile,
      templates,
      templateVersions,
      templateChoices,
      domainChoices,
      overrides,
      effectiveVersions: [...effectiveVersions].sort(
        (left, right) => right.version - left.version,
      ),
      permissions,
    };
  }

  public static empty(
    tenantId: string,
    permissions: OrganisationProfileWorkspacePermissions,
  ): OrganisationProfileWorkspace {
    return {
      tenantId,
      organization: null,
      profile: null,
      templates: [],
      templateVersions: [],
      templateChoices: [],
      domainChoices: [],
      overrides: [],
      effectiveVersions: [],
      permissions,
    };
  }
}
