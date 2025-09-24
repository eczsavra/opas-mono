# OPAS — PLAN V1 (Frozen Scope)

## Non-Negotiable Kurallar
- Single pharmacy: Türkiye mevzuatı → eczane **şubeleşmez**.
- Tenant = Pharmacy (GLN). **Her eczane için ayrı DB (DB-per-tenant)**.
- Medula/İTS çağrıları **sabit 3 egress IP** üzerinden yapılır.
- Frontend: **Next.js + Vuexy**. Varsayılan dil: **tr**.

## Çekirdek Modüller (V1)
1. **Auth & Account** (GLN + password login, forgot password, basic roles)
2. **Medula** (sabit IP’den giden entegrasyon kapısı, queue/retry, log)
3. **İTS** (sabit IP’den eczane işlemleri, log ve kanıt izi)
4. **Sales** (dispensing/OTC satış akışı; ileride e-Reçete ayrı pakette)
5. **Purchasing** (inbound: depolar, pazaryerleri, diğer eczaneler → giriş)
6. **Owner Console (Superadmin)** (sistem sahibi için tüm eczaneleri gören yönetim)
7. **AI Assistant & Insights** (öneriler, taktikler; stok/satış/alış içgörüleri)
8. **Catalog** (tenant’a kopyalanan ilaç/ürün listesi; eczane özelleştirebilir)
9. **Integrations Hub** (ÜTS, Cetas, FarmaInbox, Private Insurers, NVİ, MERNİS, GİB…)
10. **Inventory (Stock)** (lot/seri, SKT, giriş-çıkış, sayım, low-stock uyarıları)
11. **Reporting & Analytics** (satış, stok, SKT yaklaşanlar, performans)
12. **Audit & Logging** (kim, ne yaptı; her işlem izlenebilir)
13. **Settings** (eczane bilgisi, GLN, yetkiler, vergiler, parametreler)

## Mimari Taşlar (V1 için gerekli)
- Control Plane DB (Tenant registry: GLN → tenant connection)
- Tenant DB-per-tenant (register ile otomatik oluşturma + migration + seed/copy)
- Tenant Connection Resolver (GLN → doğru DB seçimi; caching)
- Egress/NAT (Medula/İTS sabit IP çıkış; HA/DR stratejisi)
- Consistent Error/ProblemDetails + i18n (tr default)
- Observability (Serilog min JSON; request/tenant id; entegrasyon logları)

## V1 Dikey Dilim Sırası (küçük adımlar)
- V1-A: Control Plane DB + Tenant Registry (GLN, status, conn)
- V1-B: Tenant Provisioner (register → DB oluştur + migrate + initial copy)
- V1-C: Auth (GLN-password login, forgot, basic roles) + /login UI (Vuexy)
- V1-D: Catalog → tenant DB’ye kopya + ürün listesi UI
- V1-E: Inventory minimal (giriş/çıkış) + temel raporlar
- V1-F: Egress katmanı (sabit IP üzerinden dummy health ping), log görünürlüğü
- V1-G: Medula/İTS kılıf uçları (dry-run; gerçek sahaya geçiş öncesi)
- V1-H: Owner Console (temel listeler, aramalar, inceleme ekranları)
- V1-I: AI first-light (basit öneriler; SKU-rotation, low-stock, fiyat)

## V1 Dışı (PLAN-LATER.md’ye)
- e-Reçete tam akış, gelişmiş promosyon/price engine, POS/ÖKC, mobil uygulama vb.

## Riskler / Notlar
- Sabit IP limiti (3) → HA/NAT tasarımı ve felaket senaryoları.
- Tenant DB yedekleme/geri yükleme; veri taşıma (GLN değişimi).
- Entegrasyon SLA’leri ve ceza koşulları (retry/backoff/queue).
