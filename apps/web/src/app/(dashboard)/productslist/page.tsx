'use client'

import { useState, useEffect, useCallback } from 'react'
import { 
  Box, 
  Typography, 
  TextField, 
  MenuItem, 
  FormControl, 
  InputLabel, 
  Select, 
  Chip,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  IconButton,
  InputAdornment,
  Alert,
  Card,
  CardContent,
  Button,
  Tooltip,
  TableSortLabel,
  Skeleton,
  Stack,
  Divider
} from '@mui/material'
import { 
  Search as SearchIcon,
  Add as AddIcon,
  Edit as EditIcon,
  Visibility as ViewIcon,
  Download as DownloadIcon,
  Upload as UploadIcon,
  Sync as SyncIcon,
  TrendingUp as TrendingUpIcon,
  Inventory as InventoryIcon,
  Business as BusinessIcon,
  MonetizationOn as MoneyIcon,
  Schedule as ScheduleIcon
} from '@mui/icons-material'
import TenantSidebar from '@/components/TenantSidebar'
import TenantNavbar from '@/components/TenantNavbar'

interface ProductRecord {
  id: string
  gtin: string
  drugName: string
  manufacturerGln: string
  manufacturerName: string
  price: number
  priceHistory: string
  isActive: boolean
  lastItsSyncAt: string | null
  createdAt: string
  updatedAt: string | null
}

interface ProductStats {
  totalProducts: number
  activeProducts: number
  inactiveProducts: number
  totalManufacturers: number
  averagePrice: number
  lastAdded: string | null
}

interface ProductListResponse {
  data: ProductRecord[]
  totalCount: number
  page: number
  limit: number
  totalPages: number
}

type SortField = 'drug_name' | 'gtin' | 'manufacturer_name' | 'price' | 'created_at_utc'
type SortOrder = 'asc' | 'desc'

export default function ProductsListPage() {
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const [products, setProducts] = useState<ProductRecord[]>([])
  const [stats, setStats] = useState<ProductStats | null>(null)
  const [loading, setLoading] = useState(true)
  const [statsLoading, setStatsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  
  // Filters and search
  const [searchTerm, setSearchTerm] = useState('')
  const [manufacturerFilter, setManufacturerFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>('all')
  
  // Sorting
  const [sortBy, setSortBy] = useState<SortField>('drug_name')
  const [sortOrder, setSortOrder] = useState<SortOrder>('asc')
  
  // Pagination
  const [page, setPage] = useState(0)
  const [rowsPerPage, setRowsPerPage] = useState(50)
  const [totalCount, setTotalCount] = useState(0)
  const [, setTotalPages] = useState(0)

  // Load product statistics
  const loadStats = useCallback(async () => {
    try {
      setStatsLoading(true)
      const tenantId = localStorage.getItem('tenantId')
      
      if (!tenantId) {
        console.error('Tenant ID not found in localStorage')
        setStatsLoading(false)
        return
      }
      
      const response = await fetch('/api/opas/tenant/products/stats', {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'x-tenant-id': tenantId
        }
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      const data = await response.json()
      setStats(data)
    } catch (err) {
      console.error('Stats load error:', err)
    } finally {
      setStatsLoading(false)
    }
  }, [])

  // Load product data
  const loadProducts = useCallback(async () => {
    try {
      setLoading(true)
      const tenantId = localStorage.getItem('tenantId')
      
      if (!tenantId) {
        console.error('Tenant ID not found in localStorage')
        setLoading(false)
        return
      }
      
      const params = new URLSearchParams({
        page: page.toString(),
        limit: rowsPerPage.toString(),
        sortBy,
        sortOrder
      })

      if (searchTerm) params.append('search', searchTerm)
      if (manufacturerFilter) params.append('manufacturer', manufacturerFilter)
      if (statusFilter !== 'all') params.append('active', statusFilter === 'active' ? 'true' : 'false')

      const response = await fetch(`/api/opas/tenant/products?${params.toString()}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'x-tenant-id': tenantId
        }
      })

      if (!response.ok) {
        const errorText = await response.text()
        throw new Error(`HTTP error! status: ${response.status} - ${errorText}`)
      }

      const result: ProductListResponse = await response.json()
      setProducts(result.data)
      setTotalCount(result.totalCount)
      setTotalPages(result.totalPages)
      
      console.log('Loaded products:', result.data.length, 'of', result.totalCount)
    } catch (err) {
      console.error('Product load error:', err)
      setError('Ürün verileri yüklenirken hata oluştu')
    } finally {
      setLoading(false)
    }
  }, [page, rowsPerPage, searchTerm, manufacturerFilter, statusFilter, sortBy, sortOrder])

  // Load data on mount and when dependencies change
  useEffect(() => {
    loadStats()
  }, [loadStats])

  useEffect(() => {
    loadProducts()
  }, [loadProducts])

  // Reset page when filters change
  useEffect(() => {
    setPage(0)
  }, [searchTerm, manufacturerFilter, statusFilter])

  const handleSort = (field: SortField) => {
    if (sortBy === field) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')
    } else {
      setSortBy(field)
      setSortOrder('asc')
    }
  }

  const handleChangePage = (event: unknown, newPage: number) => {
    setPage(newPage)
  }

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10))
    setPage(0)
  }

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY',
      minimumFractionDigits: 2
    }).format(price)
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('tr-TR', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: '#f8fafc' }}>
      <TenantSidebar open={sidebarOpen} onToggle={() => setSidebarOpen(!sidebarOpen)} currentPath="/productslist" />
      
      <Box 
        component="main" 
        sx={{ 
          flexGrow: 1,
          minHeight: '100vh',
          position: 'relative',
          transition: 'margin 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
          marginLeft: { 
            xs: 0,
            md: sidebarOpen ? '280px' : 0
          }
        }}
      >
        <TenantNavbar open={sidebarOpen} onToggle={() => setSidebarOpen(!sidebarOpen)} onSidebarToggle={() => setSidebarOpen(!sidebarOpen)} />
        
        <Box sx={{ p: 3 }}>
          {/* Header */}
          <Box sx={{ mb: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Box>
              <Typography variant="h4" component="h1" sx={{ 
                fontWeight: 600, 
                color: '#1e293b',
                mb: 1
              }}>
                Ürünler
              </Typography>
              <Typography variant="body1" sx={{ color: '#64748b' }}>
                İlaç ürünlerini görüntüleyin ve yönetin
              </Typography>
            </Box>
            
            <Stack direction="row" spacing={2}>
              <Button
                variant="outlined"
                startIcon={<SyncIcon />}
                onClick={() => {
                  loadProducts()
                  loadStats()
                }}
                disabled={loading}
              >
                Yenile
              </Button>
              <Button
                variant="outlined"
                startIcon={<DownloadIcon />}
              >
                Dışa Aktar
              </Button>
              <Button
                variant="outlined"
                startIcon={<UploadIcon />}
              >
                İçe Aktar
              </Button>
              <Button
                variant="contained"
                startIcon={<AddIcon />}
                sx={{ bgcolor: '#3b82f6', '&:hover': { bgcolor: '#2563eb' } }}
              >
                Yeni Ürün
              </Button>
            </Stack>
          </Box>

          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          {/* Statistics Cards */}
          <Box sx={{ 
            display: 'grid', 
            gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', md: '1fr 1fr 1fr 1fr' },
            gap: 3,
            mb: 3
          }}>
            <Card sx={{ borderRadius: 2 }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                  <InventoryIcon sx={{ color: '#3b82f6', mr: 1 }} />
                  <Typography variant="h6" sx={{ fontWeight: 600 }}>
                    Toplam Ürün
                  </Typography>
                </Box>
                {statsLoading ? (
                  <Skeleton width={80} height={40} />
                ) : (
                  <Typography variant="h4" sx={{ fontWeight: 700, color: '#1e293b' }}>
                    {stats?.totalProducts.toLocaleString('tr-TR') || '0'}
                  </Typography>
                )}
              </CardContent>
            </Card>

            <Card sx={{ borderRadius: 2 }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                  <TrendingUpIcon sx={{ color: '#10b981', mr: 1 }} />
                  <Typography variant="h6" sx={{ fontWeight: 600 }}>
                    Aktif Ürün
                  </Typography>
                </Box>
                {statsLoading ? (
                  <Skeleton width={80} height={40} />
                ) : (
                  <Typography variant="h4" sx={{ fontWeight: 700, color: '#10b981' }}>
                    {stats?.activeProducts.toLocaleString('tr-TR') || '0'}
                  </Typography>
                )}
              </CardContent>
            </Card>

            <Card sx={{ borderRadius: 2 }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                  <BusinessIcon sx={{ color: '#8b5cf6', mr: 1 }} />
                  <Typography variant="h6" sx={{ fontWeight: 600 }}>
                    Üretici Sayısı
                  </Typography>
                </Box>
                {statsLoading ? (
                  <Skeleton width={80} height={40} />
                ) : (
                  <Typography variant="h4" sx={{ fontWeight: 700, color: '#8b5cf6' }}>
                    {stats?.totalManufacturers.toLocaleString('tr-TR') || '0'}
                  </Typography>
                )}
              </CardContent>
            </Card>

            <Card sx={{ borderRadius: 2 }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                  <MoneyIcon sx={{ color: '#f59e0b', mr: 1 }} />
                  <Typography variant="h6" sx={{ fontWeight: 600 }}>
                    Ortalama Fiyat
                  </Typography>
                </Box>
                {statsLoading ? (
                  <Skeleton width={80} height={40} />
                ) : (
                  <Typography variant="h4" sx={{ fontWeight: 700, color: '#f59e0b' }}>
                    {stats ? formatPrice(stats.averagePrice) : '₺0,00'}
                  </Typography>
                )}
              </CardContent>
            </Card>
          </Box>

          {/* Filters */}
          <Paper sx={{ p: 3, mb: 3, borderRadius: 2 }}>
            <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
              Filtreler ve Arama
            </Typography>
            
            <Box sx={{ 
              display: 'grid', 
              gridTemplateColumns: { xs: '1fr', md: '2fr 1fr 1.5fr 1.5fr' },
              gap: 2,
              alignItems: 'center'
            }}>
              <TextField
                fullWidth
                placeholder="Ürün adı, barkod veya üretici ara..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchIcon sx={{ color: '#64748b' }} />
                    </InputAdornment>
                  ),
                }}
              />

              <FormControl fullWidth>
                <InputLabel>Durum</InputLabel>
                <Select
                  value={statusFilter}
                  label="Durum"
                  onChange={(e) => setStatusFilter(e.target.value)}
                >
                  <MenuItem value="all">Tümü</MenuItem>
                  <MenuItem value="active">Aktif</MenuItem>
                  <MenuItem value="inactive">Pasif</MenuItem>
                </Select>
              </FormControl>

              <TextField
                fullWidth
                label="Üretici"
                placeholder="Üretici adı ara..."
                value={manufacturerFilter}
                onChange={(e) => setManufacturerFilter(e.target.value)}
              />

              <Stack direction="row" spacing={1} alignItems="center">
                <Typography variant="body2" sx={{ color: '#64748b', minWidth: 'fit-content' }}>
                  Toplam: {totalCount.toLocaleString('tr-TR')} ürün
                </Typography>
                {(searchTerm || manufacturerFilter || statusFilter !== 'all') && (
                  <Button
                    size="small"
                    onClick={() => {
                      setSearchTerm('')
                      setManufacturerFilter('')
                      setStatusFilter('all')
                    }}
                  >
                    Temizle
                  </Button>
                )}
              </Stack>
            </Box>
          </Paper>

          {/* Products Table */}
          <Paper sx={{ borderRadius: 2, overflow: 'hidden' }}>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow sx={{ bgcolor: '#f8fafc' }}>
                    <TableCell>
                      <TableSortLabel
                        active={sortBy === 'gtin'}
                        direction={sortBy === 'gtin' ? sortOrder : 'asc'}
                        onClick={() => handleSort('gtin')}
                        sx={{ fontWeight: 600 }}
                      >
                        Barkod
                      </TableSortLabel>
                    </TableCell>
                    <TableCell>
                      <TableSortLabel
                        active={sortBy === 'drug_name'}
                        direction={sortBy === 'drug_name' ? sortOrder : 'asc'}
                        onClick={() => handleSort('drug_name')}
                        sx={{ fontWeight: 600 }}
                      >
                        Ürün Adı
                      </TableSortLabel>
                    </TableCell>
                    <TableCell>
                      <TableSortLabel
                        active={sortBy === 'manufacturer_name'}
                        direction={sortBy === 'manufacturer_name' ? sortOrder : 'asc'}
                        onClick={() => handleSort('manufacturer_name')}
                        sx={{ fontWeight: 600 }}
                      >
                        Üretici
                      </TableSortLabel>
                    </TableCell>
                    <TableCell>
                      <TableSortLabel
                        active={sortBy === 'price'}
                        direction={sortBy === 'price' ? sortOrder : 'asc'}
                        onClick={() => handleSort('price')}
                        sx={{ fontWeight: 600 }}
                      >
                        Fiyat
                      </TableSortLabel>
                    </TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Durum</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Son Sync</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>İşlemler</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {loading ? (
                    Array.from(new Array(rowsPerPage)).map((_, index) => (
                      <TableRow key={index}>
                        <TableCell><Skeleton /></TableCell>
                        <TableCell><Skeleton /></TableCell>
                        <TableCell><Skeleton /></TableCell>
                        <TableCell><Skeleton /></TableCell>
                        <TableCell><Skeleton /></TableCell>
                        <TableCell><Skeleton /></TableCell>
                        <TableCell><Skeleton /></TableCell>
                      </TableRow>
                    ))
                  ) : products.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={7} sx={{ textAlign: 'center', py: 4 }}>
                        <Typography variant="body1" sx={{ color: '#64748b' }}>
                          Kayıt bulunamadı
                        </Typography>
                      </TableCell>
                    </TableRow>
                  ) : (
                    products.map((product) => (
                      <TableRow key={product.id} hover>
                        <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.875rem' }}>
                          {product.gtin}
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" sx={{ fontWeight: 500 }}>
                            {product.drugName}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" sx={{ color: '#64748b' }}>
                            {product.manufacturerName || '-'}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" sx={{ fontWeight: 600, color: '#059669' }}>
                            {formatPrice(product.price)}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Chip 
                            label={product.isActive ? 'Aktif' : 'Pasif'}
                            color={product.isActive ? 'success' : 'default'}
                            size="small"
                          />
                        </TableCell>
                        <TableCell>
                          {product.lastItsSyncAt ? (
                            <Tooltip title={formatDate(product.lastItsSyncAt)}>
                              <Chip
                                icon={<ScheduleIcon />}
                                label="Sync'li"
                                size="small"
                                color="info"
                              />
                            </Tooltip>
                          ) : (
                            <Chip
                              label="Sync yok"
                              size="small"
                              color="warning"
                            />
                          )}
                        </TableCell>
                        <TableCell>
                          <Stack direction="row" spacing={1}>
                            <Tooltip title="Görüntüle">
                              <IconButton size="small" color="primary">
                                <ViewIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Düzenle">
                              <IconButton size="small" color="primary">
                                <EditIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          </Stack>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>

            {/* Pagination */}
            <Divider />
            <TablePagination
              rowsPerPageOptions={[25, 50, 100, 200]}
              component="div"
              count={totalCount}
              rowsPerPage={rowsPerPage}
              page={page}
              onPageChange={handleChangePage}
              onRowsPerPageChange={handleChangeRowsPerPage}
              labelRowsPerPage="Sayfa başına:"
              labelDisplayedRows={({ from, to, count }) => 
                `${from}-${to} / ${count !== -1 ? count : `${to}'den fazla`}`
              }
              sx={{ px: 2 }}
            />
          </Paper>
        </Box>
      </Box>
    </Box>
  )
}
