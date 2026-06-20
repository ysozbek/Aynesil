-- =====================================================================
-- AyNesil Platform :: Seed — Locales + Reference-Data Catalog + Default Values
-- Idempotent. Run as table owner (bypasses RLS to write system/global rows).
-- =====================================================================

-- ---------------------------------------------------------------------
-- Locales
-- ---------------------------------------------------------------------
insert into ref.locale(code, english_name, native_name, sort_order) values
  ('tr','Turkish','Türkçe',1),
  ('en','English','English',2)
on conflict (code) do nothing;

-- ---------------------------------------------------------------------
-- Reference TYPES (every business list is DATA here — no schema change to add more)
-- ---------------------------------------------------------------------
insert into ref.ref_type(code, name, is_system, is_hierarchical, allows_tenant_values) values
  ('session_type','Session Types',false,false,true),
  ('therapy_type','Therapy Types',false,false,true),
  ('program_type','Program Types',false,false,true),
  ('service_type','Service Types',false,false,true),
  ('goal_category','Goal Categories',false,true,true),
  ('development_area','Development Areas',false,false,true),
  ('assessment_type','Assessment Types',false,false,true),
  ('assessment_category','Assessment Categories',false,true,true),
  ('meeting_type','Meeting Types',false,false,true),
  ('leave_type','Leave Types',false,false,true),
  ('attendance_reason','Attendance Reasons',false,false,true),
  ('missed_reason','Missed Session Reasons',false,false,true),
  ('payment_method','Payment Methods',false,false,true),
  ('discount_type','Discount Types',false,false,true),
  ('scholarship_type','Scholarship Types',false,false,true),
  ('package_type','Package Types',false,false,true),
  ('lead_source','Lead Sources',false,false,true),
  ('lead_status','Lead Statuses',false,false,true),
  ('pipeline_stage','Pipeline Stages',false,false,true),
  ('notification_type','Notification Types',false,false,true),
  ('notification_category','Notification Categories',false,false,true),
  ('notification_channel','Notification Channels',true,false,false),
  ('contract_type','Contract Types',false,false,true),
  ('consent_type','Consent Types',false,false,true),
  ('educator_title','Educator Titles',false,false,true),
  ('educator_relationship','Educator Hierarchy Relationships',false,false,true),
  ('specialty','Educator Specialties',false,false,true),
  ('certification_type','Certification Types',false,false,true),
  ('guardian_relationship','Guardian Relationships',false,false,true),
  ('student_status','Student Statuses',false,false,true),
  ('enrollment_status','Enrollment Statuses',false,false,true),
  ('diagnosis_category','Diagnosis Categories',false,true,true),
  ('institution_type','Institution Types',false,false,true),
  ('room_type','Room Types',false,false,true),
  ('camp_type','Camp Types',false,false,true),
  ('activity_type','Activity Types',false,false,true),
  ('academic_term','Academic Terms',false,false,true),
  ('kpi_category','KPI Categories',false,true,true),
  ('report_category','Report Categories',false,true,true),
  ('integration_kind','Integration Kinds',true,false,false)
on conflict (code) do nothing;

-- ---------------------------------------------------------------------
-- Default VALUES (corporation_id NULL => global; tenants may extend/override)
-- ---------------------------------------------------------------------
insert into ref.ref_value(ref_type_id, code, sort_order, is_default, is_system)
select ref.type_id(v.type_code), v.code, v.sort_order, v.is_default, v.is_system
from (values
  -- session_type
  ('session_type','individual',1,true,false),
  ('session_type','group',2,false,false),
  ('session_type','intensive',3,false,false),
  ('session_type','camp',4,false,false),
  ('session_type','online',5,false,false),
  -- service_type
  ('service_type','therapy',1,true,false),
  ('service_type','education',2,false,false),
  ('service_type','consultation',3,false,false),
  ('service_type','camp',4,false,false),
  ('service_type','online',5,false,false),
  -- program_type
  ('program_type','individual_education',1,true,false),
  ('program_type','group_education',2,false,false),
  ('program_type','therapy',3,false,false),
  ('program_type','camp',4,false,false),
  ('program_type','online',5,false,false),
  -- therapy_type
  ('therapy_type','aba',1,true,false),
  ('therapy_type','floortime',2,false,false),
  ('therapy_type','speech_language',3,false,false),
  ('therapy_type','occupational',4,false,false),
  ('therapy_type','physiotherapy',5,false,false),
  ('therapy_type','psychological',6,false,false),
  -- lead_source
  ('lead_source','website',1,true,false),
  ('lead_source','phone',2,false,false),
  ('lead_source','social_media',3,false,false),
  ('lead_source','referral',4,false,false),
  ('lead_source','walk_in',5,false,false),
  -- lead_status
  ('lead_status','new',1,true,false),
  ('lead_status','contacted',2,false,false),
  ('lead_status','qualified',3,false,false),
  ('lead_status','interview',4,false,false),
  ('lead_status','converted',5,false,false),
  ('lead_status','lost',6,false,false),
  -- pipeline_stage
  ('pipeline_stage','prospect',1,true,false),
  ('pipeline_stage','contacted',2,false,false),
  ('pipeline_stage','assessment',3,false,false),
  ('pipeline_stage','recommendation',4,false,false),
  ('pipeline_stage','enrollment',5,false,false),
  -- student_status
  ('student_status','prospect',1,true,false),
  ('student_status','active',2,false,false),
  ('student_status','on_hold',3,false,false),
  ('student_status','graduated',4,false,false),
  ('student_status','terminated',5,false,false),
  -- enrollment_status
  ('enrollment_status','pending',1,true,false),
  ('enrollment_status','active',2,false,false),
  ('enrollment_status','completed',3,false,false),
  ('enrollment_status','withdrawn',4,false,false),
  -- attendance_reason
  ('attendance_reason','illness',1,false,false),
  ('attendance_reason','family',2,false,false),
  ('attendance_reason','transport',3,false,false),
  ('attendance_reason','weather',4,false,false),
  ('attendance_reason','other',5,true,false),
  -- missed_reason
  ('missed_reason','illness',1,false,false),
  ('missed_reason','family',2,false,false),
  ('missed_reason','no_show',3,false,false),
  ('missed_reason','holiday',4,false,false),
  -- payment_method
  ('payment_method','cash',1,true,false),
  ('payment_method','credit_card',2,false,false),
  ('payment_method','bank_transfer',3,false,false),
  ('payment_method','installment',4,false,false),
  -- educator_title
  ('educator_title','therapist',1,true,false),
  ('educator_title','educator',2,false,false),
  ('educator_title','psychologist',3,false,false),
  ('educator_title','consultant',4,false,false),
  ('educator_title','coordinator',5,false,false),
  -- educator_relationship
  ('educator_relationship','supervises',1,true,false),
  ('educator_relationship','consults_for',2,false,false),
  ('educator_relationship','coordinates',3,false,false),
  -- guardian_relationship
  ('guardian_relationship','mother',1,true,false),
  ('guardian_relationship','father',2,false,false),
  ('guardian_relationship','grandparent',3,false,false),
  ('guardian_relationship','legal_guardian',4,false,false),
  ('guardian_relationship','sibling',5,false,false),
  -- room_type
  ('room_type','therapy_room',1,true,false),
  ('room_type','classroom',2,false,false),
  ('room_type','online_room',3,false,false),
  ('room_type','gym',4,false,false),
  -- meeting_type
  ('meeting_type','internal',1,true,false),
  ('meeting_type','parent',2,false,false),
  ('meeting_type','prospect',3,false,false),
  ('meeting_type','external',4,false,false),
  -- leave_type
  ('leave_type','annual',1,true,false),
  ('leave_type','sick',2,false,false),
  ('leave_type','unpaid',3,false,false),
  ('leave_type','hourly',4,false,false),
  -- consent_type
  ('consent_type','data_processing',1,true,false),
  ('consent_type','camera_viewing',2,false,false),
  ('consent_type','media_release',3,false,false),
  -- diagnosis_category
  ('diagnosis_category','autism_spectrum',1,false,false),
  ('diagnosis_category','down_syndrome',2,false,false),
  ('diagnosis_category','learning_disability',3,false,false),
  ('diagnosis_category','speech_disorder',4,false,false),
  ('diagnosis_category','adhd',5,false,false),
  ('diagnosis_category','cerebral_palsy',6,false,false),
  -- development_area
  ('development_area','cognitive',1,false,false),
  ('development_area','language',2,false,false),
  ('development_area','social_emotional',3,false,false),
  ('development_area','motor',4,false,false),
  ('development_area','self_care',5,false,false),
  -- goal_category
  ('goal_category','communication',1,false,false),
  ('goal_category','behavior',2,false,false),
  ('goal_category','academic',3,false,false),
  ('goal_category','motor',4,false,false),
  ('goal_category','social',5,false,false),
  -- camp_type
  ('camp_type','summer',1,true,false),
  ('camp_type','winter',2,false,false),
  ('camp_type','weekend',3,false,false),
  ('camp_type','day',4,false,false),
  -- institution_type
  ('institution_type','kindergarten',1,false,false),
  ('institution_type','primary_school',2,false,false),
  ('institution_type','public_school',3,false,false),
  ('institution_type','rehabilitation_center',4,false,false),
  -- activity_type
  ('activity_type','call',1,true,false),
  ('activity_type','email',2,false,false),
  ('activity_type','sms',3,false,false),
  ('activity_type','note',4,false,false),
  ('activity_type','visit',5,false,false),
  -- notification_channel (system)
  ('notification_channel','email',1,true,true),
  ('notification_channel','sms',2,false,true),
  ('notification_channel','push',3,false,true),
  ('notification_channel','in_app',4,false,true),
  -- integration_kind (system)
  ('integration_kind','email',1,false,true),
  ('integration_kind','sms',2,false,true),
  ('integration_kind','payment',3,false,true),
  ('integration_kind','streaming',4,false,true),
  ('integration_kind','erp',5,false,true),
  ('integration_kind','government',6,false,true),
  ('integration_kind','identity',7,false,true)
) as v(type_code, code, sort_order, is_default, is_system)
on conflict do nothing;

-- ---------------------------------------------------------------------
-- Translations (tr / en) for the seeded values
-- ---------------------------------------------------------------------
insert into ref.ref_value_translation(ref_value_id, locale, label)
select rv.id, t.locale, t.label
from (values
  ('session_type','individual','tr','Bireysel'), ('session_type','individual','en','Individual'),
  ('session_type','group','tr','Grup'),          ('session_type','group','en','Group'),
  ('session_type','intensive','tr','Yoğun'),     ('session_type','intensive','en','Intensive'),
  ('session_type','camp','tr','Kamp'),           ('session_type','camp','en','Camp'),
  ('session_type','online','tr','Online'),       ('session_type','online','en','Online'),

  ('therapy_type','aba','tr','ABA'),                         ('therapy_type','aba','en','ABA'),
  ('therapy_type','floortime','tr','Floortime'),             ('therapy_type','floortime','en','Floortime'),
  ('therapy_type','speech_language','tr','Dil ve Konuşma'),  ('therapy_type','speech_language','en','Speech & Language'),
  ('therapy_type','occupational','tr','Ergoterapi'),         ('therapy_type','occupational','en','Occupational Therapy'),
  ('therapy_type','physiotherapy','tr','Fizyoterapi'),       ('therapy_type','physiotherapy','en','Physiotherapy'),
  ('therapy_type','psychological','tr','Psikolojik Danışmanlık'), ('therapy_type','psychological','en','Psychological Counseling'),

  ('lead_source','website','tr','Web Sitesi'),       ('lead_source','website','en','Website'),
  ('lead_source','phone','tr','Telefon'),            ('lead_source','phone','en','Phone'),
  ('lead_source','social_media','tr','Sosyal Medya'),('lead_source','social_media','en','Social Media'),
  ('lead_source','referral','tr','Referans'),        ('lead_source','referral','en','Referral'),
  ('lead_source','walk_in','tr','Doğrudan Başvuru'), ('lead_source','walk_in','en','Walk-in'),

  ('student_status','prospect','tr','Aday'),       ('student_status','prospect','en','Prospect'),
  ('student_status','active','tr','Aktif'),        ('student_status','active','en','Active'),
  ('student_status','on_hold','tr','Beklemede'),   ('student_status','on_hold','en','On Hold'),
  ('student_status','graduated','tr','Mezun'),     ('student_status','graduated','en','Graduated'),
  ('student_status','terminated','tr','Ayrıldı'),  ('student_status','terminated','en','Terminated'),

  ('educator_title','therapist','tr','Terapist'),     ('educator_title','therapist','en','Therapist'),
  ('educator_title','educator','tr','Eğitimci'),      ('educator_title','educator','en','Educator'),
  ('educator_title','psychologist','tr','Psikolog'),  ('educator_title','psychologist','en','Psychologist'),
  ('educator_title','consultant','tr','Danışman'),    ('educator_title','consultant','en','Consultant'),
  ('educator_title','coordinator','tr','Koordinatör'),('educator_title','coordinator','en','Coordinator'),

  ('consent_type','data_processing','tr','Veri İşleme (KVKK)'), ('consent_type','data_processing','en','Data Processing (KVKK)'),
  ('consent_type','camera_viewing','tr','Kamera İzleme'),       ('consent_type','camera_viewing','en','Camera Viewing'),
  ('consent_type','media_release','tr','Medya Kullanımı'),      ('consent_type','media_release','en','Media Release'),

  ('payment_method','cash','tr','Nakit'),               ('payment_method','cash','en','Cash'),
  ('payment_method','credit_card','tr','Kredi Kartı'),  ('payment_method','credit_card','en','Credit Card'),
  ('payment_method','bank_transfer','tr','Havale/EFT'), ('payment_method','bank_transfer','en','Bank Transfer'),
  ('payment_method','installment','tr','Taksit'),       ('payment_method','installment','en','Installment'),

  ('notification_channel','email','tr','E-posta'),  ('notification_channel','email','en','Email'),
  ('notification_channel','sms','tr','SMS'),        ('notification_channel','sms','en','SMS'),
  ('notification_channel','push','tr','Anlık Bildirim'), ('notification_channel','push','en','Push'),
  ('notification_channel','in_app','tr','Uygulama İçi'), ('notification_channel','in_app','en','In-App')
) as t(type_code, value_code, locale, label)
join ref.ref_value rv
  on rv.ref_type_id = ref.type_id(t.type_code)
 and rv.code = t.value_code
 and rv.corporation_id is null
on conflict (ref_value_id, locale) do nothing;
