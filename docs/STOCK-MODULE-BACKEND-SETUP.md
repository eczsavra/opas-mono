# 📦 **STOK MODÜLÜ BACKEND - KURULUM DOKÜMANI**

**Tarih:** 6 Ocak 2025, 21:30  
**Durum:** ✅ Backend Temel Altyapı Tamamlandı

---

## 🎯 **YAPILAN İŞLEMLER**

### **1. Veritabanı Tabloları Oluşturuldu**

#### **Yeni Tablolar (Tenant DB'sinde):**

```
✅ movement_types          → Hareket tipleri (referans)
✅ storage_locations       → Raf/lokasyon takibi
✅ stock_movements         → Stok hareketleri (DEFTER - append-only)
✅ stock_items_serial      → İlaç kutu takibi (seri no bazlı)
✅ stock_batches           → OTC ürün parti takibi
✅ stock_summary           → Stok özet bilgisi (cache)
```

#### **Products Tablosuna Eklenen Kolonlar:**

```
✅ category                → 'DRUG' veya 'NON_DRUG'
✅ has_datamatrix          → Karekodu var mı?
✅ requires_expiry_tracking → SKT takibi gerekli mi?
✅ is_controlled           → Kontrollü ilaç mı?
```

---

### **2. Otomatik Fonksiyonlar Eklendi**

```sql
✅ generate_stock_movement_number()  → STK-2025-00001 formatında numara
✅ generate_batch_number()           → BATCH-2025-001 formatında numara
```

---

### **3. TenantProvisioningService Güncellendi**

**Yeni tenant oluşturulduğunda artık:**
- ✅ Stok tabloları otomatik oluşturuluyor
- ✅ Hareket tipleri otomatik yükleniyor (15 adet varsayılan tip)
- ✅ Products tablosuna stok kolonları ekleniyor

**Dosya:** `src/Opas.Infrastructure/Services/TenantProvisioningService.cs`

---

### **4. DTO'lar Oluşturuldu**

**Yeni DTO Dosyaları:**

```
✅ src/Opas.Shared/Stock/StockMovementDto.cs
   - StockMovementDto
   - CreateStockMovementRequest
   - StockMovementResponse
   - StockMovementsResponse

✅ src/Opas.Shared/Stock/StockSummaryDto.cs
   - StockSummaryDto
   - StockSummaryListResponse
   - SingleStockSummaryResponse

✅ src/Opas.Shared/Stock/BatchDto.cs
   - BatchDto
   - CreateBatchRequest
   - BatchResponse
   - BatchListResponse
```

---

### **5. API Endpoint'leri Oluşturuldu**

**Stok Hareketleri Endpoint'leri:**

```
✅ GET  /api/tenant/stock/movements
   → Stok hareketlerini listele (filtreleme: tip, tarih aralığı)
   
✅ GET  /api/tenant/stock/movements/{id}
   → Tek bir hareket detayı
   
✅ POST /api/tenant/stock/movements
   → Yeni stok hareketi oluştur
   
✅ GET  /api/tenant/stock/movements/product/{productId}
   → Ürüne göre hareketleri listele
```

**Dosya:** `src/Opas.Api/Endpoints/StockMovementEndpoints.cs`

**Endpoint Kaydı:** `src/Opas.Api/Program.cs` (line 200)

---

## 📊 **HAREKET TİPLERİ (15 Adet Varsayılan)**

### **Satış Hareketleri (OUT):**
```
SALE_RETAIL        → Perakende Satış
SALE_PRESCRIPTION  → Reçeteli Satış (SGK/Medula)
SALE_INSURANCE     → Özel Sigorta Satışı
SALE_CONSIGNMENT   → Emanet Satış
SALE_CREDIT        → Veresiye Satış
```

### **Alış Hareketleri (IN):**
```
PURCHASE_DEPOT       → Depodan Alış
PURCHASE_MARKETPLACE → Pazaryeri Alışı
PURCHASE_OTHER       → Diğer Alış
```

### **İade Hareketleri:**
```
CUSTOMER_RETURN  → Müşteri İadesi (IN)
RETURN_TO_DEPOT  → Depoya İade (OUT)
```

### **Takas Hareketleri:**
```
EXCHANGE_IN   → Takas Alış (IN)
EXCHANGE_OUT  → Takas Satış (OUT)
```

### **Diğer Hareketler:**
```
WASTE       → Zayi/Fire (OUT)
UNKNOWN_IN  → Sebebi Bilinmeyen Giriş (IN)
CORRECTION  → Manuel Düzeltme
```

---

## 🔧 **YENİ TENANT TESTI**

### **Test Senaryosu:**

1. **Yeni bir eczacı kaydı oluştur**
2. **Sistem otomatik olarak stok tablolarını oluşturacak**
3. **Hareket tipleri otomatik yüklenecek**

### **Doğrulama SQL:**

```sql
-- Tenant DB'ye bağlan (örnek: opas_tenant_8680001530144)

-- Stok tablolarını kontrol et
SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name LIKE 'stock_%' 
OR table_name IN ('movement_types', 'storage_locations');

-- Hareket tiplerini kontrol et
SELECT code, name, direction, category FROM movement_types;

-- Products tablosuna yeni kolonları kontrol et
SELECT column_name FROM information_schema.columns 
WHERE table_name = 'products' 
AND column_name IN ('category', 'has_datamatrix', 'requires_expiry_tracking', 'is_controlled');
```

---

## 🧪 **TEST ENDPOINT'LERİ**

### **1. Stok Hareketi Oluştur (POST)**

```http
POST http://127.0.0.1:5080/api/tenant/stock/movements?tenantId=TNT_8680001530144&username=test_user
Content-Type: application/json

{
  "movementType": "PURCHASE_DEPOT",
  "productId": "product-uuid-here",
  "quantityChange": 100,
  "unitCost": 30.00,
  "serialNumber": "12345ABCD",
  "lotNumber": "2024A",
  "expiryDate": "2026-12-31",
  "gtin": "8680001234567",
  "notes": "İlk alış"
}
```

### **2. Stok Hareketlerini Listele (GET)**

```http
GET http://127.0.0.1:5080/api/tenant/stock/movements?tenantId=TNT_8680001530144&page=1&pageSize=50
```

### **3. Filtreli Liste (Tarih + Tip)**

```http
GET http://127.0.0.1:5080/api/tenant/stock/movements?tenantId=TNT_8680001530144&movementType=SALE_RETAIL&startDate=2025-01-01&endDate=2025-01-31
```

### **4. Ürüne Göre Hareketler**

```http
GET http://127.0.0.1:5080/api/tenant/stock/movements/product/{productId}?tenantId=TNT_8680001530144
```

---

## 📋 **SONRAKİ ADIMLAR**

### **Yapılacaklar (Öncelik Sırası):**

#### **Backend:**
```
⏳ Stock Summary Endpoint'leri
   - GET /api/tenant/stock/summary
   - GET /api/tenant/stock/summary/{productId}
   
⏳ Batch Management Endpoint'leri
   - POST /api/tenant/stock/batches (OTC batch oluştur)
   - GET /api/tenant/stock/batches/product/{productId}
   
⏳ Storage Location Endpoint'leri
   - POST /api/tenant/stock/locations (Raf ekle)
   - GET /api/tenant/stock/locations
   
⏳ Stock Summary Güncelleme Servisi
   - Her stok hareketinden sonra summary tablosunu güncelle
   - Trigger veya kod ile
```

#### **Frontend:**
```
⏳ Stok Modülü Sayfaları
   - Stok Listesi Sayfası
   - Stok Hareketleri Sayfası
   - Batch Yönetim Sayfası
   - Lokasyon Yönetim Sayfası
```

#### **Entegrasyon:**
```
⏳ Satış Modülü Entegrasyonu
   - Satış yapılınca otomatik stok hareketi
   - Karekod/barkod okutma entegrasyonu
```

---

## 🎉 **ÖZET**

**✅ Tamamlanan:**
- Veritabanı mimarisi (%100)
- DTO'lar (%100)
- Stok Hareketleri API (%100) - 4 endpoint
- Stock Summary API (%100) - 4 endpoint ✨ YENİ
- Batch Management API (%100) - 5 endpoint ✨ YENİ
- Tenant Provisioning (%100)

**⏳ Devam Eden:**
- Auto-Update Service (summary otomatik güncelleme) (%0)
- Frontend Sayfaları (%0)
- Satış Entegrasyonu (%0)

**📊 Genel İlerleme:** %70 (Backend API'ler tamamlandı!)

---

## 📋 **TAMAMLANAN ENDPOINT'LER (13 Adet)**

### **Stock Movements (4):**
- GET /api/tenant/stock/movements
- GET /api/tenant/stock/movements/{id}
- POST /api/tenant/stock/movements
- GET /api/tenant/stock/movements/product/{productId}

### **Stock Summary (4):**
- GET /api/tenant/stock/summary
- GET /api/tenant/stock/summary/{productId}
- POST /api/tenant/stock/summary/recalculate/{productId}
- GET /api/tenant/stock/summary/alerts

### **Stock Batches (5):**
- GET /api/tenant/stock/batches/product/{productId}
- POST /api/tenant/stock/batches
- PUT /api/tenant/stock/batches/{batchId}/quantity
- GET /api/tenant/stock/batches/active
- GET /api/tenant/stock/batches/expiring-soon

---

**Son Güncelleme:** 6 Ocak 2025, 22:00  
**Durum:** ✅ Backend API'ler %100 tamamlandı! Test edilmeye hazır!

