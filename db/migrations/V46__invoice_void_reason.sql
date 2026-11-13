-- =============================================================================
-- Aynesil Flyway V46 — finance.invoice.void_reason (Y10)
-- =============================================================================
-- Fatura iptali bir finansal düzeltmedir; gerekçesi kaydedilir ve iptalde zorunludur.
-- Mevcut iptal edilmiş faturaların nedeni kaydedilmemişti: kısıt NOT VALID eklenir (yalnızca yeni/güncellenen
-- satırları denetler), geçmiş kayıtlara uydurma neden yazılmaz.
-- =============================================================================

alter table finance.invoice add column if not exists void_reason text;

do $$
begin
  if not exists (select 1 from pg_constraint where conname = 'ck_invoice_void_reason'
                  and conrelid = 'finance.invoice'::regclass) then
    alter table finance.invoice
      add constraint ck_invoice_void_reason
      check (status <> 'void' or nullif(btrim(void_reason), '') is not null) not valid;
  end if;
end $$;
