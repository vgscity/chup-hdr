// Ảnh listing Store: 1:1 box art 1080/2160, app tile 300/150/71 (nền tối bo góc), poster 9:16 720x1080.
const sharp = require("sharp"); const path = require("path"); const fs = require("fs");
const MASK = path.resolve(__dirname, "../../vgs/vgs-extension/icons/logo-vgs.png");
const RA = path.join(__dirname, "anh-store"); fs.mkdirSync(RA, { recursive: true });
async function logo(h) {
  const m = await sharp(MASK).resize({ height: h }).png().toBuffer();
  const { width, height } = await sharp(m).metadata();
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}"><defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#6366f1"/><stop offset="0.55" stop-color="#8b5cf6"/><stop offset="1" stop-color="#ec4899"/></linearGradient></defs><rect width="100%" height="100%" fill="url(#g)"/></svg>`;
  return sharp(Buffer.from(svg)).composite([{ input: m, blend: "dest-in" }]).png().toBuffer();
}
async function ve(w, h, phan, ten, chu) {
  const l = await logo(Math.round(h * phan));
  const chuSvg = chu ? `<text x="50%" y="${Math.round(h * 0.86)}" text-anchor="middle" font-family="Segoe UI, Helvetica, Arial, sans-serif" font-weight="700" font-size="${Math.round(h * 0.055)}" fill="#ffffff">${chu}</text><text x="50%" y="${Math.round(h * 0.92)}" text-anchor="middle" font-family="Segoe UI, Helvetica, Arial, sans-serif" font-size="${Math.round(h * 0.032)}" fill="#a5b4fc">Color-accurate screenshots on HDR displays</text>` : "";
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}"><rect width="${w}" height="${h}" fill="#1a1626"/>${chuSvg}</svg>`;
  const buf = await sharp(Buffer.from(svg)).composite([{ input: l, gravity: chu ? "north" : "centre", top: chu ? Math.round(h * 0.18) : undefined, left: chu ? Math.round((w - (await sharp(l).metadata()).width) / 2) : undefined }]).png().toBuffer();
  fs.writeFileSync(path.join(RA, ten), buf);
}
(async () => {
  await ve(1080, 1080, 0.55, "box-art-1080.png");
  await ve(2160, 2160, 0.55, "box-art-2160.png");
  await ve(300, 300, 0.6, "tile-300.png");
  await ve(150, 150, 0.6, "tile-150.png");
  await ve(71, 71, 0.62, "tile-71.png");
  await ve(720, 1080, 0.42, "poster-720x1080.png", "VGScreenshotHDR");
  await ve(1440, 2160, 0.42, "poster-1440x2160.png", "VGScreenshotHDR");
  console.log(fs.readdirSync(RA).join(" "));
})();
