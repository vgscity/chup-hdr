// Sinh bộ logo cho gói MSIX / Microsoft Store + icon app từ mask logo VGS (gradient tím–hồng như đầu trang vgs.zone).
// Chạy: NODE_PATH=../../vgs/vgs-contest/node_modules node logo-tao.cjs
const sharp = require("sharp");
const path = require("path");
const fs = require("fs");
const MASK = path.resolve(__dirname, "../../vgs/vgs-extension/icons/logo-vgs.png");
const RA = path.join(__dirname, "anh");
const NEN = "#1a1626";

async function logo(h) {
  const m = await sharp(MASK).resize({ height: h }).png().toBuffer();
  const { width, height } = await sharp(m).metadata();
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}"><defs>
<linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#6366f1"/><stop offset="0.55" stop-color="#8b5cf6"/><stop offset="1" stop-color="#ec4899"/></linearGradient>
</defs><rect width="100%" height="100%" fill="url(#g)"/></svg>`;
  return sharp(Buffer.from(svg)).composite([{ input: m, blend: "dest-in" }]).png().toBuffer();
}

async function ve(w, h, phan, ten, { tile = false, bo = 0 } = {}) {
  const l = await logo(Math.max(8, Math.round(h * phan)));
  let nen;
  if (tile) {
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}"><rect width="${w}" height="${h}" rx="${bo}" ry="${bo}" fill="${NEN}"/></svg>`;
    nen = sharp(Buffer.from(svg));
  } else {
    nen = sharp({ create: { width: w, height: h, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } });
  }
  const buf = await nen.composite([{ input: l, gravity: "centre" }]).png().toBuffer();
  fs.writeFileSync(path.join(RA, ten), buf);
  return buf;
}

function ico(png, kich, ten) {
  // ICO chứa PNG trực tiếp (Vista+)
  const dau = Buffer.alloc(6 + 16);
  dau.writeUInt16LE(0, 0); dau.writeUInt16LE(1, 2); dau.writeUInt16LE(1, 4);
  dau.writeUInt8(kich >= 256 ? 0 : kich, 6); dau.writeUInt8(kich >= 256 ? 0 : kich, 7);
  dau.writeUInt8(0, 8); dau.writeUInt8(0, 9); dau.writeUInt16LE(1, 10); dau.writeUInt16LE(32, 12);
  dau.writeUInt32LE(png.length, 14); dau.writeUInt32LE(22, 18);
  fs.writeFileSync(path.join(RA, ten), Buffer.concat([dau, png]));
}

(async () => {
  fs.mkdirSync(RA, { recursive: true });
  // Gói MSIX (nền trong, Windows tự tô BackgroundColor)
  for (const [ten, c] of [["Square44x44Logo", 44], ["Square71x71Logo", 71], ["Square150x150Logo", 150], ["Square310x310Logo", 310], ["StoreLogo", 50]])
    for (const s of [100, 200]) await ve(c * s / 100, c * s / 100, 0.62, `${ten}.scale-${s}.png`);
  for (const s of [100, 200]) await ve(310 * s / 100, 150 * s / 100, 0.62, `Wide310x150Logo.scale-${s}.png`);
  for (const c of [16, 24, 32, 48, 256]) {
    await ve(c, c, 0.8, `Square44x44Logo.targetsize-${c}.png`);
    await ve(c, c, 0.8, `Square44x44Logo.targetsize-${c}_altform-unplated.png`);
  }
  // Store listing (ô 1:1 có nền) + icon app + tray
  await ve(300, 300, 0.62, "store-listing-300.png", { tile: true, bo: 48 });
  const i256 = await ve(256, 256, 0.66, "app-256.png", { tile: true, bo: 52 });
  ico(i256, 256, "app.ico");
  await ve(32, 32, 0.74, "tray-32.png", { tile: true, bo: 7 });
  console.log("xong", fs.readdirSync(RA).length, "file");
})();
