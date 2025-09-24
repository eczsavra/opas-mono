# OPAS TODO / TECH DEBT LOG

Bu dosya *geçici çözümler*, *ileride yapılacaklar* ve *teknik borç* notları içindir.
Her küçük adımda güncelleyelim.

## Açık Maddeler (aktif)
- [ ] MediatR **LuckyPenny lisans uyarısı**: Dev/testte tolere edildi. **Prod öncesi** kesin çözüm (paket kaynağı doğrulama veya lisans anahtarı/alternatif).
- [ ] `infra/health` davranışı: CS boşken ideal yanıt **200 + hasConnectionString:false**. Şu an 200 dönüyor ama istisna yakalama ve log pattern’i gözden geçirilecek.
- [ ] Domain event yapısı: `Entity` vs `BaseEntity` tarafında geçmişte “member hiding” uyarıları vardı. Şu an temiz. **İleride** event publish/disptach mekanizması (Application pipeline) eklenecek.
- [ ] PublicRef → Gerçek Domain’e taşıma planı: Şimdilik Infrastructure’da `ProductRef` tablosu. **İleride** Domain `Product` ile eşleme/mapper ve migration’a geçilecek.
- [ ] Culture-safe parse yerleri: `money` ve sayı parse’larında **invariant + current culture** desteği var; yeni uçlarda aynı standardı uygulayacağız.
- [ ] `/public/products` hatasızlık garantisi: CS yok → 503 JSON; CS var ama DB kapalı → 503 JSON. **Kural**: Bu uçta 500’a düşmeyeceğiz.

## Karar Kayıtları (ADR mini)
- (ADR-001) **Micro-steps** çalışma disiplini: Her adım tek değişiklik + hemen test. Büyük hamle yok.
- (ADR-002) **Mock yok**: Prod-benzeri davranış hedefi. Gerekirse ileride test double eklenecek.
- (ADR-003) **Modüler monolit**: Katmanlı mimari, Net 8, MediatR v13, EF Core 8 + Npgsql.
- (ADR-004) **i18n**: Varsayılan `tr`, namespace’ler İngilizce.

## Yakın Sonraki (kısa vade)
- [ ] Tenant altyapısı: `X-Tenant-Id` → `TenantContext` (sadece taşıma; DB’ye bağlamadan).
- [ ] Domain testleri: `Product.Create`, `Rename`, `SetActive` için 3–4 temel test.
- [ ] `/public/products` arama: ILIKE + GTIN aramasında trim/regex iyileştirmesi.

## Orta Vade
- [ ] Serilog + request-id + minimum JSON log formatı.
- [ ] Docker compose: Postgres + API (dev) — henüz bağlamadan iskelet.
- [ ] Auth (JWT + refresh) — sadece sözleşme/endpoint iskeleti.

## Biten refactorlar
- [x] Tenant endpoint'leri `ControlEndpoints.cs`'ten `ControlTenantEndpoints.cs`'e taşındı
- [x] Infra health endpoint'i `InfraHealthEndpoints.cs`'e taşındı
- [x] Public products endpoint'i `PublicProductsEndpoints.cs`'e taşındı

## Yapıldı (kapatılanlar)
- [x] CS boşken `/public/products`'ta 500 yerine 503 JSON dönmesi sağlandı.
- [x] Validation/Logging pipeline sırası düzeltildi (Logging dış katmanda).
- [x] Application katmanından AspNetCore bağı kaldırıldı (IHttpContextAccessor yok).
