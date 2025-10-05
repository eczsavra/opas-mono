'use client'

import { createContext, useContext, useState, useEffect, ReactNode } from 'react'

// Tab renk paleti (100 colors for extensive tab support)
const TAB_COLORS = [
  '#1976d2', '#2e7d32', '#ed6c02', '#9c27b0', '#d32f2f', '#0288d1', '#7b1fa2', '#c2185b',
  '#00897b', '#5e35b1', '#f57c00', '#c62828', '#00695c', '#4527a0', '#6a1b9a', '#ad1457',
  '#558b2f', '#d84315', '#01579b', '#4a148c', '#bf360c', '#33691e', '#1a237e', '#311b92',
  '#006064', '#e65100', '#b71c1c', '#880e4f', '#1b5e20', '#4e342e', '#263238', '#3e2723',
  '#0277bd', '#388e3c', '#f57f17', '#7b1fa2', '#c62828', '#0097a7', '#512da8', '#d81b60',
  '#00796b', '#5e35b1', '#ef6c00', '#ad1457', '#00695c', '#6a1b9a', '#4527a0', '#c2185b',
  '#00838f', '#6a1b9a', '#ff6f00', '#880e4f', '#004d40', '#4a148c', '#bf360c', '#6a1b9a',
  '#00695c', '#7b1fa2', '#e65100', '#ad1457', '#00897b', '#512da8', '#f57c00', '#c2185b',
  '#0288d1', '#43a047', '#fb8c00', '#8e24aa', '#e53935', '#0097a7', '#673ab7', '#ec407a',
  '#26c6da', '#66bb6a', '#ffa726', '#ab47bc', '#ef5350', '#29b6f6', '#7e57c2', '#f06292',
  '#4fc3f7', '#81c784', '#ffb74d', '#ba68c8', '#e57373', '#4dd0e1', '#9575cd', '#f48fb1',
  '#80deea', '#a5d6a7', '#ffcc80', '#ce93d8', '#ef9a9a', '#80cbc4', '#b39ddb', '#ffccbc',
  '#b0bec5', '#c5e1a5', '#ffe0b2', '#e1bee7', '#ffcdd2'
]

interface Product {
  id: string
  gtin: string
  drugName: string
  manufacturerName: string
  price: number
  quantity?: number
}

interface SaleTab {
  id: string
  number: number
  title: string
  searchQuery: string
  color: string
  products: Product[] // Her tab'ın kendi ürün listesi
}

interface SalesContextType {
  saleTabs: SaleTab[]
  activeTab: string | false
  tabCounter: number
  setSaleTabs: React.Dispatch<React.SetStateAction<SaleTab[]>>
  setActiveTab: React.Dispatch<React.SetStateAction<string | false>>
  setTabCounter: React.Dispatch<React.SetStateAction<number>>
  addTab: (tab: SaleTab) => void
  removeTab: (tabId: string) => void
  updateTab: (tabId: string, updates: Partial<SaleTab>) => void
  reorderTabs: (tabs: SaleTab[]) => void
}

const SalesContext = createContext<SalesContextType | undefined>(undefined)

// Helper function to get tenant-specific keys
const getTenantKey = (baseKey: string): string => {
  if (typeof window === 'undefined') return baseKey
  const tenantId = localStorage.getItem('tenantId') || 'default'
  return `${baseKey}_${tenantId}`
}

const STORAGE_KEY = 'opas_sales_tabs'
const ACTIVE_TAB_KEY = 'opas_active_tab'
const TAB_COUNTER_KEY = 'opas_tab_counter'

// localStorage için ayrı key (browser kapansa/elektrik kesse bile kalıcı)
const LOCAL_STORAGE_KEY = 'opas_sales_tabs_persistent'
const LOCAL_ACTIVE_TAB_KEY = 'opas_active_tab_persistent'
const LOCAL_TAB_COUNTER_KEY = 'opas_tab_counter_persistent'

export function SalesProvider({ children }: { children: ReactNode }) {
  const [saleTabs, setSaleTabs] = useState<SaleTab[]>([])
  const [activeTab, setActiveTab] = useState<string | false>(false)
  const [tabCounter, setTabCounter] = useState(1)
  const [isHydrated, setIsHydrated] = useState(false)
  const [isLoadingFromBackend, setIsLoadingFromBackend] = useState(false) // Prevents sync during load

  // Load from localStorage (instant) + backend (persistent) on mount
  useEffect(() => {
    const loadDraftSales = async () => {
      try {
        // Get tenantId from localStorage
        const tenantId = typeof window !== 'undefined' ? localStorage.getItem('tenantId') : null
        
        // ⚠️ KRITIK: If no tenantId, user not logged in → skip loading!
        if (!tenantId) {
          console.log('ℹ️ No tenantId found, skipping draft sales load (user not logged in)')
          setIsHydrated(true)
          return
        }
        
        // 1. INSTANT LOAD: localStorage'dan yükle (instant UI response)
        const persistentTabs = localStorage.getItem(getTenantKey(LOCAL_STORAGE_KEY))
        const persistentActiveTab = localStorage.getItem(getTenantKey(LOCAL_ACTIVE_TAB_KEY))
        const persistentCounter = localStorage.getItem(getTenantKey(LOCAL_TAB_COUNTER_KEY))

        const savedTabs = sessionStorage.getItem(getTenantKey(STORAGE_KEY))
        const savedActiveTab = sessionStorage.getItem(getTenantKey(ACTIVE_TAB_KEY))
        const savedCounter = sessionStorage.getItem(getTenantKey(TAB_COUNTER_KEY))

        const tabsToLoad = savedTabs || persistentTabs
        const activeTabToLoad = savedActiveTab || persistentActiveTab
        const counterToLoad = savedCounter || persistentCounter

        if (tabsToLoad) {
          const tabs = JSON.parse(tabsToLoad)
          setSaleTabs(tabs)
        }

        if (activeTabToLoad) {
          setActiveTab(activeTabToLoad)
        }

        if (counterToLoad) {
          setTabCounter(parseInt(counterToLoad, 10))
        }

        // 2. BACKEND SYNC: Backend'den yükle (if logged in and backend available)
        if (tenantId) {
          try {
            setIsLoadingFromBackend(true) // 🔒 LOCK: Prevent sync while loading
            const response = await fetch(`/api/opas/tenant/draft-sales?tenantId=${tenantId}`)
            
            if (response.ok) {
              const data = await response.json()
              
              if (data.success && data.tabs && data.tabs.length > 0) {
                // Backend'den gelen data'yı frontend formatına çevir
                const backendTabs: SaleTab[] = data.tabs.map((draft: {
                  tabId: string
                  tabLabel: string
                  products: Array<{
                    id: string
                    gtin: string
                    drugName: string
                    manufacturer: string
                    price: number
                    quantity: number
                  }>
                }) => {
                  const tabNumber = parseInt(draft.tabLabel.replace(/[^0-9]/g, ''), 10) || 1
                  const colorIndex = (tabNumber - 1) % TAB_COLORS.length
                  return {
                    id: draft.tabId,
                    number: tabNumber,
                    title: draft.tabLabel,
                    searchQuery: '',
                    color: TAB_COLORS[colorIndex], // ✅ Assign color based on tab number
                    products: draft.products.map((p) => ({
                    id: p.id,
                    gtin: p.gtin,
                    drugName: p.drugName,
                    manufacturerName: p.manufacturer,
                    price: p.price,
                    quantity: p.quantity
                  }))
                }
              })

                setSaleTabs(backendTabs)
                
                // ⚠️ CRITICAL: Validate activeTab - ensure it exists in loaded tabs!
                const loadedTabIds = backendTabs.map(t => t.id)
                const storedActiveTab = typeof window !== 'undefined' 
                  ? (sessionStorage.getItem(getTenantKey(ACTIVE_TAB_KEY)) || localStorage.getItem(getTenantKey(LOCAL_ACTIVE_TAB_KEY)))
                  : null
                
                if (storedActiveTab && loadedTabIds.includes(storedActiveTab)) {
                  // Valid activeTab from storage
                  setActiveTab(storedActiveTab)
                } else if (backendTabs.length > 0) {
                  // Invalid or missing activeTab - default to first tab
                  const firstTabId = backendTabs[0].id
                  setActiveTab(firstTabId)
                  sessionStorage.setItem(getTenantKey(ACTIVE_TAB_KEY), firstTabId)
                  localStorage.setItem(getTenantKey(LOCAL_ACTIVE_TAB_KEY), firstTabId)
                  console.log(`⚠️ ActiveTab mismatch fixed: ${storedActiveTab} → ${firstTabId}`)
                } else {
                  // No tabs at all
                  setActiveTab(false)
                }
                
                // ⚠️ CRITICAL: Restore tabCounter from backend response!
                if (data.tabCounter) {
                  setTabCounter(data.tabCounter)
                  localStorage.setItem(getTenantKey(LOCAL_TAB_COUNTER_KEY), data.tabCounter.toString())
                } else {
                  // Fallback: Calculate max tab number + 1
                  const maxTabNumber = Math.max(...backendTabs.map(t => t.number), 0)
                  setTabCounter(maxTabNumber + 1)
                  localStorage.setItem(getTenantKey(LOCAL_TAB_COUNTER_KEY), (maxTabNumber + 1).toString())
                }
                
                // localStorage'ı backend data ile güncelle
                localStorage.setItem(getTenantKey(LOCAL_STORAGE_KEY), JSON.stringify(backendTabs))
                
                console.log(`✅ Loaded ${backendTabs.length} draft sales from backend, tabCounter: ${data.tabCounter || 'calculated'}`)
              }
            } else {
              console.log('ℹ️ Backend unreachable, using local data')
            }
          } catch (backendError) {
            console.log('ℹ️ Backend sync failed, using local data:', backendError)
          } finally {
            setIsLoadingFromBackend(false) // 🔓 UNLOCK: Load complete
          }
        }
      } catch (error) {
        console.error('Failed to load sales tabs:', error)
      } finally {
        setIsHydrated(true)
      }
    }

    loadDraftSales()
  }, [])

  // Save to localStorage + backend sync whenever state changes
  useEffect(() => {
    if (!isHydrated) return
    if (isLoadingFromBackend) return // 🚫 SKIP: Don't sync while loading from backend!

    const syncToBackend = async () => {
      try {
        const tabsJson = JSON.stringify(saleTabs)
        const activeTabValue = activeTab ? activeTab : ''
        const counterValue = tabCounter.toString()

        // 1. INSTANT SAVE: localStorage + sessionStorage (instant, offline-safe)
        sessionStorage.setItem(getTenantKey(STORAGE_KEY), tabsJson)
        sessionStorage.setItem(getTenantKey(ACTIVE_TAB_KEY), activeTabValue)
        sessionStorage.setItem(getTenantKey(TAB_COUNTER_KEY), counterValue)

        localStorage.setItem(getTenantKey(LOCAL_STORAGE_KEY), tabsJson)
        localStorage.setItem(getTenantKey(LOCAL_ACTIVE_TAB_KEY), activeTabValue)
        localStorage.setItem(getTenantKey(LOCAL_TAB_COUNTER_KEY), counterValue)

        // 2. BACKGROUND SYNC: Backend'e sync et (if logged in)
        const tenantId = typeof window !== 'undefined' ? localStorage.getItem('tenantId') : null
        const username = typeof window !== 'undefined' ? localStorage.getItem('username') : null

        // ⚠️ SYNC: Always sync (even if empty) so backend knows tabs are closed!
        if (tenantId && username) {
          // Frontend formatını backend formatına çevir
          const draftSales = saleTabs.map(tab => ({
            tabId: tab.id,
            tabLabel: tab.title,
            products: tab.products.map(p => ({
              id: p.id,
              gtin: p.gtin,
              drugName: p.drugName,
              manufacturer: p.manufacturerName,
              price: p.price,
              quantity: p.quantity || 1,
              totalPrice: (p.price * (p.quantity || 1))
            })),
            isCompleted: false,
            createdBy: username,
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString()
          }))

          try {
            await fetch(`/api/opas/tenant/draft-sales/sync?tenantId=${tenantId}`, {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({
                tabs: draftSales,
                activeTabId: activeTabValue,
                tabCounter: parseInt(counterValue, 10)
              })
            })
            // Silent success (background sync)
          } catch {
            // Silent fail (localStorage is already saved, backend will sync later)
            console.log('Background sync deferred (offline or backend unavailable)')
          }
        }
      } catch (error) {
        console.error('Failed to save sales tabs:', error)
      }
    }

    syncToBackend()
  }, [saleTabs, activeTab, tabCounter, isHydrated, isLoadingFromBackend])

  const addTab = (tab: SaleTab) => {
    setSaleTabs(prev => [...prev, tab])
    setActiveTab(tab.id)
  }

  const removeTab = (tabId: string) => {
    setSaleTabs(prev => {
      const newTabs = prev.filter(tab => tab.id !== tabId)
      
      // Eğer kapatılan tab aktif tab ise, başka bir tab'ı aktif yap
      if (activeTab === tabId) {
        if (newTabs.length > 0) {
          setActiveTab(newTabs[newTabs.length - 1].id)
        } else {
          setActiveTab(false)
        }
      }
      
      return newTabs
    })
  }

  const updateTab = (tabId: string, updates: Partial<SaleTab>) => {
    setSaleTabs(prev => prev.map(tab => 
      tab.id === tabId ? { ...tab, ...updates } : tab
    ))
  }

  const reorderTabs = (tabs: SaleTab[]) => {
    setSaleTabs(tabs)
  }

  return (
    <SalesContext.Provider
      value={{
        saleTabs,
        activeTab,
        tabCounter,
        setSaleTabs,
        setActiveTab,
        setTabCounter,
        addTab,
        removeTab,
        updateTab,
        reorderTabs,
      }}
    >
      {children}
    </SalesContext.Provider>
  )
}

export function useSalesContext() {
  const context = useContext(SalesContext)
  if (!context) {
    throw new Error('useSalesContext must be used within a SalesProvider')
  }
  return context
}

