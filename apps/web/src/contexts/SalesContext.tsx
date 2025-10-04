'use client'

import { createContext, useContext, useState, useEffect, ReactNode } from 'react'

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

  // Load from localStorage (önce) ve sessionStorage on mount
  useEffect(() => {
    try {
      // 1. localStorage'dan yükle (browser kapansa bile kalıcı)
      const persistentTabs = localStorage.getItem(LOCAL_STORAGE_KEY)
      const persistentActiveTab = localStorage.getItem(LOCAL_ACTIVE_TAB_KEY)
      const persistentCounter = localStorage.getItem(LOCAL_TAB_COUNTER_KEY)

      // 2. sessionStorage'dan yükle (sayfa içinde gezinirken)
      const savedTabs = sessionStorage.getItem(STORAGE_KEY)
      const savedActiveTab = sessionStorage.getItem(ACTIVE_TAB_KEY)
      const savedCounter = sessionStorage.getItem(TAB_COUNTER_KEY)

      // sessionStorage varsa onu kullan, yoksa localStorage'dan yükle
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
    } catch (error) {
      console.error('Failed to load sales tabs from storage:', error)
    } finally {
      setIsHydrated(true)
    }
  }, [])

  // Save to BOTH sessionStorage and localStorage whenever state changes
  useEffect(() => {
    if (!isHydrated) return

    try {
      const tabsJson = JSON.stringify(saleTabs)
      const activeTabValue = activeTab ? activeTab : ''
      const counterValue = tabCounter.toString()

      // sessionStorage (sayfa içi gezinme için)
      sessionStorage.setItem(STORAGE_KEY, tabsJson)
      sessionStorage.setItem(ACTIVE_TAB_KEY, activeTabValue)
      sessionStorage.setItem(TAB_COUNTER_KEY, counterValue)

      // localStorage (browser kapansa/elektrik kesse bile kalıcı)
      localStorage.setItem(LOCAL_STORAGE_KEY, tabsJson)
      localStorage.setItem(LOCAL_ACTIVE_TAB_KEY, activeTabValue)
      localStorage.setItem(LOCAL_TAB_COUNTER_KEY, counterValue)
    } catch (error) {
      console.error('Failed to save sales tabs to storage:', error)
    }
  }, [saleTabs, activeTab, tabCounter, isHydrated])

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

