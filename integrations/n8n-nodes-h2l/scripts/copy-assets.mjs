import { copyFileSync, mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const rootDir = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const assets = [
  {
    src: resolve(rootDir, 'nodes', 'H2l', 'h2l.svg'),
    dest: resolve(rootDir, 'dist', 'nodes', 'H2l', 'h2l.svg'),
  },
];

for (const asset of assets) {
  mkdirSync(dirname(asset.dest), { recursive: true });
  copyFileSync(asset.src, asset.dest);
}
