'use client'

import { useState, useEffect } from 'react'
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
  Alert
} from '@mui/material'
import { 
  Search as SearchIcon,
  Visibility as ViewIcon,
  Edit as EditIcon
} from '@mui/icons-material'
// TenantSidebar and TenantNavbar removed - using global layout

interface GlnRecord {
  id: number
  gln: string
  companyName: string | null
  authorized: string | null
  email: string | null
  phone: string | null
  city: string | null
  town: string | null
  address: string | null
  active: boolean
  source: string | null
  importedAt: string
}

export default function StakeholdersPage() {
  // Sidebar state removed - using global layout
  const [glnList, setGlnList] = useState<GlnRecord[]>([])
  const [filteredList, setFilteredList] = useState<GlnRecord[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  
  // Filters
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedCity, setSelectedCity] = useState('')
  const [selectedDistrict, setSelectedDistrict] = useState('')
  const [statusFilter, setStatusFilter] = useState('active') // Default to active
  
  // Pagination
  const [page, setPage] = useState(0)
  const [rowsPerPage, setRowsPerPage] = useState(25)

  // Get unique cities and districts for filters
  const cities = [...new Set(glnList.map(item => item.city).filter((city): city is string => Boolean(city)))].sort()
  const districts = selectedCity 
    ? [...new Set(glnList.filter(item => item.city === selectedCity).map(item => item.town).filter((town): town is string => Boolean(town)))].sort()
    : []

  // Load GLN data from tenant database
  useEffect(() => {
    const loadGlnData = async () => {
      try {
        setLoading(true)
        
        // Get tenant ID from localStorage
        const tenantId = localStorage.getItem('tenantId')
        
        if (!tenantId) {
          console.error('Tenant ID not found in localStorage - Redirecting to login')
          window.location.href = '/t-login'
          return
        }
        
        console.log('Fetching GLN list for tenant:', tenantId)
        
        const response = await fetch('/api/opas/tenant/gln-list', {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
            'x-tenant-id': tenantId
          }
        })

        if (!response.ok) {
          const errorText = await response.text()
          console.error('Backend error response:', errorText)
          throw new Error(`HTTP error! status: ${response.status} - ${errorText}`)
        }

        const data = await response.json()
        
        // Transform backend data to frontend format
        const transformedData: GlnRecord[] = data.map((item: {
          id: number;
          gln: string;
          companyName: string;
          authorized: string;
          email: string;
          phone: string;
          city: string;
          town: string;
          address: string;
          active: boolean;
          source: string;
          importedAt: string;
        }) => ({
          id: item.id,
          gln: item.gln,
          companyName: item.companyName,
          authorized: item.authorized,
          email: item.email,
          phone: item.phone,
          city: item.city,
          town: item.town,
          address: item.address,
          active: item.active,
          source: item.source,
          importedAt: item.importedAt
        }))
        
        console.log('Loaded GLN data:', transformedData.length, 'records')
        setGlnList(transformedData)
      } catch (err) {
        console.error('GLN data load error:', err)
        setError('Paydaş verileri yüklenirken hata oluştu')
      } finally {
        setLoading(false)
      }
    }

    loadGlnData()
  }, [])

  // Filter logic
  useEffect(() => {
    let filtered = glnList

    // Status filter (active/inactive/all)
    if (statusFilter === 'active') {
      filtered = filtered.filter(item => item.active)
    } else if (statusFilter === 'inactive') {
      filtered = filtered.filter(item => !item.active)
    }

    // Search filter
    if (searchTerm) {
      const search = searchTerm.toLowerCase()
      filtered = filtered.filter(item => 
        item.gln.toLowerCase().includes(search) ||
        (item.companyName && item.companyName.toLowerCase().includes(search)) ||
        (item.city && item.city.toLowerCase().includes(search)) ||
        (item.town && item.town.toLowerCase().includes(search))
      )
    }

    // City filter
    if (selectedCity) {
      filtered = filtered.filter(item => item.city === selectedCity)
    }

    // District filter
    if (selectedDistrict) {
      filtered = filtered.filter(item => item.town === selectedDistrict)
    }

    setFilteredList(filtered)
    setPage(0) // Reset to first page when filters change
  }, [glnList, searchTerm, selectedCity, selectedDistrict, statusFilter])

  const handleChangePage = (event: unknown, newPage: number) => {
    setPage(newPage)
  }

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10))
    setPage(0)
  }

  const handleCityChange = (city: string) => {
    setSelectedCity(city)
    setSelectedDistrict('') // Reset district when city changes
  }

  return (
    <Box sx={{ p: 3 }}>
          {/* Header */}
          <Box sx={{ mb: 3 }}>
            <Typography variant="h4" component="h1" sx={{ 
              fontWeight: 600, 
              color: '#1e293b',
              mb: 1
            }}>
              Paydaşlar
            </Typography>
            <Typography variant="body1" sx={{ color: '#64748b' }}>
              GLN kayıtlarını görüntüleyin ve yönetin
            </Typography>
          </Box>

          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          {/* Filters */}
          <Paper sx={{ p: 3, mb: 3, borderRadius: 2 }}>
            <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'center' }}>
              {/* Search */}
              <TextField
                placeholder="GLN, eczane adı, şehir veya ilçe ara..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                sx={{ minWidth: 300, flexGrow: 1 }}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchIcon sx={{ color: '#64748b' }} />
                    </InputAdornment>
                  ),
                }}
              />

              {/* Status Filter */}
              <FormControl sx={{ minWidth: 120 }}>
                <InputLabel>Durum</InputLabel>
                <Select
                  value={statusFilter}
                  label="Durum"
                  onChange={(e) => setStatusFilter(e.target.value)}
                >
                  <MenuItem value="active">Aktif</MenuItem>
                  <MenuItem value="inactive">Pasif</MenuItem>
                  <MenuItem value="all">Tümü</MenuItem>
                </Select>
              </FormControl>

              {/* City Filter */}
              <FormControl sx={{ minWidth: 150 }}>
                <InputLabel>İl</InputLabel>
                <Select
                  value={selectedCity}
                  label="İl"
                  onChange={(e) => handleCityChange(e.target.value)}
                >
                  <MenuItem value="">Tümü</MenuItem>
                  {cities.map(city => (
                    <MenuItem key={city} value={city}>{city}</MenuItem>
                  ))}
                </Select>
              </FormControl>

              {/* District Filter */}
              <FormControl sx={{ minWidth: 150 }}>
                <InputLabel>İlçe</InputLabel>
                <Select
                  value={selectedDistrict}
                  label="İlçe"
                  onChange={(e) => setSelectedDistrict(e.target.value)}
                  disabled={!selectedCity}
                >
                  <MenuItem value="">Tümü</MenuItem>
                  {districts.map(district => (
                    <MenuItem key={district} value={district}>{district}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Box>

            {/* Active Filters */}
            {(searchTerm || selectedCity || selectedDistrict || statusFilter !== 'active') && (
              <Box sx={{ mt: 2, display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                {searchTerm && (
                  <Chip 
                    label={`Arama: ${searchTerm}`} 
                    onDelete={() => setSearchTerm('')} 
                    size="small" 
                  />
                )}
                {selectedCity && (
                  <Chip 
                    label={`İl: ${selectedCity}`} 
                    onDelete={() => handleCityChange('')} 
                    size="small" 
                  />
                )}
                {selectedDistrict && (
                  <Chip 
                    label={`İlçe: ${selectedDistrict}`} 
                    onDelete={() => setSelectedDistrict('')} 
                    size="small" 
                  />
                )}
                {statusFilter !== 'active' && (
                  <Chip 
                    label={`Durum: ${statusFilter === 'inactive' ? 'Pasif' : 'Tümü'}`} 
                    onDelete={() => setStatusFilter('active')} 
                    size="small" 
                  />
                )}
              </Box>
            )}
          </Paper>

          {/* Results Table */}
          <Paper sx={{ borderRadius: 2, overflow: 'hidden' }}>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow sx={{ bgcolor: '#f8fafc' }}>
                    <TableCell sx={{ fontWeight: 600 }}>GLN</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Eczane Adı</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>İl</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>İlçe</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Telefon</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Durum</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>İşlemler</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {loading ? (
                    <TableRow>
                      <TableCell colSpan={7} sx={{ textAlign: 'center', py: 4 }}>
                        Yükleniyor...
                      </TableCell>
                    </TableRow>
                  ) : filteredList.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={7} sx={{ textAlign: 'center', py: 4 }}>
                        Kayıt bulunamadı
                      </TableCell>
                    </TableRow>
                  ) : (
                    filteredList
                      .slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
                      .map((record) => (
                        <TableRow key={record.id} hover>
                          <TableCell sx={{ fontFamily: 'monospace' }}>{record.gln}</TableCell>
                          <TableCell>{record.companyName || '-'}</TableCell>
                          <TableCell>{record.city || '-'}</TableCell>
                          <TableCell>{record.town || '-'}</TableCell>
                          <TableCell>{record.phone || '-'}</TableCell>
                          <TableCell>
                            <Chip 
                              label={record.active ? 'Aktif' : 'Pasif'}
                              color={record.active ? 'success' : 'default'}
                              size="small"
                            />
                          </TableCell>
                          <TableCell>
                            <Box sx={{ display: 'flex', gap: 1 }}>
                              <IconButton size="small" color="primary">
                                <ViewIcon fontSize="small" />
                              </IconButton>
                              <IconButton size="small" color="primary">
                                <EditIcon fontSize="small" />
                              </IconButton>
                            </Box>
                          </TableCell>
                        </TableRow>
                      ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>

            {/* Pagination */}
            <TablePagination
              rowsPerPageOptions={[10, 25, 50, 100]}
              component="div"
              count={filteredList.length}
              rowsPerPage={rowsPerPage}
              page={page}
              onPageChange={handleChangePage}
              onRowsPerPageChange={handleChangeRowsPerPage}
              labelRowsPerPage="Sayfa başına:"
              labelDisplayedRows={({ from, to, count }) => 
                `${from}-${to} / ${count !== -1 ? count : `${to}'den fazla`}`
              }
            />
          </Paper>
    </Box>
  )
}