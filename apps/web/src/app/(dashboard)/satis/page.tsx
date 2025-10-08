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
          width: '100%',
          maxWidth: '100vw',
          overflowX: 'hidden', // ⚠️ CRITICAL: Prevent horizontal page scroll
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
            width: '100%',
            maxWidth: '100%',
            overflowX: 'hidden', // ⚠️ CRITICAL: No horizontal scroll for entire page!
            overflowY: 'auto',   // ⚠️ Only vertical scroll allowed
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
                     pt: 1,
                     width: '100%', // ⚠️ CRITICAL: Fixed width container
                     maxWidth: '100%',
                     overflow: 'hidden' // ⚠️ Prevent any child overflow
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
                  overflow: 'hidden',
                  maxWidth: '100%' // ⚠️ CRITICAL: Prevent horizontal overflow
                }}
              >
              <Tabs
                value={activeTab}
                onChange={handleTabChange}
                variant="scrollable"
                scrollButtons={true}
                allowScrollButtonsMobile
                sx={{
                  minHeight: 56, // ⚠️ Daha yüksek tab bar
                  width: '100%',
                  maxWidth: '100%',
                  overflow: 'hidden',
                  '& .MuiTabs-scroller': {
                    overflow: 'hidden !important',
                  },
                  '& .MuiTab-root': {
                    minHeight: 56, // ⚠️ Daha yüksek tablar
                    textTransform: 'none',
                    fontSize: '0.95rem', // ⚠️ Biraz daha büyük font
                    fontWeight: 600, // ⚠️ Daha kalın font
                    px: 3, // ⚠️ Daha geniş padding
                    py: 1.5,
                    minWidth: 120, // ⚠️ Daha geniş min width
                    maxWidth: 200, // ⚠️ Daha geniş max width
                    flexShrink: 0,
                    color: 'text.secondary',
                    borderRadius: '8px 8px 0 0', // ⚠️ Yuvarlatılmış üst köşeler
                    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
                    '&.Mui-selected': {
                      color: 'text.primary',
                      fontWeight: 700,
                      transform: 'translateY(-2px)', // ⚠️ Aktif tab hafif yukarı
                      boxShadow: '0 -2px 8px rgba(0,0,0,0.1)', // ⚠️ Gölge efekti
                    },
                    '&:hover': {
                      transform: 'translateY(-1px)',
                      backgroundColor: 'action.hover',
                    }
                  },
                  '& .MuiTabs-indicator': {
                    height: 0,
                  },
                  '& .MuiTabs-flexContainer': {
                    gap: 0.5, // ⚠️ Biraz daha geniş gap
                  },
                  '& .MuiTabs-scrollButtons': {
                    width: 48, // ⚠️ Daha geniş scroll butonları
                    flexShrink: 0,
                    '&.Mui-disabled': { opacity: 0.3 }
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
                        gap: 1.5, // ⚠️ Daha geniş gap
                        width: '100%',
                        overflow: 'hidden',
                      }}>
                        <Typography 
                          variant="body1" // ⚠️ body2'den body1'e (daha büyük)
                          sx={{ 
                            fontWeight: 'inherit', 
                            color: 'inherit',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            whiteSpace: 'nowrap',
                            flexGrow: 1,
                            minWidth: 0,
                            fontSize: '1rem', // ⚠️ Daha büyük font
                            letterSpacing: '0.02em', // ⚠️ Hafif letter spacing
                          }}
                        >
                          Satış #{tab.number}
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
                            width: 32, // ⚠️ Daha büyük close button
                            height: 32,
                            minWidth: 32,
                            borderRadius: '50%',
                            color: 'inherit',
                            opacity: activeTab === tab.id ? 0.9 : 0.7,
                            flexShrink: 0,
                            cursor: 'pointer',
                            '&:hover': {
                              opacity: 1,
                              backgroundColor: activeTab === tab.id ? 'rgba(255,255,255,0.25)' : 'rgba(0,0,0,0.15)',
                              transform: 'scale(1.15)', // ⚠️ Biraz daha büyük scale
                            },
                            transition: 'all 0.2s ease',
                          }}
                        >
                          <CloseIcon sx={{ fontSize: 18 }} />
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
