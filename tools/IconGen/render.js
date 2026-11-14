// Renders tools/IconGen/svg/<group>/<id>.svg into 128x128 opaque PNG icons of the Unity project.
// Run after icons.py:  node tools/IconGen/render.js [--sheet <file.png>]
// Needs the `playwright` package with Chromium. The optional sheet is one picture of all icons for a visual check.
const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const SIZE = 128;
const SVG_ROOT = path.join(__dirname, 'svg');
const OUT_ROOT = path.join(__dirname, '..', '..', 'My project', 'Assets', '_Project', 'Resources', 'Icons');

function listIcons() {
  const icons = [];
  for (const group of fs.readdirSync(SVG_ROOT).sort()) {
    const folder = path.join(SVG_ROOT, group);
    if (!fs.statSync(folder).isDirectory()) continue;
    for (const file of fs.readdirSync(folder).sort()) {
      if (file.endsWith('.svg')) icons.push({ group, name: file.slice(0, -4), svg: fs.readFileSync(path.join(folder, file), 'utf8') });
    }
  }
  return icons;
}

(async () => {
  const sheetIndex = process.argv.indexOf('--sheet');
  const sheetPath = sheetIndex >= 0 ? process.argv[sheetIndex + 1] : null;
  const icons = listIcons();
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: SIZE, height: SIZE }, deviceScaleFactor: 1 });
  for (const icon of icons) {
    await page.setContent(`<html><body style="margin:0;background:#000">${icon.svg}</body></html>`);
    const folder = path.join(OUT_ROOT, icon.group);
    fs.mkdirSync(folder, { recursive: true });
    await page.screenshot({ path: path.join(folder, icon.name + '.png'), clip: { x: 0, y: 0, width: SIZE, height: SIZE }, omitBackground: false });
  }
  if (sheetPath) {
    const columns = 10;
    const cell = SIZE + 8;
    const rows = Math.ceil(icons.length / columns);
    await page.setViewportSize({ width: columns * cell, height: rows * (cell + 16) });
    const cells = icons.map(i => `<div style="width:${cell}px;height:${cell + 16}px;float:left;text-align:center;font:10px sans-serif;color:#ccc">${i.svg}<br>${i.name}</div>`).join('');
    await page.setContent(`<html><body style="margin:0;background:#222">${cells}</body></html>`);
    await page.screenshot({ path: sheetPath });
  }
  await browser.close();
  console.log(`${icons.length} icons rendered to ${OUT_ROOT}`);
})();
