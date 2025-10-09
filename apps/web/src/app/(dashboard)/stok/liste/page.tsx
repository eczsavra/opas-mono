'use client'

import { useState, useEffect } from 'react'
import { useRouter } from 'next/navigation'
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  InputAdornment,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  IconButton,
  Tooltip,
  Alert,
  CircularProgress,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
  Button,
  Stack,
  Drawer,
  Divider,
  Autocomplete,
  Paper,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material'
import {
  Search as SearchIcon,
  Inventory as InventoryIcon,
  Warning as WarningIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Dashboard as DashboardIcon,
  Refresh as RefreshIcon,
  TrendingDown as TrendingDownIcon,
  TrendingUp as TrendingUpIcon,
  Add as AddIcon,
  Close as CloseIcon,
  Save as SaveIcon,
  Clear as ClearIcon,
  LocalShipping as LocalShippingIcon,
  Edit as EditIcon,
  CloudUpload as CloudUploadIcon,
} from '@mui/icons-material'
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider'
import { DatePicker } from '@mui/x-date-pickers/DatePicker'
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs'
import dayjs, { Dayjs } from 'dayjs'
import 'dayjs/locale/tr'
import StockImportModal from './StockImportModal'

interface StockItem {
  productId: string
  gtin: string
  drugName: string
  category: string
  totalQuantity: number
  totalTracked: number
  totalUntracked: number
  totalValue: number
  averageCost: number | null
  nearestExpiryDate: string | null
  lastMovementDate: string | null
  hasExpiringSoon: boolean
  hasExpired: boolean
  hasLowStock: boolean
  needsAttention: boolean
}

interface StockAlert {
  productId: string
  gtin: string
  drugName: string
  alertType: string
  alertLevel: string
  message: string
  quantity: number
  threshold: number | null
  expiryDate: string | null
}

type SortField = 'drugName' | 'totalQuantity' | 'nearestExpiryDate'
type SortOrder = 'asc' | 'desc'

interface Product {
  id: string
  gtin: string
  drugName: string
  manufacturerName: string
  price: number
  category: string
  hasDatamatrix: boolean
  requiresExpiryTracking: boolean
  isControlled: boolean
}

interface StockEntry {
  product: Product | null
  movementType: string
  quantity: number
  totalCost: number // ✅ Toplam tutar (birim maliyet değil)
  bonusQuantity: number
  serialNumber: string
  lotNumber: string
  expiryDate: Dayjs | null
  batchNumber: string
  notes: string
}

const MOVEMENT_TYPES = [
  { value: 'PURCHASE_DEPOT', label: 'Depodan Alış', icon: '🏭' },
  { value: 'PURCHASE_MARKETPLACE', label: 'Pazaryeri Alışı', icon: '🛒' },
  { value: 'PURCHASE_OTHER', label: 'Diğer Alış', icon: '📦' },
]

export default function StokListePage() {
  const router = useRouter()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [stockItems, setStockItems] = useState<StockItem[]>([])
  const [alerts, setAlerts] = useState<StockAlert[]>([])
  const [searchTerm, setSearchTerm] = useState('')
  const [categoryFilter, setCategoryFilter] = useState<string>('ALL')
  const [alertFilter, setAlertFilter] = useState<string>('ALL')
  const [sortField, setSortField] = useState<SortField>('drugName')
  const [sortOrder, setSortOrder] = useState<SortOrder>('asc')
  
  // Drawer state
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [saving, setSaving] = useState(false)
  const [importModalOpen, setImportModalOpen] = useState(false)
  
  // Product search for drawer
  const [products, setProducts] = useState<Product[]>([])
  const [searchLoading, setSearchLoading] = useState(false)
  
  // Stock correction modal state
  const [correctionModalOpen, setCorrectionModalOpen] = useState(false)
  const [correctionItem, setCorrectionItem] = useState<StockItem | null>(null)
  const [newQuantity, setNewQuantity] = useState<number>(0)
  const [correctionReason, setCorrectionReason] = useState<string>('')
  const [correctionSaving, setCorrectionSaving] = useState(false)
  
  // Form state for drawer
  const [entry, setEntry] = useState<StockEntry>({
    product: null,
    movementType: 'PURCHASE_DEPOT',
    quantity: 1,
    totalCost: 0, // ✅ Toplam tutar
    bonusQuantity: 0,
    serialNumber: '',
    lotNumber: '',
    expiryDate: null,
    batchNumber: '',
    notes: '',
  })

  // Check authentication
  useEffect(() => {
    const tenantId = localStorage.getItem('tenantId')
    if (!tenantId) {
      router.push('/t-login')
    }
  }, [router])

  // Fetch stock data
  const fetchStockData = async () => {
    setLoading(true)
    setError(null)

    try {
      const tenantId = localStorage.getItem('tenantId')
      const username = localStorage.getItem('username')

      if (!tenantId || !username) {
        throw new Error('Oturum bilgileri bulunamadı')
      }

      // Fetch stock summary
      const summaryResponse = await fetch('/api/opas/tenant/stock/summary', {
        headers: {
          'x-tenant-id': tenantId,
          'x-username': username,
        },
      })

      if (!summaryResponse.ok) {
        throw new Error('Stok listesi alınamadı')
      }

      const summaryData = await summaryResponse.json()
      // Backend returns 'summary' array, map it to match our interface
      const items = (summaryData.summary || []).map((item: StockItem & { productName?: string }) => ({
        ...item,
        drugName: item.productName || item.drugName, // Backend uses productName
        category: 'DRUG', // All ITS products are drugs
      }))
      setStockItems(items)

      // Fetch alerts
      const alertsResponse = await fetch('/api/opas/tenant/stock/summary/alerts', {
        headers: {
          'x-tenant-id': tenantId,
          'x-username': username,
        },
      })

      if (alertsResponse.ok) {
        const alertsData = await alertsResponse.json()
        setAlerts(alertsData.alerts || [])
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Stok listesi yüklenemedi')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchStockData()
  }, [])

  // Product search for drawer
  const handleProductSearch = async (searchValue: string) => {
    if (searchValue.length < 2) {
      setProducts([])
      return
    }

    setSearchLoading(true)
    try {
      const tenantId = localStorage.getItem('tenantId')
      const username = localStorage.getItem('username')

      const response = await fetch(
        `/api/opas/tenant/products/search?query=${encodeURIComponent(searchValue)}`,
        {
          headers: {
            'x-tenant-id': tenantId || '',
            'x-username': username || '',
          },
        }
      )

      if (response.ok) {
        const data = await response.json()
        const rawProducts = data.products || data.data || []
        
        // Map snake_case to camelCase
        const mappedProducts = rawProducts.map((p: Record<string, unknown>) => ({
          id: (p.product_id as string) || (p.id as string),
          gtin: p.gtin as string,
          drugName: (p.drug_name as string) || (p.drugName as string),
          manufacturerName: (p.manufacturer_name as string) || (p.manufacturerName as string) || '',
          category: (p.category as string) || 'DRUG',
          hasDatamatrix: (p.has_datamatrix as boolean) || false,
          requiresExpiryTracking: (p.requires_expiry_tracking as boolean) || true,
        }))
        
        setProducts(mappedProducts)
      }
    } catch (err) {
      console.error('Product search error:', err)
    } finally {
      setSearchLoading(false)
    }
  }

  // Save stock entry
  const handleSaveEntry = async () => {
    if (!entry.product) {
      setError('Lütfen bir ürün seçin')
      return
    }

    if (entry.quantity < 1) {
      setError('Miktar en az 1 olmalıdır')
      return
    }

    setSaving(true)
    setError(null)
    setSuccess(null)

    try {
      const tenantId = localStorage.getItem('tenantId')
      const username = localStorage.getItem('username')

      const bonusRatio = entry.bonusQuantity > 0 ? `${entry.quantity}+${entry.bonusQuantity}` : undefined
      
      // ✅ Birim maliyeti hesapla: Toplam tutar / (Miktar + MF)
      const totalQuantity = entry.quantity + entry.bonusQuantity
      const unitCost = totalQuantity > 0 ? entry.totalCost / totalQuantity : 0

      const payload: Record<string, unknown> = {
        movementType: entry.movementType,
        productId: entry.product.id,
        quantityChange: totalQuantity,
        unitCost: unitCost, // ✅ Hesaplanan birim maliyet
        bonusQuantity: entry.bonusQuantity > 0 ? entry.bonusQuantity : undefined,
        bonusRatio: bonusRatio,
        notes: entry.notes || undefined,
      }

      if (entry.product.hasDatamatrix) {
        if (!entry.serialNumber) {
          setError('İlaçlar için seri numarası zorunludur')
          setSaving(false)
          return
        }
        payload.serialNumber = entry.serialNumber
        payload.lotNumber = entry.lotNumber || undefined
        payload.gtin = entry.product.gtin
      }

      if (entry.product.requiresExpiryTracking && entry.expiryDate) {
        payload.expiryDate = entry.expiryDate.format('YYYY-MM-DD')
      }

      const response = await fetch('/api/opas/tenant/stock/movements', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'x-tenant-id': tenantId || '',
          'x-username': username || '',
        },
        body: JSON.stringify(payload),
      })

      if (!response.ok) {
        throw new Error('Stok girişi başarısız')
      }

      const result = await response.json()
      setSuccess(`Stok girişi başarılı! ${result.movement?.movementNumber || ''}`)
      
      // Reset form
      setEntry({
        product: null,
        movementType: 'PURCHASE_DEPOT',
        quantity: 1,
        totalCost: 0,
        bonusQuantity: 0,
        serialNumber: '',
        lotNumber: '',
        expiryDate: null,
        batchNumber: '',
        notes: '',
      })
      setProducts([])

      // Refresh stock list
      await fetchStockData()
      
      // Close drawer after 2 seconds
      setTimeout(() => {
        setDrawerOpen(false)
        setSuccess(null)
      }, 2000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Stok girişi sırasında hata oluştu')
    } finally {
      setSaving(false)
    }
  }

  const handleClearEntry = () => {
    setEntry({
      product: null,
      movementType: 'PURCHASE_DEPOT',
      quantity: 1,
      totalCost: 0,
      bonusQuantity: 0,
      serialNumber: '',
      lotNumber: '',
      expiryDate: null,
      batchNumber: '',
      notes: '',
    })
    setProducts([])
    setError(null)
    setSuccess(null)
  }

  // Stock correction handlers
  const handleOpenCorrection = (item: StockItem) => {
    setCorrectionItem(item)
    setNewQuantity(item.totalQuantity)
    setCorrectionReason('')
    setCorrectionModalOpen(true)
  }

  const handleSaveCorrection = async () => {
    if (!correctionItem) return

    if (newQuantity === correctionItem.totalQuantity) {
      setError('Yeni miktar mevcut miktarla aynı!')
      return
    }

    if (!correctionReason.trim()) {
      setError('Düzeltme sebebi zorunludur!')
      return
    }

    setCorrectionSaving(true)
    setError(null)

    try {
      const tenantId = localStorage.getItem('tenantId')
      const username = localStorage.getItem('username')

      const quantityDifference = newQuantity - correctionItem.totalQuantity

      const payload = {
        movementType: 'CORRECTION',
        productId: correctionItem.productId,
        quantityChange: quantityDifference,
        isCorrection: true,
        correctionReason: correctionReason.trim(),
        notes: `Stok düzeltmesi: ${correctionItem.totalQuantity} → ${newQuantity}`,
      }

      const response = await fetch('/api/opas/tenant/stock/movements', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'x-tenant-id': tenantId || '',
          'x-username': username || '',
        },
        body: JSON.stringify(payload),
      })

      if (!response.ok) {
        throw new Error('Stok düzeltmesi başarısız')
      }

      setSuccess(`Stok başarıyla düzeltildi: ${correctionItem.drugName}`)
      setCorrectionModalOpen(false)
      setCorrectionItem(null)
      setNewQuantity(0)
      setCorrectionReason('')
      
      // Refresh stock list
      await fetchStockData()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Stok düzeltmesi sırasında hata oluştu')
    } finally {
      setCorrectionSaving(false)
    }
  }

  // Get stock level info
  const getStockLevel = (quantity: number) => {
    if (quantity === 0) {
      return { label: 'Yok', color: 'error' as const, icon: <ErrorIcon /> }
    } else if (quantity <= 10) {
      return { label: 'Kritik', color: 'error' as const, icon: <WarningIcon /> }
    } else if (quantity <= 30) {
      return { label: 'Az', color: 'warning' as const, icon: <TrendingDownIcon /> }
    } else if (quantity <= 100) {
      return { label: 'Normal', color: 'success' as const, icon: <CheckCircleIcon /> }
    } else {
      return { label: 'Fazla', color: 'info' as const, icon: <TrendingUpIcon /> }
    }
  }

  // Get expiry warning
  const getExpiryWarning = (expiryDate: string | null) => {
    if (!expiryDate) return null

    const today = new Date()
    const expiry = new Date(expiryDate)
    const daysUntilExpiry = Math.ceil((expiry.getTime() - today.getTime()) / (1000 * 60 * 60 * 24))

    if (daysUntilExpiry < 0) {
      return { label: 'Tarihi Geçmiş', color: 'error' as const }
    } else if (daysUntilExpiry <= 30) {
      return { label: `${daysUntilExpiry} gün`, color: 'error' as const }
    } else if (daysUntilExpiry <= 90) {
      return { label: `${daysUntilExpiry} gün`, color: 'warning' as const }
    }
    return null
  }

  // Filter and sort
  const filteredItems = stockItems
    .filter((item) => {
      // Search filter
      const matchesSearch =
        searchTerm === '' ||
        item.drugName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        item.gtin.includes(searchTerm)

      // Category filter
      const matchesCategory = categoryFilter === 'ALL' || item.category === categoryFilter

      // Alert filter
      const matchesAlert =
        alertFilter === 'ALL' ||
        (alertFilter === 'LOW' && item.totalQuantity <= 30) ||
        (alertFilter === 'ZERO' && item.totalQuantity === 0) ||
        (alertFilter === 'EXPIRING' && item.nearestExpiryDate !== null)

      return matchesSearch && matchesCategory && matchesAlert
    })
    .sort((a, b) => {
      let comparison = 0

      if (sortField === 'drugName') {
        comparison = a.drugName.localeCompare(b.drugName, 'tr')
      } else if (sortField === 'totalQuantity') {
        comparison = a.totalQuantity - b.totalQuantity
      } else if (sortField === 'nearestExpiryDate') {
        if (!a.nearestExpiryDate && !b.nearestExpiryDate) return 0
        if (!a.nearestExpiryDate) return 1
        if (!b.nearestExpiryDate) return -1
        comparison = new Date(a.nearestExpiryDate).getTime() - new Date(b.nearestExpiryDate).getTime()
      }

      return sortOrder === 'asc' ? comparison : -comparison
    })

  // Statistics
  const stats = {
    totalProducts: stockItems.length,
    totalQuantity: stockItems.reduce((sum, item) => sum + item.totalQuantity, 0),
    lowStock: stockItems.filter((item) => item.totalQuantity > 0 && item.totalQuantity <= 30).length,
    outOfStock: stockItems.filter((item) => item.totalQuantity === 0).length,
    criticalAlerts: alerts.filter((a) => a.alertLevel === 'CRITICAL').length,
  }

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
        <CircularProgress />
      </Box>
    )
  }

  return (
    <Box sx={{ p: 1.5 }}>
      {/* Header */}
      <Box sx={{ mb: 2 }}>
        <Box sx={{ display: 'flex', gap: 1, mb: 2, alignItems: 'center' }}>
          <Button
            startIcon={<DashboardIcon />}
            onClick={() => router.push('/t-dashboard')}
            sx={{ textTransform: 'none', color: 'text.secondary' }}
          >
            Ana Sayfa
          </Button>
          <Typography color="text.secondary">/</Typography>
          <Typography color="primary.main" fontWeight={600}>
            Stok Listesi
          </Typography>
        </Box>

        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <InventoryIcon sx={{ fontSize: 40, color: 'primary.main' }} />
            <Box>
              <Typography variant="h4" fontWeight="bold">
                Stok Listesi
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Mevcut stok durumu ve uyarılar
              </Typography>
            </Box>
          </Box>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => setDrawerOpen(true)}
              sx={{
                background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                '&:hover': {
                  background: 'linear-gradient(135deg, #5568d3 0%, #6a4190 100%)',
                },
              }}
            >
              Stok Girişi
            </Button>
            <Button
              variant="outlined"
              startIcon={<CloudUploadIcon />}
              onClick={() => setImportModalOpen(true)}
              sx={{
                borderColor: 'primary.main',
                color: 'primary.main',
                '&:hover': {
                  borderColor: 'primary.dark',
                  bgcolor: 'primary.lighter',
                },
              }}
            >
              Dosyadan İçe Aktar
            </Button>
            <Tooltip title="Yenile">
              <IconButton onClick={fetchStockData} color="primary">
                <RefreshIcon />
              </IconButton>
            </Tooltip>
          </Box>
        </Box>
      </Box>

      {/* Alerts */}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {stats.criticalAlerts > 0 && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          <strong>{stats.criticalAlerts} kritik uyarı var!</strong> Stok azlığı veya tarihi yaklaşan ürünler mevcut.
        </Alert>
      )}

      {/* Statistics Cards */}
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 3 }}>
        <Card sx={{ flex: 1 }}>
          <CardContent>
            <Typography color="text.secondary" variant="body2">
              Toplam Ürün
            </Typography>
            <Typography variant="h4" fontWeight="bold">
              {stats.totalProducts}
            </Typography>
          </CardContent>
        </Card>
        <Card sx={{ flex: 1 }}>
          <CardContent>
            <Typography color="text.secondary" variant="body2">
              Toplam Adet
            </Typography>
            <Typography variant="h4" fontWeight="bold">
              {stats.totalQuantity}
            </Typography>
          </CardContent>
        </Card>
        <Card sx={{ flex: 1, bgcolor: 'warning.light' }}>
          <CardContent>
            <Typography color="warning.contrastText" variant="body2">
              Az Stok
            </Typography>
            <Typography variant="h4" fontWeight="bold" color="warning.contrastText">
              {stats.lowStock}
            </Typography>
          </CardContent>
        </Card>
        <Card sx={{ flex: 1, bgcolor: 'error.light' }}>
          <CardContent>
            <Typography color="error.contrastText" variant="body2">
              Stokta Yok
            </Typography>
            <Typography variant="h4" fontWeight="bold" color="error.contrastText">
              {stats.outOfStock}
            </Typography>
          </CardContent>
        </Card>
      </Stack>

      {/* Filters */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <TextField
              placeholder="Ürün adı veya GTIN ile ara..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              fullWidth
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon />
                  </InputAdornment>
                ),
              }}
            />
            <FormControl sx={{ minWidth: 150 }}>
              <InputLabel>Kategori</InputLabel>
              <Select value={categoryFilter} onChange={(e) => setCategoryFilter(e.target.value)} label="Kategori">
                <MenuItem value="ALL">Tümü</MenuItem>
                <MenuItem value="DRUG">İlaç</MenuItem>
                <MenuItem value="NON_DRUG">OTC/Diğer</MenuItem>
              </Select>
            </FormControl>
            <FormControl sx={{ minWidth: 150 }}>
              <InputLabel>Durum</InputLabel>
              <Select value={alertFilter} onChange={(e) => setAlertFilter(e.target.value)} label="Durum">
                <MenuItem value="ALL">Tümü</MenuItem>
                <MenuItem value="LOW">Az Stok</MenuItem>
                <MenuItem value="ZERO">Stokta Yok</MenuItem>
                <MenuItem value="EXPIRING">SKT Yaklaşan</MenuItem>
              </Select>
            </FormControl>
            <FormControl sx={{ minWidth: 150 }}>
              <InputLabel>Sırala</InputLabel>
              <Select
                value={`${sortField}-${sortOrder}`}
                onChange={(e) => {
                  const [field, order] = e.target.value.split('-')
                  setSortField(field as SortField)
                  setSortOrder(order as SortOrder)
                }}
                label="Sırala"
              >
                <MenuItem value="drugName-asc">İsim (A-Z)</MenuItem>
                <MenuItem value="drugName-desc">İsim (Z-A)</MenuItem>
                <MenuItem value="totalQuantity-asc">Miktar (Az-Çok)</MenuItem>
                <MenuItem value="totalQuantity-desc">Miktar (Çok-Az)</MenuItem>
                <MenuItem value="nearestExpiryDate-asc">SKT (Yakın-Uzak)</MenuItem>
                <MenuItem value="nearestExpiryDate-desc">SKT (Uzak-Yakın)</MenuItem>
              </Select>
            </FormControl>
          </Stack>
        </CardContent>
      </Card>

      {/* Stock Table */}
      <Card>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell><strong>Ürün</strong></TableCell>
                <TableCell><strong>GTIN</strong></TableCell>
                <TableCell><strong>Kategori</strong></TableCell>
                <TableCell align="center"><strong>Toplam</strong></TableCell>
                <TableCell align="center"><strong>Ort. Maliyet</strong></TableCell>
                <TableCell align="center"><strong>Durum</strong></TableCell>
                <TableCell align="center"><strong>En Yakın SKT</strong></TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredItems.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} align="center" sx={{ py: 4 }}>
                    <Typography color="text.secondary">
                      {searchTerm || categoryFilter !== 'ALL' || alertFilter !== 'ALL'
                        ? 'Filtreye uygun ürün bulunamadı'
                        : 'Henüz stok kaydı yok'}
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                filteredItems.map((item) => {
                  const stockLevel = getStockLevel(item.totalQuantity)
                  const expiryWarning = getExpiryWarning(item.nearestExpiryDate)

                  return (
                    <TableRow key={item.productId} hover>
                      <TableCell>
                        <Typography variant="body2" fontWeight={500}>
                          {item.drugName}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
                          {item.gtin}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={item.category === 'DRUG' ? 'İlaç' : 'OTC'}
                          size="small"
                          color={item.category === 'DRUG' ? 'primary' : 'default'}
                        />
                      </TableCell>
                      <TableCell 
                        align="center"
                        sx={{
                          position: 'relative',
                          '&:hover .edit-icon': {
                            opacity: 1,
                          },
                        }}
                      >
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 0.5 }}>
                          <Typography variant="body1" fontWeight="bold">
                            {item.totalQuantity}
                          </Typography>
                          <Tooltip title="Stok düzelt">
                            <IconButton
                              size="small"
                              className="edit-icon"
                              onClick={() => handleOpenCorrection(item)}
                              sx={{
                                opacity: 0,
                                transition: 'opacity 0.2s',
                                p: 0.5,
                                '&:hover': {
                                  bgcolor: 'primary.light',
                                  color: 'primary.main',
                                },
                              }}
                            >
                              <EditIcon sx={{ fontSize: 16 }} />
                            </IconButton>
                          </Tooltip>
                        </Box>
                      </TableCell>
                      <TableCell align="center">
                        <Typography variant="body2" color="text.secondary">
                          {item.averageCost ? `₺${item.averageCost.toFixed(2)}` : '-'}
                        </Typography>
                      </TableCell>
                      <TableCell align="center">
                        <Chip label={stockLevel.label} color={stockLevel.color} size="small" icon={stockLevel.icon} />
                      </TableCell>
                      <TableCell align="center">
                        {item.nearestExpiryDate ? (
                          <Box>
                            <Typography variant="body2" sx={{ fontSize: '0.8rem' }}>
                              {new Date(item.nearestExpiryDate).toLocaleDateString('tr-TR')}
                            </Typography>
                            {expiryWarning && (
                              <Chip
                                label={expiryWarning.label}
                                color={expiryWarning.color}
                                size="small"
                                sx={{ mt: 0.5, height: 18, fontSize: '0.65rem' }}
                              />
                            )}
                          </Box>
                        ) : (
                          <Typography variant="body2" color="text.secondary">
                            -
                          </Typography>
                        )}
                      </TableCell>
                    </TableRow>
                  )
                })
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>

      {/* Result Count */}
      {filteredItems.length > 0 && (
        <Box sx={{ mt: 2, textAlign: 'center' }}>
          <Typography variant="body2" color="text.secondary">
            Toplam {filteredItems.length} ürün gösteriliyor
          </Typography>
        </Box>
      )}

      {/* Stock Entry Drawer */}
      <Drawer
        anchor="right"
        open={drawerOpen}
        onClose={() => !saving && setDrawerOpen(false)}
        PaperProps={{
          sx: { width: { xs: '100%', sm: 500 }, p: 3 },
        }}
      >
        <LocalizationProvider dateAdapter={AdapterDayjs} adapterLocale="tr">
          <Box>
            {/* Drawer Header */}
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <LocalShippingIcon sx={{ fontSize: 28, color: 'primary.main' }} />
                <Typography variant="h5" fontWeight="bold">
                  Stok Girişi
                </Typography>
              </Box>
              <IconButton onClick={() => setDrawerOpen(false)} disabled={saving}>
                <CloseIcon />
              </IconButton>
            </Box>

            <Divider sx={{ mb: 3 }} />

            {/* Alerts */}
            {success && (
              <Alert severity="success" sx={{ mb: 2 }}>
                {success}
              </Alert>
            )}

            {/* Product Search */}
            <Autocomplete
              options={products}
              value={entry.product}
              onChange={(_, newValue) => setEntry({ ...entry, product: newValue })}
              onInputChange={(_, newInputValue) => {
                handleProductSearch(newInputValue)
              }}
              getOptionLabel={(option) => option.drugName}
              loading={searchLoading}
              noOptionsText="Ürün bulunamadı"
              loadingText="Aranıyor..."
              renderOption={(props, option) => {
                const { key, ...otherProps } = props as React.HTMLAttributes<HTMLLIElement> & { key: string }
                return (
                  <li key={key} {...otherProps}>
                    <Box sx={{ width: '100%' }}>
                      <Typography variant="body2" sx={{ fontSize: '0.7rem', fontWeight: 600 }}>
                        {option.drugName}
                      </Typography>
                      <Stack direction="row" spacing={0.5} sx={{ mt: 0.5 }}>
                        <Chip label={option.gtin} size="small" sx={{ height: 14, fontSize: '0.6rem' }} />
                        <Chip
                          label={option.category === 'DRUG' ? 'İlaç' : 'OTC'}
                          size="small"
                          color={option.category === 'DRUG' ? 'primary' : 'default'}
                          sx={{ height: 14, fontSize: '0.6rem' }}
                        />
                      </Stack>
                    </Box>
                  </li>
                )
              }}
              renderInput={(params) => (
                <TextField
                  {...params}
                  label="Ürün Ara (İsim veya GTIN)"
                  placeholder="En az 2 karakter..."
                  fullWidth
                  autoFocus
                />
              )}
              sx={{ mb: 2 }}
            />

            {/* Movement Type */}
            <TextField
              select
              label="Giriş Tipi"
              value={entry.movementType}
              onChange={(e) => setEntry({ ...entry, movementType: e.target.value })}
              fullWidth
              sx={{ mb: 2 }}
            >
              {MOVEMENT_TYPES.map((type) => (
                <MenuItem key={type.value} value={type.value}>
                  {type.icon} {type.label}
                </MenuItem>
              ))}
            </TextField>

            {/* Quantity & Bonus */}
            <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
              <TextField
                type="number"
                label="Miktar"
                value={entry.quantity}
                onChange={(e) => setEntry({ ...entry, quantity: e.target.value === '' ? 1 : parseInt(e.target.value) })}
                onFocus={(e) => e.target.select()}
                fullWidth
                InputProps={{ inputProps: { min: 1 } }}
              />
              <TextField
                type="number"
                label="MF"
                value={entry.bonusQuantity === 0 ? '' : entry.bonusQuantity}
                onChange={(e) => setEntry({ ...entry, bonusQuantity: e.target.value === '' ? 0 : parseInt(e.target.value) })}
                onFocus={(e) => e.target.select()}
                fullWidth
                InputProps={{ inputProps: { min: 0 } }}
                helperText={entry.bonusQuantity > 0 ? `${entry.quantity}+${entry.bonusQuantity}` : undefined}
              />
            </Box>

            {/* Total Cost */}
            <TextField
              type="number"
              label="Toplam Tutar (₺)"
              value={entry.totalCost === 0 ? '' : entry.totalCost}
              onChange={(e) => setEntry({ ...entry, totalCost: e.target.value === '' ? 0 : parseFloat(e.target.value) })}
              onFocus={(e) => e.target.select()}
              fullWidth
              InputProps={{ inputProps: { min: 0, step: 0.01 } }}
              placeholder="0.00"
              sx={{ mb: 2 }}
              helperText={
                entry.totalCost > 0 && entry.quantity > 0
                  ? `Birim Maliyet: ₺${(entry.totalCost / (entry.quantity + entry.bonusQuantity)).toFixed(2)}`
                  : undefined
              }
            />

            {/* Serial Number (for drugs) */}
            {entry.product?.hasDatamatrix && (
              <TextField
                label="Seri Numarası *"
                value={entry.serialNumber}
                onChange={(e) => setEntry({ ...entry, serialNumber: e.target.value })}
                fullWidth
                required
                sx={{ mb: 2 }}
                helperText="İlaçlar için zorunlu"
              />
            )}

            {/* Expiry Date */}
            {entry.product?.requiresExpiryTracking && (
              <DatePicker
                label="Son Kullanma Tarihi"
                value={entry.expiryDate}
                onChange={(newValue) => setEntry({ ...entry, expiryDate: newValue })}
                minDate={dayjs()}
                format="DD/MM/YYYY"
                slotProps={{
                  textField: {
                    fullWidth: true,
                    sx: { mb: 2 },
                  },
                }}
              />
            )}

            {/* Notes */}
            <TextField
              label="Notlar"
              value={entry.notes}
              onChange={(e) => setEntry({ ...entry, notes: e.target.value })}
              fullWidth
              multiline
              rows={2}
              sx={{ mb: 3 }}
            />

            {/* Cost Summary */}
            {entry.product && entry.totalCost > 0 && (
              <Paper sx={{ p: 2, mb: 3, bgcolor: 'primary.light', color: 'primary.contrastText' }}>
                <Typography variant="body2" fontWeight="bold">
                  Toplam Tutar: ₺{entry.totalCost.toFixed(2)}
                </Typography>
                <Typography variant="caption" sx={{ display: 'block', mt: 0.5, opacity: 0.9 }}>
                  Toplam {entry.quantity + entry.bonusQuantity} adet
                  {' → '}
                  Birim Maliyet: ₺{(entry.totalCost / (entry.quantity + entry.bonusQuantity)).toFixed(2)}
                </Typography>
              </Paper>
            )}

            {/* Action Buttons */}
            <Box sx={{ display: 'flex', gap: 2 }}>
              <Button
                variant="contained"
                startIcon={saving ? <CircularProgress size={20} color="inherit" /> : <SaveIcon />}
                onClick={handleSaveEntry}
                disabled={saving || !entry.product}
                fullWidth
                sx={{
                  background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                  '&:hover': {
                    background: 'linear-gradient(135deg, #5568d3 0%, #6a4190 100%)',
                  },
                }}
              >
                Kaydet
              </Button>
              <Button
                variant="outlined"
                startIcon={<ClearIcon />}
                onClick={handleClearEntry}
                disabled={saving}
                fullWidth
              >
                Temizle
              </Button>
            </Box>
          </Box>
        </LocalizationProvider>
      </Drawer>

      {/* Stock Correction Modal */}
      <Dialog
        open={correctionModalOpen}
        onClose={() => !correctionSaving && setCorrectionModalOpen(false)}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle sx={{ pb: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <EditIcon color="primary" />
            <Typography variant="h6">Stok Düzeltme</Typography>
          </Box>
        </DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 1 }}>
            {correctionItem && (
              <>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  {correctionItem.drugName}
                </Typography>
                <Divider sx={{ my: 2 }} />
                <TextField
                  label="Mevcut Miktar"
                  value={correctionItem.totalQuantity}
                  disabled
                  fullWidth
                  sx={{ mb: 2 }}
                  InputProps={{
                    sx: { bgcolor: 'grey.100' },
                  }}
                />
                <TextField
                  label="Yeni Miktar"
                  type="number"
                  value={newQuantity === 0 ? '' : newQuantity}
                  onChange={(e) => setNewQuantity(e.target.value === '' ? 0 : parseInt(e.target.value))}
                  onFocus={(e) => e.target.select()}
                  fullWidth
                  autoFocus
                  sx={{ mb: 2 }}
                  InputProps={{
                    inputProps: { min: 0 },
                  }}
                  placeholder="0"
                  helperText={correctionItem ? `Fark: ${newQuantity - correctionItem.totalQuantity > 0 ? '+' : ''}${newQuantity - correctionItem.totalQuantity}` : ''}
                />
                <TextField
                  label="Düzeltme Sebebi *"
                  value={correctionReason}
                  onChange={(e) => setCorrectionReason(e.target.value)}
                  fullWidth
                  multiline
                  rows={2}
                  placeholder="Örn: Sayım sonucu, fire, vb."
                  helperText="Zorunlu alan"
                />
              </>
            )}
          </Box>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button
            onClick={() => setCorrectionModalOpen(false)}
            disabled={correctionSaving}
            color="inherit"
          >
            İptal
          </Button>
          <Button
            onClick={handleSaveCorrection}
            disabled={correctionSaving || !correctionReason.trim() || (correctionItem ? newQuantity === correctionItem.totalQuantity : false)}
            variant="contained"
            startIcon={correctionSaving ? <CircularProgress size={20} color="inherit" /> : <SaveIcon />}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      {/* Stock Import Modal */}
      <StockImportModal
        open={importModalOpen}
        onClose={() => setImportModalOpen(false)}
        onSuccess={() => {
          fetchStockData()
          setImportModalOpen(false)
        }}
      />
    </Box>
  )
}

