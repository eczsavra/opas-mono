'use client'

import { useState, useEffect, useRef } from 'react'
import { 
  Box, 
  Container, 
  Typography, 
  Button, 
  Tabs, 
  Tab, 
  Paper,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  InputAdornment,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  CircularProgress,
  Chip
} from '@mui/material'
import { 
  Add as AddIcon, 
  Close as CloseIcon,
  Search as SearchIcon,
  Person as PersonIcon,
  Warning as WarningIcon,
  SwapHoriz as SwapIcon
} from '@mui/icons-material'
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

interface Customer {
  id: string
  firstName: string
  lastName: string
  phone: string
  globalPatientId: string
}

export default function SatisPage() {
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
    updateTab,
  } = useSalesContext()

  // Customer Search Modal State
  const [customerSearchOpen, setCustomerSearchOpen] = useState(false)
  const [customerSearchQuery, setCustomerSearchQuery] = useState('')
  const [customers, setCustomers] = useState<Customer[]>([])
  const [loadingCustomers, setLoadingCustomers] = useState(false)
  const customerSearchInputRef = useRef<HTMLInputElement>(null)
  
  // Customer Change Confirmation State
  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean
    oldCustomer: string
    newCustomer: Customer | null
  }>({
    open: false,
    oldCustomer: '',
    newCustomer: null
  })

  // F3 keyboard shortcut for customer search
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'F3') {
        e.preventDefault()
        setCustomerSearchOpen(true)
        // Focus input after modal opens
        setTimeout(() => {
          customerSearchInputRef.current?.focus()
        }, 200)
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [])

  // Search customers when query changes
  useEffect(() => {
    const searchCustomers = async () => {
      if (!customerSearchOpen) {
        return
      }

      if (customerSearchQuery.trim().length < 2) {
        setCustomers([])
        setLoadingCustomers(false)
        return
      }

      setLoadingCustomers(true)
      try {
        const tenantId = localStorage.getItem('tenantId')
        const username = localStorage.getItem('username')

        console.log('🔍 Searching customers with query:', customerSearchQuery) // DEBUG

        const response = await fetch(`/api/opas/customers?query=${encodeURIComponent(customerSearchQuery)}&page=1&pageSize=20`, {
          headers: {
            'X-TenantId': tenantId!,
            'X-Username': username!,
          },
        })

        console.log('📡 Response status:', response.status) // DEBUG

        if (response.ok) {
          const data = await response.json()
          console.log('✅ Customer search response:', data) // DEBUG
          const customerList = data.Customers || data.customers || []
          console.log('👥 Found customers:', customerList.length) // DEBUG
          setCustomers(customerList)
        } else {
          const errorText = await response.text()
          console.error('❌ Customer search failed:', response.status, errorText)
          setCustomers([])
        }
      } catch (error) {
        console.error('💥 Failed to search customers:', error)
        setCustomers([])
      } finally {
        setLoadingCustomers(false)
      }
    }

    const debounce = setTimeout(searchCustomers, 300)
    return () => clearTimeout(debounce)
  }, [customerSearchQuery, customerSearchOpen])

  const handleCustomerSelect = (customer: Customer) => {
    const currentTab = saleTabs.find(tab => tab.id === activeTab)
    if (!currentTab) return

    // Eğer tab'da zaten hasta varsa onay iste
    if (currentTab.customerId && currentTab.customerName) {
      setConfirmDialog({
        open: true,
        oldCustomer: currentTab.customerName,
        newCustomer: customer
      })
      setCustomerSearchOpen(false)
      setCustomerSearchQuery('')
      setCustomers([])
      return
    }

    // Hasta ata (ilk defa)
    updateTab(currentTab.id, {
      title: `${customer.firstName} ${customer.lastName}`,
      customerId: customer.id,
      customerName: `${customer.firstName} ${customer.lastName}`
    })
    
    setCustomerSearchOpen(false)
    setCustomerSearchQuery('')
    setCustomers([])
  }

  const handleConfirmCustomerChange = () => {
    const currentTab = saleTabs.find(tab => tab.id === activeTab)
    if (!currentTab || !confirmDialog.newCustomer) return

    const customer = confirmDialog.newCustomer
    updateTab(currentTab.id, {
      title: `${customer.firstName} ${customer.lastName}`,
      customerId: customer.id,
      customerName: `${customer.firstName} ${customer.lastName}`
    })

    setConfirmDialog({
      open: false,
      oldCustomer: '',
      newCustomer: null
    })
  }

  const handleCancelCustomerChange = () => {
    setConfirmDialog({
      open: false,
      oldCustomer: '',
      newCustomer: null
    })
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
    <Container 
      maxWidth="xl" 
      sx={{ 
        height: 'calc(100vh - 64px)',
        width: '100%',
        maxWidth: '100%',
        overflowX: 'hidden',
        overflowY: 'auto',
        display: 'flex',
        flexDirection: 'column',
        py: 1,
        px: 2
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
                    px: 2, // ⚠️ Padding azaltıldı
                    py: 1.5,
                    minWidth: 100, // ⚠️ Min width azaltıldı
                    maxWidth: 250, // ⚠️ Max width artırıldı - uzun isimler için
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
                          variant="body1"
                          sx={{ 
                            fontWeight: 'inherit', 
                            color: 'inherit',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            whiteSpace: 'nowrap',
                            flexGrow: 1,
                            minWidth: 0,
                            // Dinamik font size - uzun isimler için agresif küçülme
                            fontSize: tab.title.length > 25 ? '0.7rem' 
                              : tab.title.length > 20 ? '0.75rem' 
                              : tab.title.length > 15 ? '0.85rem' 
                              : tab.title.length > 12 ? '0.9rem'
                              : '1rem',
                            letterSpacing: tab.title.length > 20 ? '0' : '0.02em',
                            lineHeight: 1.2,
                          }}
                        >
                          {tab.title}
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

      {/* Customer Search Modal (F3) */}
      <Dialog
        open={customerSearchOpen}
        onClose={() => {
          setCustomerSearchOpen(false)
          setCustomerSearchQuery('')
          setCustomers([])
        }}
        maxWidth="md"
        fullWidth
        PaperProps={{
          sx: {
            borderRadius: 4,
            boxShadow: '0 24px 80px rgba(0,0,0,0.2)',
            overflow: 'hidden',
          }
        }}
      >
        <Box
          sx={{
            background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
            color: 'white',
            p: 3,
          }}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
            <Box
              sx={{
                width: 48,
                height: 48,
                borderRadius: '50%',
                background: 'rgba(255,255,255,0.2)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                backdropFilter: 'blur(10px)',
              }}
            >
              <PersonIcon sx={{ fontSize: 28 }} />
            </Box>
            <Box>
              <Typography variant="h5" sx={{ fontWeight: 700, mb: 0.5 }}>
                Hasta Ara
              </Typography>
              <Typography variant="body2" sx={{ opacity: 0.9 }}>
                F3 tuşu ile hızlıca erişebilirsiniz
              </Typography>
            </Box>
          </Box>

          <TextField
            fullWidth
            inputRef={customerSearchInputRef}
            placeholder="Ad, Soyad veya TC Kimlik No ile arama yapın..."
            value={customerSearchQuery}
            onChange={(e) => setCustomerSearchQuery(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon sx={{ color: 'rgba(255,255,255,0.7)' }} />
                </InputAdornment>
              ),
            }}
            sx={{
              '& .MuiOutlinedInput-root': {
                background: 'rgba(255,255,255,0.15)',
                backdropFilter: 'blur(10px)',
                borderRadius: 3,
                color: 'white',
                fontSize: '1.1rem',
                '& fieldset': {
                  borderColor: 'rgba(255,255,255,0.3)',
                },
                '&:hover fieldset': {
                  borderColor: 'rgba(255,255,255,0.5)',
                },
                '&.Mui-focused fieldset': {
                  borderColor: 'white',
                  borderWidth: 2,
                },
              },
              '& .MuiOutlinedInput-input': {
                color: 'white',
                '&::placeholder': {
                  color: 'rgba(255,255,255,0.7)',
                  opacity: 1,
                },
              },
            }}
          />

        </Box>

        <DialogContent sx={{ p: 0, background: '#f8fafc' }}>
          {loadingCustomers ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', py: 8 }}>
              <CircularProgress size={48} thickness={4} />
            </Box>
          ) : customers.length > 0 ? (
            <List sx={{ p: 2 }}>
              {customers.map((customer) => (
                <ListItem key={customer.id} disablePadding sx={{ mb: 1.5 }}>
                  <ListItemButton
                    onClick={() => handleCustomerSelect(customer)}
                    sx={{
                      borderRadius: 3,
                      p: 2.5,
                      background: 'white',
                      border: '2px solid transparent',
                      boxShadow: '0 2px 8px rgba(0,0,0,0.05)',
                      transition: 'all 0.2s cubic-bezier(0.4, 0, 0.2, 1)',
                      '&:hover': {
                        borderColor: '#667eea',
                        boxShadow: '0 8px 24px rgba(102, 126, 234, 0.15)',
                        transform: 'translateY(-2px)',
                      }
                    }}
                  >
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, width: '100%' }}>
                      <Box
                        sx={{
                          width: 48,
                          height: 48,
                          borderRadius: '50%',
                          background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                          color: 'white',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          fontSize: '1.2rem',
                          fontWeight: 700,
                          flexShrink: 0,
                        }}
                      >
                        {customer.firstName.charAt(0)}{customer.lastName.charAt(0)}
                      </Box>
                      <Box sx={{ flexGrow: 1, minWidth: 0 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 0.5 }}>
                          <Typography variant="h6" sx={{ fontWeight: 700, fontSize: '1.1rem' }}>
                            {customer.firstName} {customer.lastName}
                          </Typography>
                          <Chip
                            label={customer.globalPatientId}
                            size="small"
                            sx={{
                              background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                              color: 'white',
                              fontWeight: 600,
                              fontSize: '0.7rem',
                              height: 24,
                            }}
                          />
                        </Box>
                        <Typography variant="body2" sx={{ color: 'text.secondary', display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          📞 {customer.phone}
                        </Typography>
                      </Box>
                      <Typography
                        variant="body2"
                        sx={{
                          color: '#667eea',
                          fontWeight: 600,
                          fontSize: '0.9rem',
                        }}
                      >
                        Seç →
                      </Typography>
                    </Box>
                  </ListItemButton>
                </ListItem>
              ))}
            </List>
          ) : customerSearchQuery.trim().length >= 2 ? (
            <Box sx={{ textAlign: 'center', py: 8 }}>
              <Typography variant="h6" sx={{ color: 'text.secondary', mb: 1 }}>
                😔 Hasta Bulunamadı
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Arama kriterlerinize uygun hasta kaydı bulunamadı
              </Typography>
            </Box>
          ) : (
            <Box sx={{ textAlign: 'center', py: 8 }}>
              <Typography variant="h6" sx={{ color: 'text.secondary', mb: 1 }}>
                🔍 Hasta Arayın
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Aramaya başlamak için en az 2 karakter girin
              </Typography>
            </Box>
          )}
        </DialogContent>
      </Dialog>

      {/* Customer Change Confirmation Dialog */}
      <Dialog
        open={confirmDialog.open}
        onClose={handleCancelCustomerChange}
        maxWidth="sm"
        fullWidth
        PaperProps={{
          sx: {
            borderRadius: 4,
            boxShadow: '0 24px 80px rgba(0,0,0,0.25)',
            overflow: 'hidden',
          }
        }}
      >
        <Box
          sx={{
            background: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)',
            color: 'white',
            p: 4,
            textAlign: 'center',
          }}
        >
          <Box
            sx={{
              width: 80,
              height: 80,
              borderRadius: '50%',
              background: 'rgba(255,255,255,0.2)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              margin: '0 auto 16px',
              backdropFilter: 'blur(10px)',
              animation: 'pulse 2s infinite',
              '@keyframes pulse': {
                '0%, 100%': { transform: 'scale(1)', opacity: 1 },
                '50%': { transform: 'scale(1.05)', opacity: 0.9 },
              },
            }}
          >
            <WarningIcon sx={{ fontSize: 48 }} />
          </Box>
          <Typography variant="h4" sx={{ fontWeight: 700, mb: 1 }}>
            Hasta Değişikliği
          </Typography>
          <Typography variant="body1" sx={{ opacity: 0.95 }}>
            Bu satışta zaten bir hasta tanımlı
          </Typography>
        </Box>

        <DialogContent sx={{ p: 4 }}>
          <Box
            sx={{
              p: 3,
              borderRadius: 3,
              background: 'linear-gradient(135deg, #fef3c7 0%, #fde68a 100%)',
              border: '2px solid #fbbf24',
              mb: 3,
            }}
          >
            <Typography variant="body1" sx={{ fontWeight: 600, color: '#92400e', mb: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
              <WarningIcon sx={{ fontSize: 20 }} />
              Mevcut Hasta
            </Typography>
            <Typography variant="h6" sx={{ color: '#78350f', fontWeight: 700 }}>
              {confirmDialog.oldCustomer}
            </Typography>
          </Box>

          <Box sx={{ display: 'flex', justifyContent: 'center', mb: 3 }}>
            <SwapIcon sx={{ fontSize: 40, color: '#f59e0b' }} />
          </Box>

          <Box
            sx={{
              p: 3,
              borderRadius: 3,
              background: 'linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%)',
              border: '2px solid #3b82f6',
            }}
          >
            <Typography variant="body1" sx={{ fontWeight: 600, color: '#1e40af', mb: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
              <PersonIcon sx={{ fontSize: 20 }} />
              Yeni Hasta
            </Typography>
            <Typography variant="h6" sx={{ color: '#1e3a8a', fontWeight: 700 }}>
              {confirmDialog.newCustomer?.firstName} {confirmDialog.newCustomer?.lastName}
            </Typography>
          </Box>

          <Box
            sx={{
              mt: 3,
              p: 2,
              borderRadius: 2,
              background: 'linear-gradient(135deg, #fee2e2 0%, #fecaca 100%)',
              border: '1px solid #f87171',
            }}
          >
            <Typography variant="body2" sx={{ color: '#991b1b', fontWeight: 600, textAlign: 'center' }}>
              ⚠️ Bu işlem geri alınamaz! Satış kaydı yeni hasta ile ilişkilendirilecektir.
            </Typography>
          </Box>
        </DialogContent>

        <DialogActions sx={{ px: 4, pb: 4, gap: 2 }}>
          <Button
            onClick={handleCancelCustomerChange}
            variant="outlined"
            fullWidth
            sx={{
              py: 1.5,
              borderRadius: 2,
              fontSize: '1rem',
              fontWeight: 600,
              textTransform: 'none',
              borderColor: '#9ca3af',
              color: '#6b7280',
              '&:hover': {
                borderColor: '#6b7280',
                background: 'rgba(0,0,0,0.04)',
              }
            }}
          >
            İptal
          </Button>
          <Button
            onClick={handleConfirmCustomerChange}
            variant="contained"
            fullWidth
            sx={{
              py: 1.5,
              borderRadius: 2,
              fontSize: '1rem',
              fontWeight: 600,
              textTransform: 'none',
              background: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)',
              boxShadow: '0 4px 12px rgba(245, 158, 11, 0.4)',
              '&:hover': {
                background: 'linear-gradient(135deg, #d97706 0%, #b45309 100%)',
                boxShadow: '0 6px 16px rgba(245, 158, 11, 0.5)',
              }
            }}
          >
            Evet, Değiştir
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  )
}
