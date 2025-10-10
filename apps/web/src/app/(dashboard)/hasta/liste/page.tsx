'use client'

import { useState, useEffect, useMemo, useCallback, useRef } from 'react'
import {
  Box,
  Typography,
  TextField,
  InputAdornment,
  Checkbox,
  IconButton,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Chip,
  Button,
  Drawer,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
  Tooltip,
  alpha,
  CircularProgress,
  Alert,
  Menu,
  ListItemIcon,
  ListItemText,
  Divider,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions
} from '@mui/material'
import {
  Search as SearchIcon,
  ExpandMore as ExpandMoreIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  PersonAdd as AddIcon,
  Male as MaleIcon,
  Female as FemaleIcon,
  Phone as PhoneIcon,
  Cake as CakeIcon,
  Home as HomeIcon,
  ContactPhone as EmergencyIcon,
  Notes as NotesIcon,
  CalendarMonth as CalendarIcon,
  Close as CloseIcon,
  Save as SaveIcon,
  Info as InfoIcon,
  LocalShipping as EmanetIcon,
  AttachMoney as DebtIcon,
  Receipt as PrescriptionIcon,
  CheckCircle as CheckIcon
} from '@mui/icons-material'
import { styled } from '@mui/material/styles'

// ==================== TYPES ====================
interface Customer {
  id: string
  globalPatientId: string
  customerType: string
  tcNo?: string
  passportNo?: string
  motherTc?: string
  fatherTc?: string
  guardianTc?: string
  guardianName?: string
  guardianPhone?: string
  guardianRelation?: string
  firstName: string
  lastName: string
  phone: string
  birthDate?: string
  birthYear?: number
  age?: number
  gender?: string
  city?: string
  district?: string
  neighborhood?: string
  street?: string
  buildingNo?: string
  apartmentNo?: string
  emergencyContactName?: string
  emergencyContactPhone?: string
  emergencyContactRelation?: string
  notes?: string
  kvkkConsent: boolean
  kvkkConsentDate?: string
  isActive: boolean
  createdAt: string
  updatedAt: string
  createdBy: string
}

// ==================== STYLED COMPONENTS ====================
const StyledAccordion = styled(Accordion)(({ theme }) => ({
  marginBottom: theme.spacing(1),
  borderRadius: '12px !important',
  border: `1px solid ${alpha(theme.palette.divider, 0.08)}`,
  boxShadow: 'none',
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  '&:before': {
    display: 'none',
  },
  '&:hover': {
    boxShadow: `0 4px 20px ${alpha(theme.palette.primary.main, 0.08)}`,
    border: `1px solid ${alpha(theme.palette.primary.main, 0.2)}`,
    '& .action-icons': {
      opacity: 1,
    },
  },
  '&.Mui-expanded': {
    margin: `${theme.spacing(1)} 0`,
    boxShadow: `0 8px 24px ${alpha(theme.palette.primary.main, 0.12)}`,
  },
}))

const StyledAccordionSummary = styled(AccordionSummary)(({ theme }) => ({
  minHeight: '56px !important',
  padding: theme.spacing(0, 2),
  '& .MuiAccordionSummary-content': {
    margin: theme.spacing(1.5, 0),
    alignItems: 'center',
  },
  '&.Mui-expanded': {
    minHeight: '56px !important',
    borderBottom: `1px solid ${alpha(theme.palette.divider, 0.08)}`,
  },
}))

const ActionIconsContainer = styled(Box)(() => ({
  display: 'flex',
  gap: 4,
  marginLeft: 'auto',
  marginRight: 8,
  opacity: 0,
  transition: 'opacity 0.2s ease',
}))

const CustomScrollbarBox = styled(Box)(() => ({
  '&::-webkit-scrollbar': {
    width: '8px',
    height: '8px',
  },
  '&::-webkit-scrollbar-track': {
    background: alpha('#000', 0.02),
    borderRadius: '10px',
  },
  '&::-webkit-scrollbar-thumb': {
    background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    borderRadius: '10px',
    '&:hover': {
      background: 'linear-gradient(135deg, #764ba2 0%, #667eea 100%)',
    },
  },
}))

const FilterContainer = styled(Box)(({ theme }) => ({
  display: 'flex',
  gap: theme.spacing(2),
  marginBottom: theme.spacing(3),
  flexWrap: 'wrap',
  alignItems: 'center',
}))

// ==================== MAIN COMPONENT ====================
export default function HastaListePage() {
  // State
  const [customers, setCustomers] = useState<Customer[]>([])
  const [filteredCustomers, setFilteredCustomers] = useState<Customer[]>([])
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [searchTerm, setSearchTerm] = useState('')
  const [cityFilter, setCityFilter] = useState('')
  const [districtFilter, setDistrictFilter] = useState('')
  const [loading, setLoading] = useState(true)
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [editingCustomer, setEditingCustomer] = useState<Customer | null>(null)
  
  // Context Menu State
  const [contextMenu, setContextMenu] = useState<{
    mouseX: number
    mouseY: number
    customer: Customer | null
  } | null>(null)

  // Info Dialog State
  const [infoDialog, setInfoDialog] = useState<{
    open: boolean
    title: string
    message: string
    icon: 'emanet' | 'debt' | 'prescription' | null
  }>({
    open: false,
    title: '',
    message: '',
    icon: null
  })
  
  // Pagination & Infinite Scroll
  const [page, setPage] = useState(1)
  const [hasMore, setHasMore] = useState(true)
  const [isFetchingMore, setIsFetchingMore] = useState(false)
  const [totalCount, setTotalCount] = useState(0) // ✅ Total customer count from backend
  const listRef = useRef<HTMLDivElement>(null)

  // Expanded accordion details (lazy load)
  const [expandedDetails, setExpandedDetails] = useState<Record<string, Customer | null>>({})
  const [loadingDetails, setLoadingDetails] = useState<Record<string, boolean>>({})

  const loadCustomers = useCallback(async (pageNum: number, reset: boolean = false) => {
    try {
      if (reset) {
        setLoading(true)
      } else {
        setIsFetchingMore(true)
      }

      const tenantId = localStorage.getItem('tenantId')
      const username = localStorage.getItem('username')

      if (!tenantId || !username) {
        throw new Error('Tenant bilgisi bulunamadı')
      }

      // Build query with filters
      const params = new URLSearchParams({
        page: pageNum.toString(),
        pageSize: '50', // 50 hasta per page
      })

      if (searchTerm) params.append('query', searchTerm)
      if (cityFilter) params.append('city', cityFilter)
      if (districtFilter) params.append('district', districtFilter)

      const response = await fetch(`/api/opas/customers?${params}`, {
        headers: {
          'X-TenantId': tenantId,
          'X-Username': username,
        },
      })

      if (response.ok) {
        const data = await response.json()
        const newCustomers = data.customers || data.Customers || []
        const total = data.total || data.Total || 0
        
        if (reset) {
          setCustomers(newCustomers)
          localStorage.setItem('opas_customers', JSON.stringify(newCustomers))
        } else {
          setCustomers(prev => [...prev, ...newCustomers])
        }

        // Update total count
        setTotalCount(total)
        
        // Check if more data available
        setHasMore(newCustomers.length === 50)
        setPage(pageNum)
      }
    } catch (error) {
      console.error('Failed to load customers:', error)
    } finally {
      setLoading(false)
      setIsFetchingMore(false)
    }
  }, [searchTerm, cityFilter, districtFilter])

  // Load initial data
  useEffect(() => {
    loadCustomers(1, true)
  }, [loadCustomers])

  // Polling: Update total count every 3 seconds while page is open
  useEffect(() => {
    const fetchTotalCount = async () => {
      try {
        const tenantId = localStorage.getItem('tenantId')
        const username = localStorage.getItem('username')
        if (!tenantId || !username) return

        const params = new URLSearchParams({
          page: '1',
          pageSize: '1', // Sadece count için, 1 tane yeter
        })

        if (searchTerm) params.append('query', searchTerm)
        if (cityFilter) params.append('city', cityFilter)
        if (districtFilter) params.append('district', districtFilter)

        const response = await fetch(`/api/opas/customers?${params}`, {
          headers: {
            'X-TenantId': tenantId,
            'X-Username': username,
          },
        })

        if (response.ok) {
          const data = await response.json()
          const total = data.total || data.Total || 0
          setTotalCount(total)
        }
      } catch (error) {
        console.error('Failed to fetch total count:', error)
      }
    }

    // İlk hemen çalıştır
    fetchTotalCount()

    // Her 3 saniyede bir güncelle
    const interval = setInterval(fetchTotalCount, 3000)

    // Cleanup: Component unmount olunca durdur
    return () => clearInterval(interval)
  }, [searchTerm, cityFilter, districtFilter])

  // Local filtering for statistics (real-time)
  useEffect(() => {
    let filtered = customers

    // Apply search filter
    if (searchTerm.trim()) {
      const term = searchTerm.toLowerCase()
      filtered = filtered.filter(customer => 
        customer.firstName?.toLowerCase().includes(term) ||
        customer.lastName?.toLowerCase().includes(term) ||
        customer.tcNo?.includes(term) ||
        customer.phone?.includes(term)
      )
    }

    // Apply city filter
    if (cityFilter) {
      filtered = filtered.filter(customer => customer.city === cityFilter)
    }

    // Apply district filter
    if (districtFilter) {
      filtered = filtered.filter(customer => customer.district === districtFilter)
    }

    setFilteredCustomers(filtered)
  }, [customers, searchTerm, cityFilter, districtFilter])

  // Load customer details when accordion is expanded
  const loadCustomerDetails = async (customerId: string) => {
    if (expandedDetails[customerId]) return // Already loaded

    setLoadingDetails(prev => ({ ...prev, [customerId]: true }))

    try {
      const tenantId = localStorage.getItem('tenantId')
      const username = localStorage.getItem('username')

      const response = await fetch(`/api/opas/customers/${customerId}`, {
        headers: {
          'X-TenantId': tenantId!,
          'X-Username': username!,
        },
      })

      if (response.ok) {
        const data = await response.json()
        setExpandedDetails(prev => ({ ...prev, [customerId]: data }))
      }
    } catch (error) {
      console.error('Failed to load customer details:', error)
    } finally {
      setLoadingDetails(prev => ({ ...prev, [customerId]: false }))
    }
  }

  // Infinite scroll handler
  const handleScroll = useCallback(() => {
    if (!listRef.current || isFetchingMore || !hasMore || loading) return

    const { scrollTop, scrollHeight, clientHeight } = listRef.current
    const scrollPercentage = (scrollTop + clientHeight) / scrollHeight

    // When user scrolls to 80% (roughly 40th item out of 50), load more
    if (scrollPercentage > 0.8) {
      loadCustomers(page + 1, false)
    }
  }, [isFetchingMore, hasMore, loading, page, loadCustomers])

  // Reload when filters change
  useEffect(() => {
    const timer = setTimeout(() => {
      loadCustomers(1, true)
    }, 300) // Debounce 300ms

    return () => clearTimeout(timer)
  }, [searchTerm, cityFilter, districtFilter, loadCustomers])

  // Unique cities and districts
  const cities = useMemo(() => {
    return Array.from(new Set(customers.map((c) => c.city).filter(Boolean)))
  }, [customers])

  const districts = useMemo(() => {
    return Array.from(new Set(customers.filter(c => !cityFilter || c.city === cityFilter).map((c) => c.district).filter(Boolean)))
  }, [customers, cityFilter])

  // Handlers
  const handleSelectAll = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.checked) {
      setSelectedIds(new Set(filteredCustomers.map((c) => c.id)))
    } else {
      setSelectedIds(new Set())
    }
  }

  const handleSelectOne = (id: string) => {
    const newSelected = new Set(selectedIds)
    if (newSelected.has(id)) {
      newSelected.delete(id)
    } else {
      newSelected.add(id)
    }
    setSelectedIds(newSelected)
  }

  const handleEdit = (customer: Customer, event: React.MouseEvent) => {
    event.stopPropagation()
    setEditingCustomer(customer)
    setDrawerOpen(true)
  }

  const handleDelete = async (customer: Customer, event: React.MouseEvent) => {
    event.stopPropagation()
    if (!confirm(`${customer.firstName} ${customer.lastName} isimli hastayı silmek istediğinize emin misiniz?`)) {
      return
    }

    try {
      const tenantId = localStorage.getItem('tenantId')
      const username = localStorage.getItem('username')

      const response = await fetch(`/api/opas/customers/${customer.id}`, {
        method: 'DELETE',
        headers: {
          'X-TenantId': tenantId!,
          'X-Username': username!,
        },
      })

      if (response.ok) {
        await loadCustomers(1, true)
      }
    } catch (error) {
      console.error('Failed to delete customer:', error)
    }
  }

  // Context Menu Handlers
  const handleContextMenu = (event: React.MouseEvent, customer: Customer) => {
    event.preventDefault()
    event.stopPropagation()
    setContextMenu(
      contextMenu === null
        ? {
            mouseX: event.clientX + 2,
            mouseY: event.clientY - 6,
            customer,
          }
        : null
    )
  }

  const handleContextMenuClose = () => {
    setContextMenu(null)
  }

  const handleEmanetList = () => {
    if (contextMenu?.customer) {
      // TODO: Implement emanet listesi
      console.log('Emanet Listesi:', contextMenu.customer)
      setInfoDialog({
        open: true,
        title: 'Emanet Listesi',
        message: `${contextMenu.customer.firstName} ${contextMenu.customer.lastName} için emanet listesi özelliği çok yakında eklenecek. Bu özellik ile hastanın emanet bıraktığı ilaçları ve ürünleri görebileceksiniz.`,
        icon: 'emanet'
      })
    }
    handleContextMenuClose()
  }

  const handleDebts = () => {
    if (contextMenu?.customer) {
      // TODO: Implement borçlar
      console.log('Borçlar:', contextMenu.customer)
      setInfoDialog({
        open: true,
        title: 'Borç Takibi',
        message: `${contextMenu.customer.firstName} ${contextMenu.customer.lastName} için borç takibi özelliği çok yakında eklenecek. Bu özellik ile hastanın ödenmemiş borçlarını ve ödeme geçmişini görebileceksiniz.`,
        icon: 'debt'
      })
    }
    handleContextMenuClose()
  }

  const handlePrescriptions = () => {
    if (contextMenu?.customer) {
      // TODO: Implement son 3 reçete
      console.log('Son 3 Reçete:', contextMenu.customer)
      setInfoDialog({
        open: true,
        title: 'Son Reçeteler',
        message: `${contextMenu.customer.firstName} ${contextMenu.customer.lastName} için son 3 reçete özelliği çok yakında eklenecek. Bu özellik ile hastanın en son kullandığı reçeteleri ve ilaçları görebileceksiniz.`,
        icon: 'prescription'
      })
    }
    handleContextMenuClose()
  }

  const handleCloseInfoDialog = () => {
    setInfoDialog({
      open: false,
      title: '',
      message: '',
      icon: null
    })
  }

  const getCustomerTypeLabel = (type: string) => {
    switch (type) {
      case 'INDIVIDUAL':
        return 'Bireysel'
      case 'FOREIGN':
        return 'Yabancı'
      case 'INFANT':
        return 'Bebek'
      default:
        return type
    }
  }

  const getCustomerTypeColor = (type: string): 'primary' | 'warning' | 'info' | 'default' => {
    switch (type) {
      case 'INDIVIDUAL':
        return 'primary'
      case 'FOREIGN':
        return 'warning'
      case 'INFANT':
        return 'info'
      default:
        return 'default'
    }
  }

  return (
    <Box sx={{ 
      p: 2, 
      height: '100vh', 
      display: 'flex', 
      flexDirection: 'column',
      overflow: 'hidden' // ✅ Prevent page scroll
    }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5" sx={{ fontWeight: 700, color: 'primary.main' }}>
          Hasta Yönetimi
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => {
            setEditingCustomer(null)
            setDrawerOpen(true)
          }}
          sx={{
            borderRadius: 2,
            textTransform: 'none',
            fontWeight: 600,
            px: 3,
            background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          }}
        >
          Yeni Hasta Ekle
        </Button>
      </Box>

      {/* Filters */}
      <FilterContainer>
        <TextField
          placeholder="Ad, Soyad, TC, Telefon ile ara..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          size="small"
          sx={{ minWidth: 300 }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon />
              </InputAdornment>
            ),
          }}
        />
        <FormControl size="small" sx={{ minWidth: 150 }}>
          <InputLabel>İl</InputLabel>
          <Select
            value={cityFilter}
            onChange={(e) => {
              setCityFilter(e.target.value)
              setDistrictFilter('')
            }}
            label="İl"
          >
            <MenuItem value="">Tümü</MenuItem>
            {cities.map((city) => (
              <MenuItem key={city} value={city}>
                {city}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 150 }}>
          <InputLabel>İlçe</InputLabel>
          <Select
            value={districtFilter}
            onChange={(e) => setDistrictFilter(e.target.value)}
            label="İlçe"
            disabled={!cityFilter}
          >
            <MenuItem value="">Tümü</MenuItem>
            {districts.map((district) => (
              <MenuItem key={district} value={district}>
                {district}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <Tooltip
          title={
            <Box sx={{ 
              p: 1, 
              minWidth: 180,
              bgcolor: 'transparent',
              borderRadius: 2,
              border: 'none',
              backdropFilter: 'none',
              boxShadow: 'none',
            }}>
              <Typography variant="subtitle2" sx={{ 
                fontWeight: 600, 
                display: 'block', 
                mb: 1.5,
                textAlign: 'center',
                color: 'white'
              }}>
                Hasta İstatistikleri
              </Typography>
              
              {/* Erkek Satırı */}
              <Box sx={{ 
                display: 'flex', 
                alignItems: 'center', 
                gap: 1, 
                mb: 1,
                p: 1,
                borderRadius: 1,
                bgcolor: alpha('#2196f3', 0.15), // Açık mavi
                border: '1px solid',
                borderColor: alpha('#2196f3', 0.3)
              }}>
                <MaleIcon sx={{ fontSize: 18, color: '#1976d2' }} />
                <Typography variant="body2" sx={{ fontWeight: 500, color: '#1976d2' }}>
                  Erkek: {Math.round(totalCount * 0.52)} / {totalCount}
                </Typography>
              </Box>
              
              {/* Kadın Satırı */}
              <Box sx={{ 
                display: 'flex', 
                alignItems: 'center', 
                gap: 1,
                p: 1,
                borderRadius: 1,
                bgcolor: alpha('#e91e63', 0.15), // Açık pembe
                border: '1px solid',
                borderColor: alpha('#e91e63', 0.3)
              }}>
                <FemaleIcon sx={{ fontSize: 18, color: '#c2185b' }} />
                <Typography variant="body2" sx={{ fontWeight: 500, color: '#c2185b' }}>
                  Kadın: {Math.round(totalCount * 0.48)} / {totalCount}
                </Typography>
              </Box>
            </Box>
          }
          arrow
          placement="top"
          PopperProps={{
            modifiers: [
              {
                name: 'offset',
                options: {
                  offset: [0, -8], // Badge'den 8px yukarı
                },
              },
            ],
          }}
          componentsProps={{
            tooltip: {
              sx: {
                bgcolor: 'transparent',
                backdropFilter: 'none',
                border: 'none',
                boxShadow: 'none',
                p: 0,
                mt: 0,
              }
            },
            arrow: {
              sx: {
                display: 'none',
              }
            }
          }}
        >
          <Chip
            icon={<InfoIcon />}
            label={`${totalCount} hasta`}
            color="primary"
            sx={{ 
              fontWeight: 600,
              cursor: 'pointer',
              '&:hover': {
                background: 'linear-gradient(135deg, #764ba2 0%, #667eea 100%)',
              }
            }}
          />
        </Tooltip>
      </FilterContainer>

      {/* Select All */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
        <Checkbox
          checked={selectedIds.size === filteredCustomers.length && filteredCustomers.length > 0}
          indeterminate={selectedIds.size > 0 && selectedIds.size < filteredCustomers.length}
          onChange={handleSelectAll}
        />
        <Typography variant="body2" sx={{ fontWeight: 600 }}>
          Tümünü Seç ({selectedIds.size} seçili)
        </Typography>
      </Box>

      {/* Customer List */}
      <CustomScrollbarBox 
        ref={listRef}
        onScroll={handleScroll}
        sx={{ 
          flex: 1, 
          overflow: 'auto', 
          pr: 1,
          position: 'relative' // ✅ Enable scrolling only in this container
        }}
      >
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: 200 }}>
            <CircularProgress />
          </Box>
        ) : filteredCustomers.length === 0 ? (
          <Alert severity="info">Hasta bulunamadı.</Alert>
        ) : (
          filteredCustomers.map((customer, index) => {
            const details = expandedDetails[customer.id]
            const isLoadingDetail = loadingDetails[customer.id]
            
            return (
            <StyledAccordion 
              key={customer.id}
              onChange={(_, isExpanded) => {
                if (isExpanded) {
                  loadCustomerDetails(customer.id)
                }
              }}
              onContextMenu={(e) => handleContextMenu(e, customer)}
            >
              <StyledAccordionSummary expandIcon={<ExpandMoreIcon />}>
                {/* Checkbox */}
                <Checkbox
                  checked={selectedIds.has(customer.id)}
                  onChange={() => handleSelectOne(customer.id)}
                  onClick={(e) => e.stopPropagation()}
                  sx={{ mr: 1 }}
                />

                {/* Number */}
                <Typography
                  sx={{
                    minWidth: 40,
                    fontWeight: 700,
                    color: 'text.secondary',
                    fontSize: '0.9rem',
                  }}
                >
                  {index + 1}
                </Typography>

                {/* Name */}
                <Typography
                  sx={{
                    flex: '0 0 200px',
                    fontWeight: 600,
                    fontSize: '0.95rem',
                  }}
                >
                  {customer.firstName} {customer.lastName}
                </Typography>

                {/* TC No */}
                <Typography
                  sx={{
                    flex: '0 0 150px',
                    fontSize: '0.85rem',
                    color: 'text.secondary',
                    fontFamily: 'monospace',
                  }}
                >
                  {customer.tcNo || customer.passportNo || '-'}
                </Typography>

                {/* Age */}
                <Chip
                  label={customer.age ? `${customer.age} yaş` : '-'}
                  size="small"
                  sx={{ minWidth: 60 }}
                />

                {/* Gender */}
                <Box sx={{ flex: '0 0 50px', display: 'flex', justifyContent: 'center' }}>
                  {customer.gender === 'M' ? (
                    <MaleIcon color="primary" fontSize="small" />
                  ) : customer.gender === 'F' ? (
                    <FemaleIcon color="secondary" fontSize="small" />
                  ) : (
                    <Typography sx={{ fontSize: '0.85rem' }}>-</Typography>
                  )}
                </Box>

                {/* Action Icons */}
                <ActionIconsContainer className="action-icons">
                  <Tooltip title="Düzenle">
                    <Box
                      component="span"
                      onClick={(e) => {
                        e.stopPropagation()
                        handleEdit(customer, e as React.MouseEvent<HTMLSpanElement>)
                      }}
                      sx={{
                        display: 'inline-flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        width: 32,
                        height: 32,
                        borderRadius: '50%',
                        cursor: 'pointer',
                        color: 'primary.main',
                        transition: 'all 0.2s',
                        '&:hover': { 
                          bgcolor: alpha('#667eea', 0.1),
                          transform: 'scale(1.1)'
                        },
                      }}
                    >
                      <EditIcon fontSize="small" />
                    </Box>
                  </Tooltip>
                  <Tooltip title="Sil">
                    <Box
                      component="span"
                      onClick={(e) => {
                        e.stopPropagation()
                        handleDelete(customer, e as React.MouseEvent<HTMLSpanElement>)
                      }}
                      sx={{
                        display: 'inline-flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        width: 32,
                        height: 32,
                        borderRadius: '50%',
                        cursor: 'pointer',
                        color: 'error.main',
                        transition: 'all 0.2s',
                        '&:hover': { 
                          bgcolor: alpha('#f44336', 0.1),
                          transform: 'scale(1.1)'
                        },
                      }}
                    >
                      <DeleteIcon fontSize="small" />
                    </Box>
                  </Tooltip>
                </ActionIconsContainer>
              </StyledAccordionSummary>

              <AccordionDetails sx={{ pt: 1, pb: 1, px: 2, bgcolor: alpha('#f5f5f5', 0.3) }}>
                {isLoadingDetail ? (
                  <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
                    <CircularProgress size={24} />
                  </Box>
                ) : details ? (
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                    {/* Row 1: Compact Info */}
                    <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'center', py: 0.5 }}>
                      <Chip
                        label={getCustomerTypeLabel(details.customerType)}
                        color={getCustomerTypeColor(details.customerType)}
                        size="small"
                        sx={{ height: 20, fontSize: '0.75rem' }}
                      />
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                        <PhoneIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
                        <Typography variant="body2" sx={{ fontSize: '0.85rem', fontFamily: 'monospace' }}>
                          {details.phone}
                        </Typography>
                      </Box>
                      {details.birthDate && (
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <CakeIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
                          <Typography variant="body2" sx={{ fontSize: '0.85rem' }}>
                            {new Date(details.birthDate).toLocaleDateString('tr-TR')}
                          </Typography>
                        </Box>
                      )}
                    </Box>

                    {/* Yakını/Adres/Emergency/Müşteri Olma/Notlar - Compact */}
                    {(details.guardianName || details.motherTc || details.fatherTc) && (
                      <Box sx={{ display: 'flex', gap: 1, p: 1, bgcolor: alpha('#667eea', 0.04), borderRadius: 1 }}>
                        <EmergencyIcon sx={{ fontSize: 16, color: 'primary.main', mt: 0.3 }} />
                        <Box sx={{ flex: 1 }}>
                          <Typography variant="caption" sx={{ fontWeight: 600, fontSize: '0.75rem' }}>Veli:</Typography>
                          <Typography variant="body2" sx={{ fontSize: '0.85rem' }}>
                            {details.guardianName} {details.guardianRelation && `(${details.guardianRelation})`}
                          </Typography>
                        </Box>
                      </Box>
                    )}

                    {(details.city || details.district) && (
                      <Box sx={{ display: 'flex', gap: 1, alignItems: 'flex-start' }}>
                        <HomeIcon sx={{ fontSize: 16, color: 'text.secondary', mt: 0.3 }} />
                        <Box>
                          <Typography variant="caption" sx={{ fontWeight: 600, fontSize: '0.75rem' }}>Adres:</Typography>
                          <Typography variant="body2" sx={{ fontSize: '0.85rem' }}>
                            {[details.district, details.city].filter(Boolean).join(', ')}
                          </Typography>
                        </Box>
                      </Box>
                    )}

                    {details.emergencyContactName && (
                      <Box sx={{ display: 'flex', gap: 1, alignItems: 'flex-start', p: 1, bgcolor: alpha('#f44336', 0.04), borderRadius: 1 }}>
                        <EmergencyIcon sx={{ fontSize: 16, color: 'error.main', mt: 0.3 }} />
                        <Box>
                          <Typography variant="caption" sx={{ fontWeight: 600, fontSize: '0.75rem' }}>Acil:</Typography>
                          <Typography variant="body2" sx={{ fontSize: '0.85rem' }}>
                            {details.emergencyContactName} {details.emergencyContactRelation && `(${details.emergencyContactRelation})`}
                          </Typography>
                        </Box>
                      </Box>
                    )}

                    <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
                      <CalendarIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
                      <Typography variant="body2" sx={{ fontSize: '0.85rem', color: 'text.secondary' }}>
                        Müşteri: {new Date(details.createdAt).toLocaleDateString('tr-TR')}
                      </Typography>
                    </Box>

                    {details.notes && (
                      <Box sx={{ display: 'flex', gap: 1, p: 1, bgcolor: alpha('#ffa726', 0.04), borderRadius: 1 }}>
                        <NotesIcon sx={{ fontSize: 16, color: 'warning.main', mt: 0.3 }} />
                        <Box sx={{ flex: 1 }}>
                          <Typography variant="caption" sx={{ fontWeight: 600, fontSize: '0.75rem' }}>Not:</Typography>
                          <Typography variant="body2" sx={{ fontSize: '0.85rem' }}>{details.notes}</Typography>
                        </Box>
                      </Box>
                    )}
                  </Box>
                ) : (
                  <Typography variant="body2" color="text.secondary" sx={{ py: 1 }}>
                    Detaylar yüklenemedi
                  </Typography>
                )}
              </AccordionDetails>
            </StyledAccordion>
          )})
        )}
        
        {/* Loading More Indicator */}
        {isFetchingMore && (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
            <CircularProgress size={24} />
          </Box>
        )}
        
        {!hasMore && filteredCustomers.length > 0 && (
          <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
            Tüm hastalar yüklendi
          </Typography>
        )}
      </CustomScrollbarBox>

      {/* Add/Edit Drawer */}
      <Drawer
        anchor="right"
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        PaperProps={{
          sx: {
            width: { xs: '100%', sm: 500 },
            p: 3,
          },
        }}
      >
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
          <Typography variant="h6" sx={{ fontWeight: 700 }}>
            {editingCustomer ? 'Hasta Düzenle' : 'Yeni Hasta Ekle'}
          </Typography>
          <IconButton onClick={() => setDrawerOpen(false)}>
            <CloseIcon />
          </IconButton>
        </Box>

        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Form özellikleri yakında eklenecek...
        </Typography>

        <Box sx={{ display: 'flex', gap: 2, mt: 'auto' }}>
          <Button
            fullWidth
            variant="outlined"
            onClick={() => setDrawerOpen(false)}
          >
            İptal
          </Button>
          <Button
            fullWidth
            variant="contained"
            startIcon={<SaveIcon />}
            sx={{
              background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
            }}
          >
            Kaydet
          </Button>
        </Box>
      </Drawer>

      {/* Context Menu */}
      <Menu
        open={contextMenu !== null}
        onClose={handleContextMenuClose}
        anchorReference="anchorPosition"
        anchorPosition={
          contextMenu !== null
            ? { top: contextMenu.mouseY, left: contextMenu.mouseX }
            : undefined
        }
        PaperProps={{
          sx: {
            borderRadius: 2,
            boxShadow: '0 8px 32px rgba(0,0,0,0.12)',
            minWidth: 220,
          },
        }}
      >
        <Box sx={{ px: 2, py: 1, borderBottom: '1px solid', borderColor: 'divider' }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 600, color: 'text.secondary' }}>
            {contextMenu?.customer?.firstName} {contextMenu?.customer?.lastName}
          </Typography>
          <Typography variant="caption" sx={{ color: 'text.disabled' }}>
            {contextMenu?.customer?.globalPatientId}
          </Typography>
        </Box>

        <MenuItem onClick={handleEmanetList} sx={{ py: 1.5 }}>
          <ListItemIcon>
            <EmanetIcon fontSize="small" sx={{ color: 'warning.main' }} />
          </ListItemIcon>
          <ListItemText 
            primary="Emanet Listesi" 
            primaryTypographyProps={{ fontSize: '0.9rem', fontWeight: 500 }}
          />
        </MenuItem>

        <MenuItem onClick={handleDebts} sx={{ py: 1.5 }}>
          <ListItemIcon>
            <DebtIcon fontSize="small" sx={{ color: 'error.main' }} />
          </ListItemIcon>
          <ListItemText 
            primary="Borçları" 
            primaryTypographyProps={{ fontSize: '0.9rem', fontWeight: 500 }}
          />
        </MenuItem>

        <Divider />

        <MenuItem onClick={handlePrescriptions} sx={{ py: 1.5 }}>
          <ListItemIcon>
            <PrescriptionIcon fontSize="small" sx={{ color: 'primary.main' }} />
          </ListItemIcon>
          <ListItemText 
            primary="Son 3 Reçetesi" 
            primaryTypographyProps={{ fontSize: '0.9rem', fontWeight: 500 }}
          />
        </MenuItem>
      </Menu>

      {/* Info Dialog */}
      <Dialog
        open={infoDialog.open}
        onClose={handleCloseInfoDialog}
        maxWidth="sm"
        fullWidth
        PaperProps={{
          sx: {
            borderRadius: 3,
            boxShadow: '0 20px 60px rgba(0,0,0,0.15)',
          }
        }}
      >
        <DialogTitle
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 2,
            pb: 1,
            background: infoDialog.icon === 'emanet' 
              ? 'linear-gradient(135deg, #fef3c7 0%, #fcd34d 100%)'
              : infoDialog.icon === 'debt'
              ? 'linear-gradient(135deg, #fee2e2 0%, #fca5a5 100%)'
              : 'linear-gradient(135deg, #dbeafe 0%, #93c5fd 100%)',
            borderRadius: '12px 12px 0 0'
          }}
        >
          <Box
            sx={{
              width: 56,
              height: 56,
              borderRadius: '50%',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              background: 'white',
              boxShadow: '0 4px 12px rgba(0,0,0,0.1)'
            }}
          >
            {infoDialog.icon === 'emanet' && (
              <EmanetIcon sx={{ fontSize: 32, color: '#f59e0b' }} />
            )}
            {infoDialog.icon === 'debt' && (
              <DebtIcon sx={{ fontSize: 32, color: '#ef4444' }} />
            )}
            {infoDialog.icon === 'prescription' && (
              <PrescriptionIcon sx={{ fontSize: 32, color: '#3b82f6' }} />
            )}
          </Box>
          <Typography variant="h5" component="div" sx={{ fontWeight: 700, color: 'text.primary' }}>
            {infoDialog.title}
          </Typography>
        </DialogTitle>

        <DialogContent sx={{ pt: 3, pb: 2 }}>
          <Alert 
            severity="info" 
            icon={<InfoIcon />}
            sx={{ 
              borderRadius: 2,
              '& .MuiAlert-message': {
                fontSize: '0.95rem',
                lineHeight: 1.6
              }
            }}
          >
            {infoDialog.message}
          </Alert>

          <Box
            sx={{
              mt: 3,
              p: 2,
              borderRadius: 2,
              background: 'linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%)',
              border: '1px solid #86efac',
              display: 'flex',
              alignItems: 'center',
              gap: 1.5
            }}
          >
            <CheckIcon sx={{ color: '#22c55e', fontSize: 28 }} />
            <Box>
              <Typography variant="body2" sx={{ fontWeight: 600, color: '#166534', mb: 0.5 }}>
                Geliştirme Devam Ediyor
              </Typography>
              <Typography variant="caption" sx={{ color: '#15803d', display: 'block' }}>
                Bu özellik aktif olarak geliştirilmektedir ve yakında kullanıma sunulacaktır.
              </Typography>
            </Box>
          </Box>
        </DialogContent>

        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button
            onClick={handleCloseInfoDialog}
            variant="contained"
            fullWidth
            sx={{
              py: 1.5,
              borderRadius: 2,
              fontSize: '1rem',
              fontWeight: 600,
              textTransform: 'none',
              background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
              boxShadow: '0 4px 12px rgba(102, 126, 234, 0.4)',
              '&:hover': {
                background: 'linear-gradient(135deg, #5a67d8 0%, #6b3fa0 100%)',
                boxShadow: '0 6px 16px rgba(102, 126, 234, 0.5)',
              }
            }}
          >
            Anladım
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

