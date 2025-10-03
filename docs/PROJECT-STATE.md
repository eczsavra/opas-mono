## OPAS Projesi - Detaylı Durum Raporu (Güncel Durum)

Bu dosya, projenin mevcut mimarisini, veri tabanı yapısını, kayıt (registration) akışını, entegrasyonları ve bilinen kararları tek yerde özetler. Düzenli olarak güncellenecektir.

### 1) Teknoloji ve Mimari
- Backend: .NET 8 (ASP.NET Core Minimal API)
- Frontend: Next.js 14 + React 18 + TypeScript + MUI
- Veritabanı: PostgreSQL (Docker, DB-per-tenant)
- ORM: EF Core 8
- Logging: Serilog + özel OPAS logging servisleri
- Entegrasyonlar: ITS, NVI (dev’de bypass opsiyonu)

### 2) Veritabanları ve Tablolar

#### A) Control Plane DB: `opas_control`
- tenants (kayıtlar: 2)
  - t_id: `TNT_` + GLN (örn: TNT_8680001530144)
  - gln, type (eczane), eczane_adi, ili, ilcesi
  - ad, soyad, tc_no, dogum_yili
  - email, isEmailVerified, cep_tel, isCepTelVerified
  - username, password (şimdilik plain text)
  - isActive, isNviVerified, isCompleted
  - kayit_olusturulma_zamani, kayit_guncellenme_zamani, kayit_silinme_zamani

- tenants_usernames (kayıtlar: 2)
  - t_id, username (benzersizlik kontrolü için)

- token_store: ITS token saklama (timestamped adlandırma)
- super_admins: Yönetici hesapları
- __EFMigrationsHistory

#### B) Public DB: `opas_public`
- central_products (34,533 kayıt)
  - gtin, drug_name, manufacturer_gln, manufacturer_name
  - active, imported, created_at_utc, updated_at_utc, is_deleted

- gln_registry (41,843 kayıt)
  - GLN ve eczane/paydaş kimlik bilgileri (aktiflik dâhil)

- __EFMigrationsHistory

#### C) Tenant DB’ler: `opas_tenant_<GLN>`
Her tenant için 3 tablo:
- products (34,533 kayıt; merkezi ürünlerden beslenir)
- gln_list (paydaş listesi; merkezi GLN’lerden beslenir)
- tenant_info (1 kayıt; registration verilerinin kopyası + isCompleted)

Mevcut örnekler:
- opas_tenant_8680001517626
- opas_tenant_8680001530144

### 3) Registration Akışı (6 Adım)
1. GLN doğrulama (GLN registry, “Bu Benim” onayı)
2. Eczacı kimlik (Ad/Soyad/TC/Doğum Yılı) — NVI doğrulama (dev’de bypass mümkün)
3. E‑mail doğrulama (6 haneli kod, 3 dk)
4. SMS doğrulama (6 haneli kod, 3 dk)
5. Kullanıcı adı ve şifre (username min 4; DB’de benzersizlik kontrolü, şifre görünür/gizle)
6. Tamamlama (başarı ekranı + 10 sn sonra `/t-login` yönlendirme)

Kayıt Sonrası Otomasyon:
- Tenant kaydı (tenants + tenants_usernames)
- Tenant DB oluşturma (opas_tenant_<GLN>)
- 3 tablo oluşturma (products, gln_list, tenant_info)
- Products sync (merkezi → tenant)
- GLN sync (merkezi → tenant)
- tenant_info senkronizasyonu ve `isCompleted = true` işaretleme

### 4) Önemli Mimari Kararlar
- Multi-tenancy: DB-per-tenant yaklaşımı
- Tenant ID: Rastgele yerine GLN tabanlı (`TNT_<GLN>`); DB adı `opas_tenant_<GLN>`
- Password: İstek gereği şimdilik plain text (prod’da hash’lenecek)
- ITS entegrasyonu: Token, ürün ve GLN listesi çekme; günlük sync (GLN 05:00, ürün 06:00)
- NVI: Gerçek doğrulama; dev ortamında konfigürasyonla bypass edilebilir

### 5) Frontend UX İyileştirmeleri
- Username gerçek zamanlı uygunluk kontrolü (yeşil/kırmızı; devam butonu engeli)
- Şifre görünür/gizle (göz ikonu)
- Auto‑focus ve Enter ile form onayı
- Uzun işlem uyarısı (4 sn sonra, animasyonlu)
- Başarı sayfası + geri sayım ve yönlendirme

### 6) Servisler ve Job’lar (Önemli)
- TenantProvisioningService: DB oluşturma + tablo kurulum + ilk senkronlar
- TenantProductSyncService: central_products → tenant.products
- TenantGlnSyncService: gln_registry → tenant.gln_list
- CentralProductSyncService: ITS → opas_public.central_products
- ItsTokenService, ItsProductService: ITS entegrasyonu
- NviService: Kimlik doğrulama (retry + dev bypass)
- ProductSyncScheduler (06:00), GlnImportScheduler (05:00)

### 7) Bilinen Notlar ve Riskler
- Parola plain text: Prod’da zorunlu hash
- NVI dev bypass: Prod’da kapatılmalı
- Public DB’de duplicate `__EFMigrationsHistory` kaydı olabilir (temizlenebilir)

### 8) Sayılar (En Son Gözlem)
- opas_control.tenants: 2 kayıt
- opas_control.tenants_usernames: 2 kayıt
- opas_public.central_products: 34,533 kayıt
- opas_public.gln_registry: 41,843 kayıt
- Örnek tenant DB (`opas_tenant_8680001530144`):
  - products: 34,533
  - gln_list: 0
  - tenant_info.isCompleted: false

### 9) Yol Haritası (Öneri)
- Kısa vadede: Login implementasyonu, dashboard başlangıcı
- Orta vadede: Satış modülü (YN ÖKC, stok, iade/iptal)
- Uzun vadede: AI destekli satış ekranı, raporlama


