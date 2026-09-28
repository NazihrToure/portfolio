const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "..");
const output = path.join(root, "build");
const files = ["index.html", "projects.html", "blog.html", "contact.html", "css", "js"];

fs.mkdirSync(output, { recursive: true });

for (const file of files) {
  fs.cpSync(path.join(root, file), path.join(output, file), { recursive: true });
}

console.log("Static portfolio copied to build/.");
