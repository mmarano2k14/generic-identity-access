import { execFileSync } from "node:child_process";
import { createHash } from "node:crypto";
import { cpSync, existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(scriptDir, "../..");
const outputDirectory = path.resolve(
  process.argv[2] ?? path.join(root, "artifacts", "shared-identity-local"),
);

function readJson(file) {
  return JSON.parse(readFileSync(file, "utf8"));
}

function writeJson(file, value) {
  writeFileSync(file, `${JSON.stringify(value, null, 2)}\n`, "utf8");
}

function resolveNpmCli() {
  const candidates = [
    process.env.npm_execpath,
    path.join(path.dirname(process.execPath), "node_modules", "npm", "bin", "npm-cli.js"),
    path.resolve(path.dirname(process.execPath), "..", "lib", "node_modules", "npm", "bin", "npm-cli.js"),
  ].filter(Boolean);

  for (const candidate of candidates) {
    if (existsSync(candidate)) {
      return candidate;
    }
  }

  throw new Error(
    "Unable to resolve npm CLI. Run this builder from an npm script or install npm beside Node.js.",
  );
}

const npmCli = resolveNpmCli();

function runNpm(args, cwd) {
  execFileSync(process.execPath, [npmCli, ...args], {
    cwd,
    stdio: "inherit",
    windowsHide: true,
    env: process.env,
  });
}

function packageFileName(name, version) {
  const normalized = name.startsWith("@") ? name.slice(1).replace("/", "-") : name;
  return `${normalized}-${version}.tgz`;
}

function copyPackage(source, destination) {
  cpSync(source, destination, {
    recursive: true,
    filter(sourcePath) {
      const base = path.basename(sourcePath);
      return base !== "node_modules" && base !== "package-lock.json" && !base.endsWith(".tgz");
    },
  });
}

function sha256(file) {
  return createHash("sha256").update(readFileSync(file)).digest("hex");
}

mkdirSync(outputDirectory, { recursive: true });

const clientRoot = path.join(root, "clients", "typescript");
const packageRoots = {
  contracts: path.join(root, "packages", "contracts"),
  auth: path.join(root, "packages", "auth"),
  react: path.join(root, "packages", "react"),
  next: path.join(root, "packages", "next"),
};

for (const required of [clientRoot, ...Object.values(packageRoots)]) {
  if (!existsSync(path.join(required, "package.json"))) {
    throw new Error(`Required package is missing: ${required}`);
  }
}

const clientManifest = readJson(path.join(clientRoot, "package.json"));
const manifests = Object.fromEntries(
  Object.entries(packageRoots).map(([key, packageRoot]) => [
    key,
    readJson(path.join(packageRoot, "package.json")),
  ]),
);

const versions = {
  client: clientManifest.version,
  contracts: manifests.contracts.version,
  auth: manifests.auth.version,
  react: manifests.react.version,
  next: manifests.next.version,
};

const expectedArtifacts = [
  packageFileName(clientManifest.name, versions.client),
  packageFileName(manifests.contracts.name, versions.contracts),
  packageFileName(manifests.auth.name, versions.auth),
  packageFileName(manifests.react.name, versions.react),
  packageFileName(manifests.next.name, versions.next),
];

for (const artifact of expectedArtifacts) {
  const target = path.join(outputDirectory, artifact);
  if (existsSync(target)) {
    rmSync(target, { force: true });
  }
}

console.log("Building legacy TypeScript client artifact...");
try {
  runNpm(["run", "build"], clientRoot);
} catch {
  console.log("Restoring legacy TypeScript client development dependencies after the initial build could not resolve its toolchain...");
  runNpm(["install", "--ignore-scripts", "--no-audit", "--no-fund", "--package-lock=false"], clientRoot);
  runNpm(["run", "build"], clientRoot);
}

const stageRoot = mkdtempSync(path.join(tmpdir(), "generic-identity-local-packages-"));
try {
  const stageClientRoot = path.join(stageRoot, "client");
  copyPackage(clientRoot, stageClientRoot);
  const stageClientPackageJsonPath = path.join(stageClientRoot, "package.json");
  const stageClientPackageJson = readJson(stageClientPackageJsonPath);
  stageClientPackageJson.private = false;
  stageClientPackageJson.publishConfig = { access: "public" };
  writeJson(stageClientPackageJsonPath, stageClientPackageJson);
  console.log(`Packing ${stageClientPackageJson.name}@${stageClientPackageJson.version}...`);
  runNpm(["pack", "--ignore-scripts", "--pack-destination", outputDirectory], stageClientRoot);

  const dependencyVersions = {
    "@identity-access/client": versions.client,
    "@generic-identity/contracts": versions.contracts,
    "@generic-identity/auth": versions.auth,
    "@generic-identity/react": versions.react,
  };

  for (const key of ["contracts", "auth", "react", "next"]) {
    const sourceRoot = packageRoots[key];
    const stagePackageRoot = path.join(stageRoot, key);
    copyPackage(sourceRoot, stagePackageRoot);

    const packageJsonPath = path.join(stagePackageRoot, "package.json");
    const packageJson = readJson(packageJsonPath);
    packageJson.private = false;
    packageJson.publishConfig = { access: "public" };

    if (key === "contracts") {
      packageJson.dependencies = {
        "@identity-access/client": dependencyVersions["@identity-access/client"],
      };
    } else if (key === "auth") {
      packageJson.dependencies = {
        "@generic-identity/contracts": dependencyVersions["@generic-identity/contracts"],
        "@identity-access/client": dependencyVersions["@identity-access/client"],
      };
    } else if (key === "react") {
      packageJson.dependencies = {
        "@generic-identity/auth": dependencyVersions["@generic-identity/auth"],
        "@generic-identity/contracts": dependencyVersions["@generic-identity/contracts"],
      };
    } else if (key === "next") {
      packageJson.dependencies = {
        "@generic-identity/auth": dependencyVersions["@generic-identity/auth"],
        "@generic-identity/contracts": dependencyVersions["@generic-identity/contracts"],
        "@generic-identity/react": dependencyVersions["@generic-identity/react"],
        "server-only": packageJson.dependencies?.["server-only"] ?? "0.0.1",
      };
    }

    writeJson(packageJsonPath, packageJson);
    console.log(`Packing ${packageJson.name}@${packageJson.version}...`);
    runNpm(["pack", "--ignore-scripts", "--pack-destination", outputDirectory], stagePackageRoot);
  }
} finally {
  rmSync(stageRoot, { recursive: true, force: true });
}

const artifacts = expectedArtifacts.map((fileName) => {
  const file = path.join(outputDirectory, fileName);
  if (!existsSync(file)) {
    throw new Error(`Expected local package artifact was not created: ${file}`);
  }
  return {
    file: fileName,
    sha256: sha256(file),
  };
});

const artifactManifest = {
  schemaVersion: 1,
  generatedFrom: "generic-identity local consumer packages",
  versions,
  artifacts,
};
writeJson(path.join(outputDirectory, "manifest.json"), artifactManifest);

console.log("");
console.log("Shared Identity local consumer package artifacts: GREEN");
console.log(`Output: ${outputDirectory}`);
for (const artifact of artifacts) {
  console.log(`  ${artifact.file}`);
}
