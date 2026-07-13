import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = fileURLToPath(new URL("..", import.meta.url));
const read = (path) => readFileSync(new URL(path, import.meta.url), "utf8");

test("Pages build uses relative assets and a React entrypoint", () => {
  const config = read("../vite.config.ts");
  const html = read("../index.html");
  const packageJson = JSON.parse(read("../package.json"));

  assert.equal(root.endsWith("web/"), true);
  assert.match(config, /base:\s*["']\.\/["']/);
  assert.match(html, /src="\.\/src\/main\.tsx"/);
  assert.equal(packageJson.scripts.build, "tsc -b && vite build");
  assert.equal(packageJson.dependencies.react.startsWith("^19."), true);
});
