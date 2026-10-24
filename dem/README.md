# vgs-tai-dem — bộ đếm tải file VGS trên R2

Worker Cloudflare đếm lượt tải (ẩn danh, D1) rồi phát file từ bucket `vgs-tai`. Theo mẫu bộ đếm của app CleanTV (VGS).

- `GET /tai/chup-hdr?tu=<nguồn>` → ghi một lượt (băm IP+ngày+muối, một người một lượt/ngày) rồi 302 sang `/file/chup-hdr/<bản>/VGScreenshotHDR-<bản>.exe`.
- `GET|HEAD /file/<app>/<bản>/<tên>` → phát file từ R2 (hỗ trợ Range, cache 1 năm vì tên có số bản).
- `GET /so-lieu?app=chup-hdr&ngay=7|14|30|90` + header `x-khoa` → JSON cho admin vgs.zone (#chup-hdr, qua `/api/admin/chup-hdr`).

Bản đang phát = `BAN_CHUP_HDR` trong `wrangler.jsonc`. Ra bản mới: đẩy exe lên R2 `chup-hdr/<bản>/VGScreenshotHDR-<bản>.exe`,
đổi `BAN_CHUP_HDR`, rồi deploy:

```bash
cd dem && npx wrangler deploy
```
Secret của Worker (`wrangler secret put`): `XEM_KHOA` (khoá đọc `/so-lieu`, phía vgs.zone giữ bản sao) và `MUOI` (muối băm IP+ngày;
thiếu muối thì Worker không ghi lượt nào). D1: `vgs-tai-dem`, bảng `su_kien` (schema.sql). Cần `wrangler` (npm i -D wrangler).
