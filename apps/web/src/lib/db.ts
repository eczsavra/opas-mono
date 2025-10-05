import Dexie, { Table } from 'dexie'

export interface CachedProduct {
  id: string
  gtin: string
  drugName: string
  manufacturerGln: string
  manufacturerName: string
  price: number
  isActive: boolean
  lastSyncAt: Date
  tenantId: string // TENANT-SPECIFIC
}

export interface CacheMetadata {
  key: string
  lastSyncAt: Date
  tenantId: string
}

export class OpasDB extends Dexie {
  products!: Table<CachedProduct>
  metadata!: Table<CacheMetadata>

  constructor() {
    super('opas_offline_db')
    
    this.version(1).stores({
      products: 'id, gtin, drugName, tenantId, [tenantId+drugName], [tenantId+gtin]',
      metadata: 'key, tenantId'
    })
  }
}

export const db = new OpasDB()

