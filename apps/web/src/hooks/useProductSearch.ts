import { useState, useCallback } from 'react'
import { db, CachedProduct } from '@/lib/db'

interface Product {
  id: string
  gtin: string
  drugName: string
  manufacturerName: string
  price: number
  quantity?: number
  category?: string
  unitCost?: number
  stockQuantity?: number
  serialNumber?: string
  expiryDate?: string
  lotNumber?: string
}

interface SearchResult {
  products: Product[]
  source: 'backend' | 'cache'
  error?: string
}

export function useProductSearch() {
  const [isSearching, setIsSearching] = useState(false)

  const searchProducts = useCallback(async (
    query: string,
    pageSize: number = 10
  ): Promise<SearchResult> => {
    if (!query || query.length < 2) {
      return { products: [], source: 'backend' }
    }

    setIsSearching(true)

    try {
      // Get tenantId
      const tenantId = typeof window !== 'undefined' ? localStorage.getItem('tenantId') : null
      
      if (!tenantId) {
        console.warn('No tenantId found, cannot search products')
        return { products: [], source: 'backend', error: 'Not logged in' }
      }

      // 1. TRY BACKEND FIRST
      try {
        const response = await fetch(
          `/api/opas/tenant/products/search?search=${encodeURIComponent(query)}&pageSize=${pageSize}`,
          {
            signal: AbortSignal.timeout(3000) // 3 second timeout
          }
        )

        if (response.ok) {
          const data = await response.json()
          
          // Backend can return: {success: true, data: [...]} OR {success: true, products: [...]} OR {data: [...]}
          const products = data.products || data.data || []
          
          if (products && products.length > 0) {
            // ✅ BACKEND SUCCESS: Cache the results
            await cacheProducts(products, tenantId)
            
            return {
              products: products.map((p: Record<string, unknown>) => ({
                id: (p.product_id as string) || (p.id as string),
                gtin: p.gtin as string,
                drugName: (p.drug_name as string) || (p.drugName as string),
                manufacturerName: (p.manufacturer_name as string) || (p.manufacturerName as string) || '',
                price: (p.price as number) || 0,
                category: (p.category as string) || 'DRUG',
                unitCost: (p.unit_cost as number) || (p.unitCost as number),
                stockQuantity: (p.stock_quantity as number) ?? (p.stockQuantity as number) ?? 0
              })),
              source: 'backend'
            }
          }
        }
      } catch {
        console.log('Backend unreachable, falling back to cache')
      }

      // 2. BACKEND FAILED: USE CACHE
      const cachedProducts = await searchFromCache(query, tenantId, pageSize)
      
      if (cachedProducts.length > 0) {
        console.log(`✅ Found ${cachedProducts.length} products from cache (offline mode)`)
        return {
          products: cachedProducts.map(p => ({
            id: p.id,
            gtin: p.gtin,
            drugName: p.drugName,
            manufacturerName: p.manufacturerName,
            price: p.price
          })),
          source: 'cache'
        }
      }

      // 3. NO RESULTS
      return {
        products: [],
        source: 'cache',
        error: 'No cached products found. Please connect to internet.'
      }

    } catch (error) {
      console.error('Product search error:', error)
      return {
        products: [],
        source: 'backend',
        error: 'Search failed'
      }
    } finally {
      setIsSearching(false)
    }
  }, [])

  return { searchProducts, isSearching }
}

// Helper: Cache products to IndexedDB
async function cacheProducts(products: Array<Record<string, unknown>>, tenantId: string) {
  try {
    const cachedProducts: CachedProduct[] = products.map((p: Record<string, unknown>) => ({
      id: (p.product_id as string) || (p.id as string),
      gtin: p.gtin as string,
      drugName: (p.drug_name as string) || (p.drugName as string),
      manufacturerGln: (p.manufacturer_gln as string) || (p.manufacturerGln as string) || '',
      manufacturerName: (p.manufacturer_name as string) || (p.manufacturerName as string) || '',
      price: (p.price as number) || 0,
      isActive: (p.is_active as boolean) !== false && (p.isActive as boolean) !== false,
      lastSyncAt: new Date(),
      tenantId
    }))

    // Upsert (insert or update)
    await db.products.bulkPut(cachedProducts)
    
    // Update metadata
    await db.metadata.put({
      key: `last_product_sync_${tenantId}`,
      lastSyncAt: new Date(),
      tenantId
    })

    console.log(`💾 Cached ${cachedProducts.length} products for tenant ${tenantId}`)
  } catch (error) {
    console.error('Failed to cache products:', error)
  }
}

// Helper: Search from IndexedDB cache
async function searchFromCache(
  query: string,
  tenantId: string,
  limit: number
): Promise<CachedProduct[]> {
  try {
    const lowerQuery = query.toLowerCase()

    // Search by drug name or GTIN
    const results = await db.products
      .where('tenantId')
      .equals(tenantId)
      .and(p => 
        p.drugName.toLowerCase().includes(lowerQuery) ||
        p.gtin.includes(query)
      )
      .limit(limit)
      .toArray()

    return results
  } catch (error) {
    console.error('Cache search error:', error)
    return []
  }
}

