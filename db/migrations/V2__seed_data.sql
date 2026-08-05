-- =============================================================================
-- AyNesil Platform :: Flyway V2 — Seed Data
-- Idempotent (ON CONFLICT DO NOTHING / DO UPDATE).
-- Owner rolüyle çalışır — RLS bypass. Sadece sistem/global veriler.
-- =============================================================================


-- =============================================================================
-- Source: db/seed/01_reference_data_seed.sql
-- =============================================================================

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
  ('employment_type','Employment Types',false,false,true),
  ('specialty','Educator Specialties',false,false,true),
  ('certification_type','Certification Types',false,false,true),
  ('guardian_relationship','Guardian Relationships',false,false,true),
  ('student_status','Student Statuses',false,false,true),
  ('gender','Genders',true,false,false),
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
  -- gender
  ('gender','male',1,false,true),
  ('gender','female',2,false,true),
  ('gender','unspecified',3,true,true),
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
  -- package_type
  ('package_type','session_package',1,true,false),
  ('package_type','program_package',2,false,false),
  ('package_type','credit_package',3,false,false),
  ('package_type','camp_package',4,false,false),
  -- scholarship_type
  ('scholarship_type','need_based',1,true,false),
  ('scholarship_type','merit',2,false,false),
  ('scholarship_type','sibling',3,false,false),
  ('scholarship_type','staff',4,false,false),
  -- discount_type
  ('discount_type','percentage',1,true,false),
  ('discount_type','fixed_amount',2,false,false),
  ('discount_type','sibling',3,false,false),
  ('discount_type','early_bird',4,false,false),
  -- educator_title
  ('educator_title','therapist',1,true,false),
  ('educator_title','educator',2,false,false),
  ('educator_title','psychologist',3,false,false),
  ('educator_title','consultant',4,false,false),
  ('educator_title','coordinator',5,false,false),
  -- employment_type
  ('employment_type','full_time',1,true,false),
  ('employment_type','part_time',2,false,false),
  ('employment_type','contractor',3,false,false),
  -- educator_relationship
  ('educator_relationship','supervises',1,true,false),
  ('educator_relationship','consults_for',2,false,false),
  ('educator_relationship','coordinates',3,false,false),
  -- specialty
  ('specialty','aba',1,true,false),
  ('specialty','speech_language',2,false,false),
  ('specialty','occupational',3,false,false),
  ('specialty','physiotherapy',4,false,false),
  ('specialty','psychological',5,false,false),
  ('specialty','special_education',6,false,false),
  -- certification_type
  ('certification_type','bcba',1,true,false),
  ('certification_type','rbt',2,false,false),
  ('certification_type','speech_license',3,false,false),
  ('certification_type','ot_license',4,false,false),
  ('certification_type','teaching_certificate',5,false,false),
  -- assessment_type
  ('assessment_type','intake',1,true,false),
  ('assessment_type','developmental',2,false,false),
  ('assessment_type','progress',3,false,false),
  ('assessment_type','diagnostic',4,false,false),
  ('assessment_type','exit',5,false,false),
  -- assessment_category
  ('assessment_category','cognitive',1,true,false),
  ('assessment_category','language',2,false,false),
  ('assessment_category','social_emotional',3,false,false),
  ('assessment_category','motor',4,false,false),
  ('assessment_category','adaptive',5,false,false),
  ('assessment_category','academic',6,false,false),
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
  -- academic_term
  ('academic_term','fall',1,true,false),
  ('academic_term','spring',2,false,false),
  ('academic_term','summer',3,false,false),
  ('academic_term','year_round',4,false,false),
  -- report_category
  ('report_category','clinical',1,true,false),
  ('report_category','administrative',2,false,false),
  ('report_category','financial',3,false,false),
  ('report_category','operational',4,false,false),
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

  ('enrollment_status','pending','tr','Beklemede'),     ('enrollment_status','pending','en','Pending'),
  ('enrollment_status','active','tr','Aktif'),          ('enrollment_status','active','en','Active'),
  ('enrollment_status','completed','tr','Tamamlandı'),  ('enrollment_status','completed','en','Completed'),
  ('enrollment_status','withdrawn','tr','Ayrıldı'),     ('enrollment_status','withdrawn','en','Withdrawn'),

  ('gender','male','tr','Erkek'),              ('gender','male','en','Male'),
  ('gender','female','tr','Kadın'),            ('gender','female','en','Female'),
  ('gender','unspecified','tr','Belirtilmedi'),('gender','unspecified','en','Unspecified'),

  ('employment_type','full_time','tr','Tam Zamanlı'),   ('employment_type','full_time','en','Full Time'),
  ('employment_type','part_time','tr','Yarı Zamanlı'),  ('employment_type','part_time','en','Part Time'),
  ('employment_type','contractor','tr','Sözleşmeli'),   ('employment_type','contractor','en','Contractor'),

  ('specialty','aba','tr','ABA'), ('specialty','aba','en','ABA'),
  ('specialty','speech_language','tr','Dil ve Konuşma'), ('specialty','speech_language','en','Speech & Language'),
  ('specialty','occupational','tr','Ergoterapi'), ('specialty','occupational','en','Occupational Therapy'),
  ('specialty','physiotherapy','tr','Fizyoterapi'), ('specialty','physiotherapy','en','Physiotherapy'),
  ('specialty','psychological','tr','Psikolojik Danışmanlık'), ('specialty','psychological','en','Psychological Counseling'),
  ('specialty','special_education','tr','Özel Eğitim'), ('specialty','special_education','en','Special Education'),

  ('certification_type','bcba','tr','BCBA'), ('certification_type','bcba','en','BCBA'),
  ('certification_type','rbt','tr','RBT'), ('certification_type','rbt','en','RBT'),
  ('certification_type','speech_license','tr','DKT Yetki Belgesi'), ('certification_type','speech_license','en','Speech License'),
  ('certification_type','ot_license','tr','Ergoterapi Yetki Belgesi'), ('certification_type','ot_license','en','OT License'),
  ('certification_type','teaching_certificate','tr','Öğretmenlik Belgesi'), ('certification_type','teaching_certificate','en','Teaching Certificate'),

  ('assessment_type','intake','tr','İlk Değerlendirme'), ('assessment_type','intake','en','Intake'),
  ('assessment_type','developmental','tr','Gelişimsel'), ('assessment_type','developmental','en','Developmental'),
  ('assessment_type','progress','tr','İlerleme'), ('assessment_type','progress','en','Progress'),
  ('assessment_type','diagnostic','tr','Tanısal'), ('assessment_type','diagnostic','en','Diagnostic'),
  ('assessment_type','exit','tr','Çıkış Değerlendirmesi'), ('assessment_type','exit','en','Exit'),

  ('assessment_category','cognitive','tr','Bilişsel'), ('assessment_category','cognitive','en','Cognitive'),
  ('assessment_category','language','tr','Dil'), ('assessment_category','language','en','Language'),
  ('assessment_category','social_emotional','tr','Sosyal-Duygusal'), ('assessment_category','social_emotional','en','Social-Emotional'),
  ('assessment_category','motor','tr','Motor'), ('assessment_category','motor','en','Motor'),
  ('assessment_category','adaptive','tr','Uyumsal'), ('assessment_category','adaptive','en','Adaptive'),
  ('assessment_category','academic','tr','Akademik'), ('assessment_category','academic','en','Academic'),

  ('package_type','session_package','tr','Seans Paketi'), ('package_type','session_package','en','Session Package'),
  ('package_type','program_package','tr','Program Paketi'), ('package_type','program_package','en','Program Package'),
  ('package_type','credit_package','tr','Kredi Paketi'), ('package_type','credit_package','en','Credit Package'),
  ('package_type','camp_package','tr','Kamp Paketi'), ('package_type','camp_package','en','Camp Package'),

  ('scholarship_type','need_based','tr','İhtiyaç Bazlı'), ('scholarship_type','need_based','en','Need Based'),
  ('scholarship_type','merit','tr','Başarı'), ('scholarship_type','merit','en','Merit'),
  ('scholarship_type','sibling','tr','Kardeş İndirimi'), ('scholarship_type','sibling','en','Sibling'),
  ('scholarship_type','staff','tr','Personel'), ('scholarship_type','staff','en','Staff'),

  ('discount_type','percentage','tr','Yüzde'), ('discount_type','percentage','en','Percentage'),
  ('discount_type','fixed_amount','tr','Sabit Tutar'), ('discount_type','fixed_amount','en','Fixed Amount'),
  ('discount_type','sibling','tr','Kardeş'), ('discount_type','sibling','en','Sibling'),
  ('discount_type','early_bird','tr','Erken Kayıt'), ('discount_type','early_bird','en','Early Bird'),

  ('academic_term','fall','tr','Güz'), ('academic_term','fall','en','Fall'),
  ('academic_term','spring','tr','Bahar'), ('academic_term','spring','en','Spring'),
  ('academic_term','summer','tr','Yaz'), ('academic_term','summer','en','Summer'),
  ('academic_term','year_round','tr','Yıl Boyu'), ('academic_term','year_round','en','Year Round'),

  ('report_category','clinical','tr','Klinik'), ('report_category','clinical','en','Clinical'),
  ('report_category','administrative','tr','İdari'), ('report_category','administrative','en','Administrative'),
  ('report_category','financial','tr','Finansal'), ('report_category','financial','en','Financial'),
  ('report_category','operational','tr','Operasyonel'), ('report_category','operational','en','Operational'),

  ('service_type','therapy','tr','Terapi'),               ('service_type','therapy','en','Therapy'),
  ('service_type','education','tr','Eğitim'),             ('service_type','education','en','Education'),
  ('service_type','consultation','tr','Danışmanlık'),     ('service_type','consultation','en','Consultation'),
  ('service_type','camp','tr','Kamp'),                    ('service_type','camp','en','Camp'),
  ('service_type','online','tr','Online'),                ('service_type','online','en','Online'),

  ('program_type','individual_education','tr','Bireysel Eğitim'), ('program_type','individual_education','en','Individual Education'),
  ('program_type','group_education','tr','Grup Eğitimi'),         ('program_type','group_education','en','Group Education'),
  ('program_type','therapy','tr','Terapi'),                       ('program_type','therapy','en','Therapy'),
  ('program_type','camp','tr','Kamp'),                            ('program_type','camp','en','Camp'),
  ('program_type','online','tr','Online'),                        ('program_type','online','en','Online'),

  ('lead_status','new','tr','Yeni'),               ('lead_status','new','en','New'),
  ('lead_status','contacted','tr','İletişime Geçildi'), ('lead_status','contacted','en','Contacted'),
  ('lead_status','qualified','tr','Nitelikli'),    ('lead_status','qualified','en','Qualified'),
  ('lead_status','interview','tr','Görüşme'),      ('lead_status','interview','en','Interview'),
  ('lead_status','converted','tr','Dönüştürüldü'), ('lead_status','converted','en','Converted'),
  ('lead_status','lost','tr','Kaybedildi'),        ('lead_status','lost','en','Lost'),

  ('pipeline_stage','prospect','tr','Aday'),                 ('pipeline_stage','prospect','en','Prospect'),
  ('pipeline_stage','contacted','tr','İletişime Geçildi'),   ('pipeline_stage','contacted','en','Contacted'),
  ('pipeline_stage','assessment','tr','Değerlendirme'),      ('pipeline_stage','assessment','en','Assessment'),
  ('pipeline_stage','recommendation','tr','Öneri'),          ('pipeline_stage','recommendation','en','Recommendation'),
  ('pipeline_stage','enrollment','tr','Kayıt'),              ('pipeline_stage','enrollment','en','Enrollment'),

  ('activity_type','call','tr','Telefon'),   ('activity_type','call','en','Call'),
  ('activity_type','email','tr','E-posta'),  ('activity_type','email','en','Email'),
  ('activity_type','sms','tr','SMS'),        ('activity_type','sms','en','SMS'),
  ('activity_type','note','tr','Not'),       ('activity_type','note','en','Note'),
  ('activity_type','visit','tr','Ziyaret'),  ('activity_type','visit','en','Visit'),

  ('attendance_reason','illness','tr','Hastalık'),       ('attendance_reason','illness','en','Illness'),
  ('attendance_reason','family','tr','Aile'),            ('attendance_reason','family','en','Family'),
  ('attendance_reason','transport','tr','Ulaşım'),       ('attendance_reason','transport','en','Transport'),
  ('attendance_reason','weather','tr','Hava Koşulları'), ('attendance_reason','weather','en','Weather'),
  ('attendance_reason','other','tr','Diğer'),            ('attendance_reason','other','en','Other'),

  ('missed_reason','illness','tr','Hastalık'),     ('missed_reason','illness','en','Illness'),
  ('missed_reason','family','tr','Aile'),          ('missed_reason','family','en','Family'),
  ('missed_reason','no_show','tr','Gelmedi'),      ('missed_reason','no_show','en','No Show'),
  ('missed_reason','holiday','tr','Tatil'),        ('missed_reason','holiday','en','Holiday'),

  ('guardian_relationship','mother','tr','Anne'),                 ('guardian_relationship','mother','en','Mother'),
  ('guardian_relationship','father','tr','Baba'),                 ('guardian_relationship','father','en','Father'),
  ('guardian_relationship','grandparent','tr','Büyükanne/Büyükbaba'), ('guardian_relationship','grandparent','en','Grandparent'),
  ('guardian_relationship','legal_guardian','tr','Yasal Vasi'),   ('guardian_relationship','legal_guardian','en','Legal Guardian'),
  ('guardian_relationship','sibling','tr','Kardeş'),              ('guardian_relationship','sibling','en','Sibling'),

  ('diagnosis_category','autism_spectrum','tr','Otizm Spektrum'),           ('diagnosis_category','autism_spectrum','en','Autism Spectrum'),
  ('diagnosis_category','down_syndrome','tr','Down Sendromu'),              ('diagnosis_category','down_syndrome','en','Down Syndrome'),
  ('diagnosis_category','learning_disability','tr','Öğrenme Güçlüğü'),      ('diagnosis_category','learning_disability','en','Learning Disability'),
  ('diagnosis_category','speech_disorder','tr','Konuşma Bozukluğu'),        ('diagnosis_category','speech_disorder','en','Speech Disorder'),
  ('diagnosis_category','adhd','tr','DEHB'),                                ('diagnosis_category','adhd','en','ADHD'),
  ('diagnosis_category','cerebral_palsy','tr','Serebral Palsi'),            ('diagnosis_category','cerebral_palsy','en','Cerebral Palsy'),

  ('development_area','cognitive','tr','Bilişsel'),                 ('development_area','cognitive','en','Cognitive'),
  ('development_area','language','tr','Dil'),                       ('development_area','language','en','Language'),
  ('development_area','social_emotional','tr','Sosyal-Duygusal'),   ('development_area','social_emotional','en','Social-Emotional'),
  ('development_area','motor','tr','Motor'),                        ('development_area','motor','en','Motor'),
  ('development_area','self_care','tr','Öz Bakım'),                 ('development_area','self_care','en','Self Care'),

  ('goal_category','communication','tr','İletişim'), ('goal_category','communication','en','Communication'),
  ('goal_category','behavior','tr','Davranış'),      ('goal_category','behavior','en','Behavior'),
  ('goal_category','academic','tr','Akademik'),      ('goal_category','academic','en','Academic'),
  ('goal_category','motor','tr','Motor'),            ('goal_category','motor','en','Motor'),
  ('goal_category','social','tr','Sosyal'),          ('goal_category','social','en','Social'),

  ('room_type','therapy_room','tr','Terapi Odası'), ('room_type','therapy_room','en','Therapy Room'),
  ('room_type','classroom','tr','Sınıf'),           ('room_type','classroom','en','Classroom'),
  ('room_type','online_room','tr','Online Oda'),    ('room_type','online_room','en','Online Room'),
  ('room_type','gym','tr','Spor Salonu'),           ('room_type','gym','en','Gym'),

  ('educator_relationship','supervises','tr','Süpervizyon'),     ('educator_relationship','supervises','en','Supervises'),
  ('educator_relationship','consults_for','tr','Danışmanlık'),   ('educator_relationship','consults_for','en','Consults For'),
  ('educator_relationship','coordinates','tr','Koordinasyon'),   ('educator_relationship','coordinates','en','Coordinates'),

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
  ('notification_channel','in_app','tr','Uygulama İçi'), ('notification_channel','in_app','en','In-App'),

  ('integration_kind','email','tr','E-posta'),           ('integration_kind','email','en','Email'),
  ('integration_kind','sms','tr','SMS'),                 ('integration_kind','sms','en','SMS'),
  ('integration_kind','payment','tr','Ödeme'),           ('integration_kind','payment','en','Payment'),
  ('integration_kind','streaming','tr','Yayın'),         ('integration_kind','streaming','en','Streaming'),
  ('integration_kind','erp','tr','ERP'),                 ('integration_kind','erp','en','ERP'),
  ('integration_kind','government','tr','Kamu'),         ('integration_kind','government','en','Government'),
  ('integration_kind','identity','tr','Kimlik Doğrulama'), ('integration_kind','identity','en','Identity')
) as t(type_code, value_code, locale, label)
join ref.ref_value rv
  on rv.ref_type_id = ref.type_id(t.type_code)
 and rv.code = t.value_code
 and rv.corporation_id is null
on conflict (ref_value_id, locale) do nothing;


-- =============================================================================
-- Source: db/seed/02_akran_bootstrap.sql
-- =============================================================================

-- =====================================================================
-- AyNesil Platform :: Seed — Akran Hareket tenant bootstrap (example)
-- Demonstrates: corporation + campuses, base RBAC, tenant-specific reference
-- values, and a tenant override of a global value. Run as table owner.
-- =====================================================================

-- ---------------------------------------------------------------------
-- Corporation + campuses
-- ---------------------------------------------------------------------
insert into core.corporation(code, legal_name, display_name, default_locale, default_currency, timezone)
values ('akran','Akran Hareket Özel Eğitim','Akran Hareket','tr','TRY','Europe/Istanbul')
on conflict (code) do nothing;

insert into core.campus(corporation_id, code, name, city)
select c.id, v.code, v.name, v.city
from core.corporation c,
     (values ('ETLK','Etiler Kampüs','İstanbul'),
             ('KDKY','Kadıköy Kampüs','İstanbul'),
             ('ANK','Ankara Kampüs','Ankara')) as v(code,name,city)
where c.code = 'akran'
on conflict (corporation_id, code) do nothing;

-- ---------------------------------------------------------------------
-- Permission catalog (sample) + admin role
-- ---------------------------------------------------------------------
insert into iam.permission(code, resource, action) values
  ('student:read','student','read'),
  ('student:write','student','write'),
  ('session:read','session','read'),
  ('session:write','session','write'),
  ('finance:read','finance','read'),
  ('finance:write','finance','write'),
  ('refdata:manage','refdata','manage')
on conflict (code) do nothing;

insert into iam.role(corporation_id, code, name, is_system)
select c.id, 'admin', 'Administrator', true
from core.corporation c where c.code = 'akran'
on conflict (corporation_id, code) do nothing;

insert into iam.role_permission(role_id, permission_id)
select r.id, p.id
from iam.role r
join core.corporation c on c.id = r.corporation_id and c.code = 'akran'
cross join iam.permission p
where r.code = 'admin'
on conflict do nothing;

-- ---------------------------------------------------------------------
-- Tenant-SPECIFIC reference values (only Akran sees these)
--   e.g. a custom therapy type + a custom session type
-- ---------------------------------------------------------------------
insert into ref.ref_value(ref_type_id, corporation_id, code, sort_order, is_active)
select ref.type_id(v.type_code), c.id, v.code, v.sort_order, true
from core.corporation c,
     (values ('therapy_type','hydrotherapy',7),
             ('session_type','home_visit',6)) as v(type_code, code, sort_order)
where c.code = 'akran'
on conflict do nothing;

insert into ref.ref_value_translation(ref_value_id, locale, label)
select rv.id, t.locale, t.label
from core.corporation c
join ref.ref_value rv on rv.corporation_id = c.id
join (values ('therapy_type','hydrotherapy','tr','Hidroterapi'),
             ('therapy_type','hydrotherapy','en','Hydrotherapy'),
             ('session_type','home_visit','tr','Ev Ziyareti'),
             ('session_type','home_visit','en','Home Visit'))
       as t(type_code, value_code, locale, label)
  on rv.ref_type_id = ref.type_id(t.type_code) and rv.code = t.value_code
where c.code = 'akran'
on conflict (ref_value_id, locale) do nothing;

-- ---------------------------------------------------------------------
-- Tenant OVERRIDE of a GLOBAL value (deactivate 'online' session type for Akran,
-- and push 'group' to the top) WITHOUT mutating the shared rows.
-- ---------------------------------------------------------------------
insert into ref.ref_value_tenant_override(corporation_id, ref_value_id, is_active, sort_order)
select c.id, rv.id, false, null
from core.corporation c
join ref.ref_value rv on rv.ref_type_id = ref.type_id('session_type')
                     and rv.code = 'online' and rv.corporation_id is null
where c.code = 'akran'
on conflict (corporation_id, ref_value_id) do nothing;

