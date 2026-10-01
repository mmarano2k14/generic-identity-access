import { execFileSync } from "node:child_process";
import { createHash } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(scriptDir, "../..");
const outputDirectory = path.join(root, "artifacts", "shared-identity-release");
const builder = path.join(root, "scripts", "shared-identity", "build-local-consumer-packages.mjs");
const releaseVersion = "1.0.0";

function sha256(file) {
  return createHash("sha256").update(readFileSync(file)).digest("hex");
}

function run(command, args, options = {}) {
  return execFileSync(command, args, {
    cwd: options.cwd ?? root,
    stdio: options.stdio ?? "pipe",
    encoding: options.encoding ?? "utf8",
    windowsHide: true,
    env: process.env,
  });
}

function readPackedPackageJson(tarball) {
  const text = run("tar", ["-xOf", tarball, "package/package.json"]);
  return JSON.parse(text);
}

function listPackedFiles(tarball) {
  return run("tar", ["-tf", tarball]).split(/\r?\n/).filter(Boolean);
}

if (!existsSync(builder)) {
  throw new Error(`Release package builder is missing: ${builder}`);
}
mkdirSync(outputDirectory, { recursive: true });
run(process.execPath, [builder, outputDirectory], { stdio: "inherit" });

const localManifestPath = path.join(outputDirectory, "manifest.json");
if (!existsSync(localManifestPath)) {
  throw new Error("Release package artifact manifest was not generated.");
}
const localManifest = JSON.parse(readFileSync(localManifestPath, "utf8"));
const expectedVersions = {
  client: "0.26.0",
  contracts: releaseVersion,
  auth: releaseVersion,
  react: releaseVersion,
  next: releaseVersion,
};
for (const [key, expected] of Object.entries(expectedVersions)) {
  if (localManifest.versions?.[key] !== expected) {
    throw new Error(`Release version mismatch for ${key}: expected ${expected}, got ${localManifest.versions?.[key]}`);
  }
}

const expectedNames = new Map([
  ["@identity-access/client", "0.26.0"],
  ["@generic-identity/contracts", releaseVersion],
  ["@generic-identity/auth", releaseVersion],
  ["@generic-identity/react", releaseVersion],
  ["@generic-identity/next", releaseVersion],
]);
const qualifiedPackages = [];
for (const artifact of localManifest.artifacts ?? []) {
  const tarball = path.join(outputDirectory, artifact.file);
  if (!existsSync(tarball)) {
    throw new Error(`Release artifact is missing: ${tarball}`);
  }
  if (sha256(tarball) !== artifact.sha256) {
    throw new Error(`Release artifact checksum mismatch: ${artifact.file}`);
  }

  const packageJson = readPackedPackageJson(tarball);
  const expectedVersion = expectedNames.get(packageJson.name);
  if (!expectedVersion) {
    throw new Error(`Unexpected package in release set: ${packageJson.name}`);
  }
  if (packageJson.version !== expectedVersion) {
    throw new Error(`${packageJson.name} must be version ${expectedVersion}; got ${packageJson.version}`);
  }
  if (packageJson.private === true) {
    throw new Error(`${packageJson.name} release artifact must not remain private.`);
  }
  if (packageJson.publishConfig?.access !== "public") {
    throw new Error(`${packageJson.name} release artifact must declare publishConfig.access=public.`);
  }
  for (const [depName, depVersion] of Object.entries(packageJson.dependencies ?? {})) {
    if (/^(?:file:|link:|workspace:)/.test(String(depVersion))) {
      throw new Error(`${packageJson.name} release dependency ${depName} must not use a local link: ${depVersion}`);
    }
  }

  const files = listPackedFiles(tarball);
  for (const forbidden of ["node_modules", ".env", "secrets", "package-lock.json"]) {
    if (files.some((entry) => entry.toLowerCase().includes(forbidden.toLowerCase()))) {
      throw new Error(`${packageJson.name} release artifact contains forbidden entry matching '${forbidden}'.`);
    }
  }
  if (!files.includes("package/package.json")) {
    throw new Error(`${packageJson.name} release artifact is missing package.json.`);
  }
  if (packageJson.name.startsWith("@generic-identity/") && !packageJson.exports?.["."]) {
    throw new Error(`${packageJson.name} must expose its public root export.`);
  }

  qualifiedPackages.push({
    name: packageJson.name,
    version: packageJson.version,
    file: artifact.file,
    sha256: artifact.sha256,
  });
}
if (qualifiedPackages.length !== 5) {
  throw new Error(`Release qualification expected 5 package artifacts, got ${qualifiedPackages.length}.`);
}

const releaseManifest = {
  schemaVersion: 1,
  release: releaseVersion,
  status: "qualified-not-published",
  generatedAtUtc: new Date().toISOString(),
  packages: qualifiedPackages.sort((a, b) => a.name.localeCompare(b.name)),
};
writeFileSync(
  path.join(outputDirectory, "release-manifest.json"),
  `${JSON.stringify(releaseManifest, null, 2)}\n`,
  "utf8",
);

console.log("");
console.log(`Shared Identity ${releaseVersion} release artifact qualification: GREEN`);
console.log(`Output: ${outputDirectory}`);
for (const pkg of releaseManifest.packages) {
  console.log(`  ${pkg.name}@${pkg.version}  ${pkg.file}`);
}
