-- =====================================================================
-- 003_seed_data.sql
-- Optional seed script. The application also seeds via DbInitializer
-- on first run; this file exists so reviewers can populate a fresh
-- database from sqlite3 CLI without running the app.
--
-- Dates are written as ISO-8601 strings (matching EF's SQLite mapping).
-- =====================================================================

INSERT INTO Providers (ProviderName, County, Status, CreatedDate, IsDeleted)
VALUES
  ('Atlanta Family Care',     'Fulton',   'Active',   datetime('now'), 0),
  ('DeKalb Senior Services',  'DeKalb',   'Active',   datetime('now'), 0),
  ('Cobb Health Partners',    'Cobb',     'Active',   datetime('now'), 0),
  ('Gwinnett Pediatrics',     'Gwinnett', 'Pending',  datetime('now'), 0),
  ('North Fulton Behavioral', 'Fulton',   'Inactive', datetime('now'), 0),
  ('South DeKalb Clinic',     'DeKalb',   'Active',   datetime('now'), 0),
  ('Cherokee Wellness',       'Cherokee', 'Active',   datetime('now'), 0),
  ('Henry County Outreach',   'Henry',    'Pending',  datetime('now'), 0);

-- Licenses: a deliberately mixed set covering every assignment scenario.
INSERT INTO Licenses (ProviderId, LicenseNumber, LicenseStatus, ExpirationDate, CreatedDate, IsDeleted)
SELECT p.ProviderId, 'AFC-1001', 'Active', date('now', '+8 months'), datetime('now'), 0 FROM Providers p WHERE p.ProviderName = 'Atlanta Family Care' UNION ALL
SELECT p.ProviderId, 'AFC-1002', 'Active', date('now', '+20 days'),  datetime('now'), 0 FROM Providers p WHERE p.ProviderName = 'Atlanta Family Care' UNION ALL
-- Active provider whose only license already expired by date - the spec scenario.
SELECT p.ProviderId, 'DSS-2001', 'Active', date('now', '-2 months'), datetime('now'), 0 FROM Providers p WHERE p.ProviderName = 'DeKalb Senior Services' UNION ALL
SELECT p.ProviderId, 'CHP-3001', 'Suspended', date('now', '+6 months'), datetime('now'), 0 FROM Providers p WHERE p.ProviderName = 'Cobb Health Partners' UNION ALL
SELECT p.ProviderId, 'CHP-3002', 'Active',    date('now', '+1 year'),  datetime('now'), 0 FROM Providers p WHERE p.ProviderName = 'Cobb Health Partners' UNION ALL
SELECT p.ProviderId, 'NFB-4001', 'Expired',   date('now', '-1 year'),  datetime('now'), 0 FROM Providers p WHERE p.ProviderName = 'North Fulton Behavioral' UNION ALL
SELECT p.ProviderId, 'SDC-5001', 'Active',    date('now', '+15 days'), datetime('now'), 0 FROM Providers p WHERE p.ProviderName = 'South DeKalb Clinic' UNION ALL
SELECT p.ProviderId, 'SDC-5002', 'Active',    date('now', '+2 years'), datetime('now'), 0 FROM Providers p WHERE p.ProviderName = 'South DeKalb Clinic' UNION ALL
SELECT p.ProviderId, 'CW-6001',  'Active',    date('now', '+60 days'), datetime('now'), 0 FROM Providers p WHERE p.ProviderName = 'Cherokee Wellness';
