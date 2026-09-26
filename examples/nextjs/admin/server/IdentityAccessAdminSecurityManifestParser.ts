import "server-only";
import type {
  IdentityApplicationSecurityManifestAction,
  IdentityApplicationSecurityManifestFeature,
  IdentityApplicationSecurityManifestRequest,
  IdentityApplicationSecurityManifestResource,
} from "@identity-access/client";

/** Parses one project-owned JSON manifest file without turning the administration UI into a capability authoring surface. */
export class IdentityAccessAdminSecurityManifestParser {
  static readonly #maximumManifestBytes = 262_144;

  public async parse(formData: FormData): Promise<IdentityApplicationSecurityManifestRequest> {
    const entry = formData.get("manifestFile");
    if (!(entry instanceof File)) {
      throw new Error("Invalid administration input: a JSON security manifest file is required.");
    }
    if (entry.size <= 0) {
      throw new Error("Invalid administration input: the security manifest file is empty.");
    }
    if (entry.size > IdentityAccessAdminSecurityManifestParser.#maximumManifestBytes) {
      throw new Error("Invalid administration input: the security manifest file must not exceed 256 KiB.");
    }
    if (!entry.name.toLowerCase().endsWith(".json")) {
      throw new Error("Invalid administration input: the security manifest file must use the .json extension.");
    }

    let parsed: unknown;
    try {
      parsed = JSON.parse(await entry.text());
    } catch {
      throw new Error("Invalid administration input: the security manifest file is not valid JSON.");
    }

    const root = IdentityAccessAdminSecurityManifestParser.#object(parsed, "manifest");
    const schemaVersion = IdentityAccessAdminSecurityManifestParser.#integer(root.schemaVersion, "schemaVersion");
    if (schemaVersion !== 1) {
      throw new Error("Invalid administration input: schemaVersion must be 1.");
    }

    const applicationKey = IdentityAccessAdminSecurityManifestParser.#slug(root.applicationKey, "applicationKey");
    const modelVersion = IdentityAccessAdminSecurityManifestParser.#integer(root.modelVersion, "modelVersion");
    const rbac = IdentityAccessAdminSecurityManifestParser.#object(root.rbac, "rbac");
    const project = IdentityAccessAdminSecurityManifestParser.#rbacSegment(rbac.project, "rbac.project");
    const namespaces = IdentityAccessAdminSecurityManifestParser.#array(rbac.namespaces, "rbac.namespaces")
      .map((value, index) => IdentityAccessAdminSecurityManifestParser.#rbacSegment(value, `rbac.namespaces[${index}]`));
    if (namespaces.length === 0) {
      throw new Error("Invalid administration input: rbac.namespaces must contain at least one namespace.");
    }
    if (new Set(namespaces).size !== namespaces.length) {
      throw new Error("Invalid administration input: rbac.namespaces must not contain duplicates.");
    }

    const resources = IdentityAccessAdminSecurityManifestParser.#resources(root.resources);
    const capabilityKeys = new Set<string>();
    for (const resource of resources) {
      for (const feature of resource.features) {
        for (const action of feature.actions) {
          const key = `${resource.name}:${feature.name}:${action.name}`;
          if (capabilityKeys.has(key)) {
            throw new Error(`Invalid administration input: duplicate capability ${key}.`);
          }
          capabilityKeys.add(key);
        }
      }
    }

    return {
      schemaVersion: 1,
      applicationKey,
      modelVersion,
      rbac: { project, namespaces },
      resources,
    };
  }

  static #resources(value: unknown): readonly IdentityApplicationSecurityManifestResource[] {
    const entries = IdentityAccessAdminSecurityManifestParser.#array(value, "resources");
    if (entries.length === 0) {
      throw new Error("Invalid administration input: resources must contain at least one resource.");
    }

    return entries.map((entry, resourceIndex) => {
      const resource = IdentityAccessAdminSecurityManifestParser.#object(entry, `resources[${resourceIndex}]`);
      const name = IdentityAccessAdminSecurityManifestParser.#slug(resource.name, `resources[${resourceIndex}].name`);
      const features = IdentityAccessAdminSecurityManifestParser.#features(resource.features, resourceIndex);
      return { name, features };
    });
  }

  static #features(value: unknown, resourceIndex: number): readonly IdentityApplicationSecurityManifestFeature[] {
    const entries = IdentityAccessAdminSecurityManifestParser.#array(value, `resources[${resourceIndex}].features`);
    if (entries.length === 0) {
      throw new Error(`Invalid administration input: resources[${resourceIndex}].features must contain at least one feature.`);
    }

    return entries.map((entry, featureIndex) => {
      const feature = IdentityAccessAdminSecurityManifestParser.#object(entry, `resources[${resourceIndex}].features[${featureIndex}]`);
      const name = IdentityAccessAdminSecurityManifestParser.#slug(feature.name, `resources[${resourceIndex}].features[${featureIndex}].name`);
      const actions = IdentityAccessAdminSecurityManifestParser.#actions(feature.actions, resourceIndex, featureIndex);
      return { name, actions };
    });
  }

  static #actions(value: unknown, resourceIndex: number, featureIndex: number): readonly IdentityApplicationSecurityManifestAction[] {
    const path = `resources[${resourceIndex}].features[${featureIndex}].actions`;
    const entries = IdentityAccessAdminSecurityManifestParser.#array(value, path);
    if (entries.length === 0) {
      throw new Error(`Invalid administration input: ${path} must contain at least one action.`);
    }

    return entries.map((entry, actionIndex) => {
      const actionPath = `${path}[${actionIndex}]`;
      const action = IdentityAccessAdminSecurityManifestParser.#object(entry, actionPath);
      return {
        name: IdentityAccessAdminSecurityManifestParser.#slug(action.name, `${actionPath}.name`),
        displayName: IdentityAccessAdminSecurityManifestParser.#displayName(action.displayName, `${actionPath}.displayName`),
      };
    });
  }

  static #object(value: unknown, path: string): Record<string, unknown> {
    if (typeof value !== "object" || value === null || Array.isArray(value)) {
      throw new Error(`Invalid administration input: ${path} must be a JSON object.`);
    }
    return value as Record<string, unknown>;
  }

  static #array(value: unknown, path: string): readonly unknown[] {
    if (!Array.isArray(value)) {
      throw new Error(`Invalid administration input: ${path} must be a JSON array.`);
    }
    return value;
  }

  static #integer(value: unknown, path: string): number {
    if (typeof value !== "number" || !Number.isSafeInteger(value) || value <= 0) {
      throw new Error(`Invalid administration input: ${path} must be a positive integer.`);
    }
    return value;
  }

  static #slug(value: unknown, path: string): string {
    if (typeof value !== "string") {
      throw new Error(`Invalid administration input: ${path} must be a string.`);
    }
    const normalized = value.trim().toLowerCase();
    if (!/^[a-z][a-z0-9-]{0,63}$/u.test(normalized)) {
      throw new Error(`Invalid administration input: ${path} must be a lowercase slug of at most 64 characters.`);
    }
    return normalized;
  }

  static #rbacSegment(value: unknown, path: string): string {
    if (typeof value !== "string") {
      throw new Error(`Invalid administration input: ${path} must be a string.`);
    }
    const normalized = value.trim().toLowerCase();
    if (!normalized || normalized.length > 128 || normalized.includes(":") || normalized.includes("*")) {
      throw new Error(`Invalid administration input: ${path} must be a concrete RBAC context segment of at most 128 characters.`);
    }
    return normalized;
  }

  static #displayName(value: unknown, path: string): string {
    if (typeof value !== "string" || value.trim().length === 0 || value !== value.trim() || value.length > 200 || /[\u0000-\u001f\u007f]/u.test(value)) {
      throw new Error(`Invalid administration input: ${path} must contain between 1 and 200 printable trimmed characters.`);
    }
    return value;
  }
}
