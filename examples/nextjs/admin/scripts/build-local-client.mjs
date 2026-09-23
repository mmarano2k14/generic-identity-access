import {
  cpSync,
  existsSync,
  lstatSync,
  mkdirSync,
  readFileSync,
  rmSync,
  unlinkSync,
  writeFileSync,
} from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const hostRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const clientRoot = resolve(hostRoot, "../../../clients/typescript");
const clientConfig = resolve(clientRoot, "tsconfig.json");
const clientDist = resolve(clientRoot, "dist");
const tsc = resolve(hostRoot, "node_modules/typescript/bin/tsc");
const result = spawnSync(process.execPath, [tsc, "-p", clientConfig], { stdio: "inherit" });
if (result.status !== 0) process.exit(result.status ?? 1);

// npm may materialize a local file: dependency as a symlink/junction to a package
// outside the Next.js application root. TypeScript can resolve that layout, while
// the production Next.js bundler can reject it. Always replace the installed local
// dependency with a real package directory owned by this host before typecheck,
// build, or dev startup.
const installedClientRoot = resolve(hostRoot, "node_modules/@identity-access/client");
if (existsSync(installedClientRoot)) {
  const installedStat = lstatSync(installedClientRoot);
  if (installedStat.isSymbolicLink()) {
    unlinkSync(installedClientRoot);
  } else {
    rmSync(installedClientRoot, { recursive: true, force: true });
  }
}

mkdirSync(installedClientRoot, { recursive: true });
const installedDist = resolve(installedClientRoot, "dist");
cpSync(clientDist, installedDist, { recursive: true, force: true });
for (const fileName of ["package.json", "README.md"]) {
  const source = resolve(clientRoot, fileName);
  writeFileSync(resolve(installedClientRoot, fileName), readFileSync(source));
}

for (const requiredFile of [
  "package.json",
  "dist/index.js",
  "dist/index.d.ts",
]) {
  if (!existsSync(resolve(installedClientRoot, requiredFile))) {
    throw new Error(`Local Identity Access client materialization is incomplete: ${requiredFile}`);
  }
}
