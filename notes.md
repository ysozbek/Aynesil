flyway çalışması

# İlk kurulum — otomatik çalışır
docker compose up
# Migration durumu görüntüle
docker compose run --rm aynesil-flyway info
# Checksum doğrula
docker compose run --rm aynesil-flyway validate
# Yeni migration ekle
echo "ALTER TABLE students.student ADD COLUMN IF NOT EXISTS notes text;" \
  > db/migrations/V3__add_student_notes.sql
docker compose run --rm aynesil-flyway migrate
# Sorun varsa onar (başarısız migration satırı)
docker compose run --rm aynesil-flyway repair