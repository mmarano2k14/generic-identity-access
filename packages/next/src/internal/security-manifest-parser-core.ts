import type {
  IdentityApplicationSecurityManifestAction,
  IdentityApplicationSecurityManifestFeature,
  IdentityApplicationSecurityManifestRequest,
  IdentityApplicationSecurityManifestResource,
} from "@generic-identity/contracts/application-security";

/**
 * Validates a project-owned JSON security manifest uploaded through a trusted
 * server action. Shape and bounds match the Identity Admin GOLDEN parser.
 *
 * This is input validation, NOT a capability authoring or authorization API.
 * Registration and access-control decisions remain in Identity Access .NET.
 */
export async function parseApplicationSecurityManifestFileCore(
  formData: FormData,
  maxManifestBytes: number,
): Promise<IdentityApplicationSecurityManifestRequest> {
  const entry = formData.get("manifestFile");
  if (typeof File === "undefined" || !(entry instanceof File)) {
    throw invalid("a JSON security manifest file is required.");
  }
  if (entry.size <= 0) {
    throw invalid("the security manifest file is empty.");
  }
  if (entry.size > maxManifestBytes) {
    throw invalid("the security manifest file must not exceed 256 KiB.");
  }
  if (!entry.name.toLowerCase().endsWith(".json")) {
    throw invalid("the security manifest file must use the .json extension.");
  }

  let parsed: unknown;
  try {
    parsed = JSON.parse(await entry.text()) as unknown;
  } catch {
    throw invalid("the security manifest file is not valid JSON.");
  }

  const root = objectValue(parsed, "manifest");
  const schemaVersion = positiveInteger(root.schemaVersion, "schemaVersion");
  if (schemaVersion !== 1) throw invalid("schemaVersion must be 1.");

  const applicationKey = slug(root.applicationKey, "applicationKey");
  const modelVersion = positiveInteger(root.modelVersion, "modelVersion");
  const rbac = objectValue(root.rbac, "rbac");
  const project = rbacSegment(rbac.project, "rbac.project");
  const namespaces = arrayValue(rbac.namespaces, "rbac.namespaces")
    .map((entry, index) => rbacSegment(entry, `rbac.namespaces[${index}]`));

  if (namespaces.length === 0) {
    throw invalid("rbac.namespaces must contain at least one namespace.");
  }
  if (new Set(namespaces).size !== namespaces.length) {
    throw invalid("rbac.namespaces must not contain duplicates.");
  }

  const resources = manifestResources(root.resources);
  const capabilities = new Set<string>();
  for (const resource of resources) {
    for (const feature of resource.features) {
      for (const action of feature.actions) {
        const key = `${resource.name}:${feature.name}:${action.name}`;
        if (capabilities.has(key)) {
          throw invalid(`duplicate capability ${key}.`);
        }
        capabilities.add(key);
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

function manifestResources(value: unknown): readonly IdentityApplicationSecurityManifestResource[] {
  const entries = arrayValue(value, "resources");
  if (entries.length === 0) throw invalid("resources must contain at least one resource.");

  return entries.map((entry, resourceIndex) => {
    const path = `resources[${resourceIndex}]`;
    const resource = objectValue(entry, path);
    return {
      name: slug(resource.name, `${path}.name`),
      features: manifestFeatures(resource.features, resourceIndex),
    };
  });
}

function manifestFeatures(
  value: unknown,
  resourceIndex: number,
): readonly IdentityApplicationSecurityManifestFeature[] {
  const path = `resources[${resourceIndex}].features`;
  const entries = arrayValue(value, path);
  if (entries.length === 0) throw invalid(`${path} must contain at least one feature.`);

  return entries.map((entry, featureIndex) => {
    const featurePath = `${path}[${featureIndex}]`;
    const feature = objectValue(entry, featurePath);
    return {
      name: slug(feature.name, `${featurePath}.name`),
      actions: manifestActions(feature.actions, resourceIndex, featureIndex),
    };
  });
}

function manifestActions(
  value: unknown,
  resourceIndex: number,
  featureIndex: number,
): readonly IdentityApplicationSecurityManifestAction[] {
  const path = `resources[${resourceIndex}].features[${featureIndex}].actions`;
  const entries = arrayValue(value, path);
  if (entries.length === 0) throw invalid(`${path} must contain at least one action.`);

  return entries.map((entry, actionIndex) => {
    const actionPath = `${path}[${actionIndex}]`;
    const action = objectValue(entry, actionPath);
    return {
      name: slug(action.name, `${actionPath}.name`),
      displayName: displayName(action.displayName, `${actionPath}.displayName`),
    };
  });
}

function objectValue(value: unknown, path: string): Record<string, unknown> {
  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    throw invalid(`${path} must be a JSON object.`);
  }
  return value as Record<string, unknown>;
}

function arrayValue(value: unknown, path: string): readonly unknown[] {
  if (!Array.isArray(value)) throw invalid(`${path} must be a JSON array.`);
  return value;
}

function positiveInteger(value: unknown, path: string): number {
  if (typeof value !== "number" || !Number.isSafeInteger(value) || value <= 0) {
    throw invalid(`${path} must be a positive integer.`);
  }
  return value;
}

function slug(value: unknown, path: string): string {
  if (typeof value !== "string") throw invalid(`${path} must be a string.`);
  const normalized = value.trim().toLowerCase();
  if (!/^[a-z][a-z0-9-]{0,63}$/u.test(normalized)) {
    throw invalid(`${path} must be a lowercase slug of at most 64 characters.`);
  }
  return normalized;
}

function rbacSegment(value: unknown, path: string): string {
  if (typeof value !== "string") throw invalid(`${path} must be a string.`);
  const normalized = value.trim().toLowerCase();
  if (!normalized || normalized.length > 128 || normalized.includes(":") || normalized.includes("*")) {
    throw invalid(`${path} must be a concrete RBAC context segment of at most 128 characters.`);
  }
  return normalized;
}

function displayName(value: unknown, path: string): string {
  if (
    typeof value !== "string" ||
    value.trim().length === 0 ||
    value !== value.trim() ||
    value.length > 200 ||
    /[\u0000-\u001f\u007f]/u.test(value)
  ) {
    throw invalid(`${path} must contain between 1 and 200 printable trimmed characters.`);
  }
  return value;
}

function invalid(detail: string): Error {
  return new Error(`Invalid administration input: ${detail}`);
}
