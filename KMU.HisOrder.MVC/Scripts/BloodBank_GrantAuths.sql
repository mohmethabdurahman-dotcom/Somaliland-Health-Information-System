-- Blood Bank authorization: Admin + Staff only (same checkbox style as other systems).
-- Blood Bank Admin  = full access including Staff Audit
-- Blood Bank Staff  = all Blood Bank menus except Staff Audit
-- Assign in User Auth Setting. Users must log out/in after changes.

INSERT INTO kmu_projects (project_id, project_name, url, creator, create_time)
SELECT 'BloodBank_Admin', 'Blood Bank Admin', '/BloodBank/Home', 'SYSTEM', NOW()
WHERE NOT EXISTS (SELECT 1 FROM kmu_projects WHERE project_id = 'BloodBank_Admin');

INSERT INTO kmu_projects (project_id, project_name, url, creator, create_time)
SELECT 'BloodBank_Staff', 'Blood Bank Staff', '/BloodBank/Home', 'SYSTEM', NOW()
WHERE NOT EXISTS (SELECT 1 FROM kmu_projects WHERE project_id = 'BloodBank_Staff');

-- Migrate Reception → Staff, Lab → Admin
INSERT INTO kmu_auths (user_idno, project_id, creator, create_time)
SELECT a.user_idno, 'BloodBank_Staff', 'SYSTEM', NOW()
FROM kmu_auths a
WHERE a.project_id IN ('BloodBank', 'BloodBank_Reception')
  AND NOT EXISTS (
    SELECT 1 FROM kmu_auths x
    WHERE x.user_idno = a.user_idno AND x.project_id = 'BloodBank_Staff'
  );

INSERT INTO kmu_auths (user_idno, project_id, creator, create_time)
SELECT a.user_idno, 'BloodBank_Admin', 'SYSTEM', NOW()
FROM kmu_auths a
WHERE a.project_id = 'BloodBank_Lab'
  AND NOT EXISTS (
    SELECT 1 FROM kmu_auths x
    WHERE x.user_idno = a.user_idno AND x.project_id = 'BloodBank_Admin'
  );

DELETE FROM kmu_auths WHERE project_id IN ('BloodBank', 'BloodBank_Reception', 'BloodBank_Lab');
DELETE FROM kmu_projects WHERE project_id IN ('BloodBank', 'BloodBank_Reception', 'BloodBank_Lab');

INSERT INTO kmu_auths (user_idno, project_id, creator, create_time)
SELECT 'admin', 'BloodBank_Admin', 'SYSTEM', NOW()
WHERE EXISTS (SELECT 1 FROM kmu_users WHERE user_idno = 'admin')
  AND NOT EXISTS (
    SELECT 1 FROM kmu_auths WHERE user_idno = 'admin' AND project_id = 'BloodBank_Admin'
  );

SELECT project_id, COUNT(*) AS users
FROM kmu_auths
WHERE project_id IN ('BloodBank_Admin', 'BloodBank_Staff')
GROUP BY project_id
ORDER BY project_id;
