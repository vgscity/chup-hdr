CREATE TABLE IF NOT EXISTS su_kien (
  id INTEGER PRIMARY KEY,
  luc TEXT NOT NULL,            -- ISO UTC
  loai TEXT NOT NULL,           -- tai
  app TEXT NOT NULL,            -- chup-hdr …
  may TEXT NOT NULL DEFAULT '', -- băm IP+ngày+muối, không lưu IP
  ban TEXT NOT NULL DEFAULT '',
  them TEXT NOT NULL DEFAULT '',-- nguồn bấm (tu=…)
  quoc_gia TEXT NOT NULL DEFAULT ''
);
CREATE INDEX IF NOT EXISTS su_kien_app_loai_luc ON su_kien (app, loai, luc);
