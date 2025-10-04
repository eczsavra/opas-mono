'use client'

import { useState } from 'react'
import { 
  Box, 
  Container, 
  Typography, 
  Button, 
  Tabs, 
  Tab, 
  Paper
} from '@mui/material'
import { 
  Add as AddIcon, 
  Close as CloseIcon 
} from '@mui/icons-material'
import TenantSidebar from '@/components/TenantSidebar'
import TenantNavbar from '@/components/TenantNavbar'
import ModernSearchBox from '@/components/sales/ModernSearchBox'
import { useSalesContext } from '@/contexts/SalesContext'

// Tab renk paleti
const TAB_COLORS = [
  '#1976d2', // Mavi
  '#2e7d32', // Yeşil
  '#ed6c02', // Turuncu
  '#9c27b0', // Mor
  '#d32f2f', // Kırmızı
  '#0288d1', // Açık Mavi
  '#7b1fa2', // Koyu Mor
  '#c2185b', // Pembe
  '#f57c00', // Koyu Turuncu
  '#388e3c', // Koyu Yeşil
]

export default function SatisPage() {
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const [navbarOpen, setNavbarOpen] = useState(false)
  
  // Context'ten state'leri al
  const {
    saleTabs,
    activeTab,
    tabCounter,
    setActiveTab,
    setTabCounter,
    addTab,
    removeTab,
    reorderTabs,
  } = useSalesContext()

  const handleSidebarToggle = () => {
    setSidebarOpen(!sidebarOpen)
  }

  const handleNavbarToggle = () => {
    setNavbarOpen(!navbarOpen)
  }

  const handleProductSelect = () => {
    // TODO: Seçilen ürünü satış sepetine ekle
  }

  const handleNewSale = () => {
    const colorIndex = (tabCounter - 1) % TAB_COLORS.length
    const newTab = {
      id: `sale-${Date.now()}`,
      number: tabCounter,
      title: `Satış #${tabCounter}`,
      searchQuery: '',
      color: TAB_COLORS[colorIndex],
      products: [], // Her yeni tab boş ürün listesi ile başlar
    }
    
    addTab(newTab)
    setTabCounter(tabCounter + 1)
  }

  const handleTabChange = (event: React.SyntheticEvent, newValue: string) => {
    setActiveTab(newValue)
  }

  const handleCloseTab = (tabId: string, event: React.MouseEvent) => {
    event.stopPropagation()
    removeTab(tabId)
  }

  const handleDragStart = (e: React.DragEvent, tabId: string) => {
    e.dataTransfer.effectAllowed = 'move'
    e.dataTransfer.setData('text/plain', tabId)
  }

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault()
    e.dataTransfer.dropEffect = 'move'
  }

  const handleDrop = (e: React.DragEvent, targetTabId: string) => {
    e.preventDefault()
    const draggedTabId = e.dataTransfer.getData('text/plain')
    
    if (draggedTabId === targetTabId) return
    
    const draggedIndex = saleTabs.findIndex(tab => tab.id === draggedTabId)
    const targetIndex = saleTabs.findIndex(tab => tab.id === targetTabId)
    
    if (draggedIndex === -1 || targetIndex === -1) return
    
    const newTabs = [...saleTabs]
    const [draggedTab] = newTabs.splice(draggedIndex, 1)
    newTabs.splice(targetIndex, 0, draggedTab)
    
    reorderTabs(newTabs)
  }

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', backgroundColor: '#f8fafc' }}>
      {/* Tenant Sidebar */}
      <TenantSidebar 
        open={sidebarOpen} 
        onToggle={handleSidebarToggle}
        currentPath="/satis"
      />

      {/* Main Content */}
      <Box 
        component="main" 
        sx={{ 
          flexGrow: 1,
          minHeight: '100vh',
          transition: 'margin 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
          marginLeft: { 
            xs: 0,
            md: sidebarOpen ? '280px' : 0
          }
        }}
      >
        {/* Tenant Navbar */}
        <TenantNavbar 
          open={navbarOpen}
          onToggle={handleNavbarToggle}
          onSidebarToggle={handleSidebarToggle}
        />

        {/* Page Content */}
        <Container 
          maxWidth="xl" 
          sx={{ 
            height: 'calc(100vh - 64px)',
            maxWidth: '100vw',
            overflow: 'hidden',
            display: 'flex',
            flexDirection: 'column',
            py: 2,
            px: 3
          }}
        >
                 {/* Yeni Satış Butonu ve Tab Sistemi */}
                 <Box 
                   sx={{ 
                     flexShrink: 0, // Boyut değişmesin
                     pb: 2,
                     pt: 1
                   }}
                 >
            {/* Yeni Satış Butonu */}
            <Box sx={{ mb: 2 }}>
              <Button
                variant="contained"
                size="large"
                startIcon={<AddIcon />}
                onClick={handleNewSale}
                sx={{
                  borderRadius: 3,
                  px: 4,
                  py: 1.5,
                  fontSize: '1.1rem',
                  fontWeight: 600,
                  background: 'linear-gradient(45deg, #1976d2 30%, #42a5f5 90%)',
                  boxShadow: '0 4px 20px rgba(25, 118, 210, 0.3)',
                  '&:hover': {
                    background: 'linear-gradient(45deg, #1565c0 30%, #1976d2 90%)',
                    boxShadow: '0 6px 25px rgba(25, 118, 210, 0.4)',
                    transform: 'translateY(-2px)',
                  },
                  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
                }}
              >
                Yeni Satış
              </Button>
            </Box>

            {/* Tab Sistemi */}
            {saleTabs.length > 0 && (
              <Paper 
                elevation={2} 
                sx={{ 
                  borderRadius: 2,
                  overflow: 'hidden'
                }}
              >
              <Tabs
                value={activeTab}
                onChange={handleTabChange}
                variant="fullWidth"
                sx={{
                  minHeight: 48,
                  '& .MuiTab-root': {
                    minHeight: 48,
                    textTransform: 'none',
                    fontSize: '0.85rem',
                    fontWeight: 500,
                    px: 1,
                    minWidth: 50,
                    maxWidth: 200,
                    color: 'text.secondary',
                    '&.Mui-selected': {
                      color: 'text.primary',
                      fontWeight: 600,
                    }
                  },
                  '& .MuiTabs-indicator': {
                    height: 0,
                  },
                  '& .MuiTabs-flexContainer': {
                    gap: 0.25,
                  }
                }}
              >
                {saleTabs.map((tab) => (
                  <Tab
                    key={tab.id}
                    value={tab.id}
                    draggable
                    onDragStart={(e) => handleDragStart(e, tab.id)}
                    onDragOver={handleDragOver}
                    onDrop={(e) => handleDrop(e, tab.id)}
                    sx={{
                      backgroundColor: activeTab === tab.id ? tab.color : `${tab.color}20`,
                      color: activeTab === tab.id ? '#fff' : tab.color,
                      fontWeight: activeTab === tab.id ? 600 : 500,
                      transition: 'all 0.2s ease',
                      borderRadius: '8px 8px 0 0',
                      mx: 0.5,
                      cursor: 'grab',
                      '&:active': {
                        cursor: 'grabbing',
                      },
                      '&:hover': {
                        backgroundColor: activeTab === tab.id ? tab.color : `${tab.color}30`,
                        color: activeTab === tab.id ? '#fff' : tab.color,
                      },
                      '&.Mui-selected': {
                        color: '#fff',
                      }
                    }}
                    label={
                      <Box sx={{ 
                        display: 'flex', 
                        alignItems: 'center', 
                        justifyContent: 'space-between',
                        gap: 0.5,
                        width: '100%',
                        overflow: 'hidden',
                      }}>
                        <Typography 
                          variant="body2" 
                          sx={{ 
                            fontWeight: 'inherit', 
                            color: 'inherit',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            whiteSpace: 'nowrap',
                            flexGrow: 1,
                            minWidth: 0,
                          }}
                        >
                          {tab.number}
                        </Typography>
                        <Box
                          component="span"
                          onClick={(e: React.MouseEvent<HTMLSpanElement>) => {
                            e.stopPropagation()
                            handleCloseTab(tab.id, e as React.MouseEvent)
                          }}
                          sx={{
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            width: 28,
                            height: 28,
                            minWidth: 28,
                            borderRadius: '50%',
                            color: 'inherit',
                            opacity: activeTab === tab.id ? 0.9 : 0.7,
                            flexShrink: 0,
                            cursor: 'pointer',
                            '&:hover': {
                              opacity: 1,
                              backgroundColor: activeTab === tab.id ? 'rgba(255,255,255,0.2)' : 'rgba(0,0,0,0.1)',
                              transform: 'scale(1.1)',
                            },
                            transition: 'all 0.2s ease',
                          }}
                        >
                          <CloseIcon sx={{ fontSize: 16 }} />
                        </Box>
                      </Box>
                    }
                  />
                ))}
              </Tabs>
            </Paper>
            )}
          </Box>

          {/* Tab İçeriği - Scrollable */}
          {activeTab && (
            <Box 
              sx={{ 
                flex: 1,
                overflow: 'hidden',
                minHeight: 0,
                maxWidth: '100%',
                display: 'flex',
                flexDirection: 'column'
              }}
            >
              <ModernSearchBox 
                key={activeTab}
                tabId={activeTab}
                onProductSelect={handleProductSelect}
              />
            </Box>
          )}

          {/* Tab Yoksa Boş Durum */}
          {saleTabs.length === 0 && (
            <Paper 
              elevation={1} 
              sx={{ 
                borderRadius: 2,
                p: 6,
                textAlign: 'center',
                backgroundColor: 'grey.50'
              }}
            >
              <Typography variant="h6" color="text.secondary" gutterBottom>
                Henüz satış başlatılmadı
              </Typography>
              <Typography variant="body2" color="text.secondary">
                &quot;Yeni Satış&quot; butonuna tıklayarak ilk satışınızı başlatın
              </Typography>
            </Paper>
          )}
        </Container>
      </Box>
    </Box>
  )
}
