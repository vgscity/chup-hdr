// Bộ đếm tải ẩn danh cho file VGS trên R2 (23/10/2026, theo mẫu cleantv-dem). Không lưu IP: lượt tải ghi băm IP+ngày+muối
// (muối là secret MUOI của Worker, không nằm trong mã) để đếm người tải khác nhau trong ngày. Đường: /tai/<app>?tu=<nguồn> (đếm rồi chuyển sang file), /file/<app>/<bản>/<tên>
// (phát file từ R2), /so-lieu?app=<app>&ngay=N (JSON cho admin vgs.zone, khoá qua header x-khoa).
const APP = {
  "chup-hdr": {
    ban: env => env.BAN_CHUP_HDR,
    khoa: ban => `chup-hdr/${ban}/VGScreenshotHDR-${ban}.exe`,
    tenFile: ban => `VGScreenshotHDR-${ban}.exe`,
    loai: "application/vnd.microsoft.portable-executable",
  },
};
const cat = (s, n) => String(s ?? "").replace(/[\u0000-\u001f]/g, "").trim().slice(0, n);

async function bam(chu) {
  const d = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(chu));
  return [...new Uint8Array(d)].slice(0, 8).map(b => b.toString(16).padStart(2, "0")).join("");
}

// Một người (băm IP+ngày) tải một app chỉ tính một lượt mỗi ngày.
async function ghi(env, r) {
  await env.DB.prepare(`INSERT INTO su_kien (luc, loai, app, may, ban, them, quoc_gia)
      SELECT ?1, 'tai', ?2, ?3, ?4, ?5, ?6 WHERE NOT EXISTS (
        SELECT 1 FROM su_kien WHERE loai = 'tai' AND app = ?2 AND may = ?3 AND luc >= ?7)`)
    .bind(new Date().toISOString(), r.app, r.may, r.ban || "", r.them || "", r.quoc_gia || "",
      new Date(Date.now() - 86400e3).toISOString()).run();
}

async function guiFile(env, req, app, ban, ten) {
  const cau = APP[app];
  if (!cau || !/^[0-9][0-9A-Za-z.-]{0,20}$/.test(ban) || ten !== cau.tenFile(ban)) return new Response("Không có", { status: 404 });
  const khoa = cau.khoa(ban);
  const tuyChon = {};
  if (req.headers.has("range")) tuyChon.range = req.headers;
  if (["if-none-match", "if-modified-since"].some(k => req.headers.has(k))) tuyChon.onlyIf = req.headers;
  const obj = req.method === "HEAD" ? await env.TAI.head(khoa) : await env.TAI.get(khoa, tuyChon);
  if (!obj) return new Response("Không có", { status: 404 });
  const h = new Headers();
  obj.writeHttpMetadata(h);
  h.set("content-type", cau.loai);
  h.set("content-disposition", `attachment; filename="${ten}"`);
  h.set("etag", obj.httpEtag);
  h.set("accept-ranges", "bytes");
  h.set("cache-control", "public, max-age=31536000, immutable"); // tên có số bản, không bao giờ đổi
  h.set("content-length", String(obj.size));
  if (req.method === "HEAD") return new Response(null, { headers: h });
  if (!("body" in obj)) return new Response(null, { status: 304, headers: h });
  if (tuyChon.range && obj.range) {
    const off = obj.range.offset ?? 0, len = obj.range.length ?? obj.size - off;
    h.set("content-range", `bytes ${off}-${off + len - 1}/${obj.size}`);
    h.set("content-length", String(len));
    return new Response(obj.body, { status: 206, headers: h });
  }
  return new Response(obj.body, { headers: h });
}

export default {
  async fetch(req, env, ctx) {
    const url = new URL(req.url);
    const phan = url.pathname.split("/").filter(Boolean);

    if (phan[0] === "tai" && phan[1] && APP[phan[1]]) {
      const app = phan[1], cau = APP[app], ban = cau.ban(env);
      const ip = req.headers.get("cf-connecting-ip") || "";
      const ngay = new Date(Date.now() + 7 * 3600e3).toISOString().slice(0, 10);
      const may = await bam(`${ip}|${ngay}|${env.MUOI}`);
      // Không có muối (secret MUOI chưa đặt) thì không ghi: băm thiếu muối dò ngược ra IP được.
      if (req.method === "GET" && env.MUOI) ctx.waitUntil(ghi(env, { app, may, ban, them: cat(url.searchParams.get("tu"), 40), quoc_gia: req.cf?.country || "" }).catch(() => {}));
      return Response.redirect(new URL(`/file/${app}/${ban}/${cau.tenFile(ban)}`, url).toString(), 302);
    }

    if (phan[0] === "file" && phan.length === 4 && (req.method === "GET" || req.method === "HEAD")) return guiFile(env, req, phan[1], phan[2], phan[3]);

    if (url.pathname === "/so-lieu") {
      if (!env.XEM_KHOA || req.headers.get("x-khoa") !== env.XEM_KHOA) return new Response("Không có quyền", { status: 403 });
      const app = url.searchParams.get("app") || "chup-hdr";
      if (!APP[app]) return new Response("Không có", { status: 404 });
      const n = [7, 14, 30, 90].includes(Number(url.searchParams.get("ngay"))) ? Number(url.searchParams.get("ngay")) : 14;
      return Response.json(await soLieu(env, app, n), { headers: { "cache-control": "no-store" } });
    }
    return new Response("vgs-tai", { status: 404 });
  },
};

const VN = `datetime(luc, '+7 hours')`;
async function soLieu(env, app, n) {
  const q = (sql, ...a) => env.DB.prepare(sql).bind(...a).all().then(r => r.results);
  const mot = async (sql, ...a) => (await q(sql, ...a))[0] || {};
  const tu = `datetime('now','+7 hours','-${n} days')`, truoc = `datetime('now','+7 hours','-${2 * n} days')`;
  const [tai, nguoi, ban, nguon, quocGia, theoNgay, ganDay] = await Promise.all([
    mot(`SELECT COUNT(*) tong, SUM(${VN} >= ${tu}) ky, SUM(${VN} >= ${truoc} AND ${VN} < ${tu}) kyTruoc FROM su_kien WHERE loai='tai' AND app=?`, app),
    mot(`SELECT COUNT(DISTINCT may) tong, COUNT(DISTINCT CASE WHEN ${VN} >= ${tu} THEN may END) ky,
           COUNT(DISTINCT CASE WHEN ${VN} >= ${truoc} AND ${VN} < ${tu} THEN may END) kyTruoc FROM su_kien WHERE loai='tai' AND app=?`, app),
    q(`SELECT ban, COUNT(*) n FROM su_kien WHERE loai='tai' AND app=? GROUP BY ban ORDER BY n DESC`, app),
    q(`SELECT CASE WHEN them = '' THEN 'khác' ELSE them END nguon, COUNT(*) n FROM su_kien WHERE loai='tai' AND app=? AND ${VN} >= ${tu} GROUP BY 1 ORDER BY n DESC LIMIT 8`, app),
    q(`SELECT CASE WHEN quoc_gia = '' THEN '?' ELSE quoc_gia END qg, COUNT(*) n FROM su_kien WHERE loai='tai' AND app=? AND ${VN} >= ${tu} GROUP BY 1 ORDER BY n DESC LIMIT 8`, app),
    q(`SELECT date(${VN}) d, COUNT(*) tai, COUNT(DISTINCT may) nguoi FROM su_kien WHERE loai='tai' AND app=? AND ${VN} >= ${tu} GROUP BY d ORDER BY d`, app),
    q(`SELECT ${VN} luc, ban, them nguon, quoc_gia qg FROM su_kien WHERE loai='tai' AND app=? ORDER BY luc DESC LIMIT 20`, app),
  ]);
  return { app, ngay: n, banHienTai: APP[app].ban(env), tai, nguoi, ban, nguon, quocGia, theoNgay, ganDay };
}
