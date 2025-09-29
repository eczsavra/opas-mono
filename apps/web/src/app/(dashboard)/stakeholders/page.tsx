'use client'

import { useState, useEffect } from 'react'
import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Typography,
  TextField,
  InputAdornment,
  Chip,
  CircularProgress,
  Alert,
  Card,
  CardContent,
  IconButton,
  Tooltip,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Stack,
  Button
} from '@mui/material'
import {
  Search as SearchIcon,
  Store as StoreIcon,
  LocationCity as CityIcon,
  Email as EmailIcon,
  Phone as PhoneIcon,
  Person as PersonIcon,
  Refresh as RefreshIcon,
  Clear as ClearIcon,
  Home as HomeIcon,
  ArrowBack as ArrowBackIcon
} from '@mui/icons-material'

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

interface StatsData {
  total: number
  active: number
  inactive: number
  topCities: { city: string; count: number }[]
}

export default function StakeholdersPage() {
  const [glnData, setGlnData] = useState<GlnRecord[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(0)
  const [rowsPerPage, setRowsPerPage] = useState(25)
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedCity, setSelectedCity] = useState('')
  const [selectedTown, setSelectedTown] = useState('')
  const [selectedActive, setSelectedActive] = useState<boolean | null>(null)
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [stats, setStats] = useState<StatsData | null>(null)
  const [townList, setTownList] = useState<{ town: string; count: number }[]>([])

  // GLN verilerini çek
  const fetchGlnData = async () => {
    setLoading(true)
    setError(null)
    try {
      const params = new URLSearchParams({
        page: String(page + 1),
        pageSize: String(rowsPerPage),
        ...(searchTerm && { search: searchTerm }),
        ...(selectedCity && { city: selectedCity }),
        ...(selectedTown && { town: selectedTown }),
        ...(selectedActive !== null && { active: String(selectedActive) })
      })

      const response = await fetch(`/api/opas/gln-registry?${params}`)
      if (!response.ok) throw new Error('Veri çekilemedi')

      const result = await response.json()
      if (result.success) {
        setGlnData(result.data)
        setTotalCount(result.totalCount)
        setTotalPages(result.totalPages)
      }
    } catch (err) {
      setError('GLN verileri yüklenirken hata oluştu')
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  // İstatistikleri çek
  const fetchStats = async () => {
    try {
      const response = await fetch('/api/opas/gln-registry/stats')
      if (!response.ok) throw new Error('İstatistikler çekilemedi')

      const result = await response.json()
      if (result.success) {
        setStats(result.stats)
      }
    } catch (err) {
      console.error('İstatistikler yüklenemedi:', err)
    }
  }

  useEffect(() => {
    fetchGlnData()
    fetchStats()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, rowsPerPage, searchTerm, selectedCity, selectedTown, selectedActive])

  const handleChangePage = (event: unknown, newPage: number) => {
    setPage(newPage)
  }

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10))
    setPage(0)
  }

  const handleSearch = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(event.target.value)
    setPage(0)
  }

  // İlçe listesini çek
  const fetchTowns = async (city: string) => {
    try {
      const response = await fetch(`/api/opas/gln-registry/towns/${encodeURIComponent(city)}`)
      if (!response.ok) throw new Error('İlçeler çekilemedi')

      const result = await response.json()
      if (result.success) {
        setTownList(result.data || [])
      }
    } catch (err) {
      console.error('İlçeler yüklenirken hata:', err)
      setTownList([])
    }
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const handleCityFilter = (event: any) => {
    const newCity = event.target.value
    setSelectedCity(newCity)
    setSelectedTown('') // Şehir değiştiğinde ilçeyi temizle
    setPage(0)
    
    // Şehir seçildiyse ilçeleri getir
    if (newCity) {
      fetchTowns(newCity)
    } else {
      setTownList([])
    }
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const handleTownFilter = (event: any) => {
    setSelectedTown(event.target.value)
    setPage(0)
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const handleActiveFilter = (event: any) => {
    const value = event.target.value
    setSelectedActive(value === '' ? null : value === 'true')
    setPage(0)
  }

  const clearFilters = () => {
    setSearchTerm('')
    setSelectedCity('')
    setSelectedTown('')
    setSelectedActive(null)
    setTownList([])
    setPage(0)
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Başlık ve İstatistikler */}
      <Box sx={{ mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
          <Button
            variant="outlined"
            startIcon={<ArrowBackIcon />}
            onClick={() => window.history.back()}
            sx={{ borderRadius: 2 }}
          >
            Geri
          </Button>
          <Button
            variant="outlined"
            startIcon={<HomeIcon />}
            href="/"
            sx={{ borderRadius: 2 }}
          >
            Ana Sayfa
          </Button>
        </Box>
        <Typography variant="h4" gutterBottom sx={{ fontWeight: 600, color: '#1976d2' }}>
          <StoreIcon sx={{ mr: 1, verticalAlign: 'bottom', fontSize: 32 }} />
          Eczaneler (GLN Kayıtları)
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Sistemde kayıtlı eczanelerin GLN listesi
        </Typography>
      </Box>

      {/* İstatistik Kartları */}
      {stats && (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(4, 1fr)' }, gap: 2, mb: 3 }}>
          <Box>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom variant="body2">
                  Toplam Eczane
                </Typography>
                <Typography variant="h4" sx={{ color: '#1976d2' }}>
                  {stats.total.toLocaleString('tr-TR')}
                </Typography>
              </CardContent>
            </Card>
          </Box>
          <Box>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom variant="body2">
                  Aktif
                </Typography>
                <Typography variant="h4" sx={{ color: '#4caf50' }}>
                  {stats.active.toLocaleString('tr-TR')}
                </Typography>
              </CardContent>
            </Card>
          </Box>
          <Box>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom variant="body2">
                  Pasif
                </Typography>
                <Typography variant="h4" sx={{ color: '#ff9800' }}>
                  {stats.inactive.toLocaleString('tr-TR')}
                </Typography>
              </CardContent>
            </Card>
          </Box>
          <Box>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom variant="body2">
                  En Çok GLN
                </Typography>
                <Typography variant="h6">
                  {stats.topCities[0]?.city || '-'}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  {stats.topCities[0]?.count.toLocaleString('tr-TR') || 0} kayıt
                </Typography>
              </CardContent>
            </Card>
          </Box>
        </Box>
      )}

      {/* Arama ve Filtreler */}
      <Paper sx={{ p: 2, mb: 3 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems="center">
          <TextField
            fullWidth
            variant="outlined"
            placeholder="GLN, eczane adı veya yetkili ara..."
            value={searchTerm}
            onChange={handleSearch}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon />
                </InputAdornment>
              ),
            }}
            sx={{ maxWidth: { sm: 400 } }}
          />
          
          <FormControl sx={{ minWidth: 150 }}>
            <InputLabel>Şehir</InputLabel>
            <Select
              value={selectedCity}
              onChange={handleCityFilter}
              label="Şehir"
            >
              <MenuItem value="">Tümü</MenuItem>
              {stats?.topCities.map(city => (
                <MenuItem key={city.city} value={city.city}>
                  {city.city} ({city.count})
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          
          <FormControl sx={{ minWidth: 120 }}>
            <InputLabel>İlçe</InputLabel>
            <Select
              value={selectedTown}
              onChange={handleTownFilter}
              label="İlçe"
              disabled={!selectedCity}
            >
              <MenuItem value="">Tümü</MenuItem>
              {townList.map(town => (
                <MenuItem key={town.town} value={town.town}>
                  {town.town} ({town.count})
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl sx={{ minWidth: 120 }}>
            <InputLabel>Durum</InputLabel>
            <Select
              value={selectedActive === null ? '' : String(selectedActive)}
              onChange={handleActiveFilter}
              label="Durum"
            >
              <MenuItem value="">Tümü</MenuItem>
              <MenuItem value="true">Aktif</MenuItem>
              <MenuItem value="false">Pasif</MenuItem>
            </Select>
          </FormControl>

          <Tooltip title="Yenile">
            <IconButton onClick={fetchGlnData} color="primary">
              <RefreshIcon />
            </IconButton>
          </Tooltip>

          <Tooltip title="Filtreleri Temizle">
            <IconButton onClick={clearFilters} color="secondary">
              <ClearIcon />
            </IconButton>
          </Tooltip>
        </Stack>
      </Paper>

      {/* Tablo */}
      <Paper sx={{ width: '100%', overflow: 'hidden' }}>
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
            <CircularProgress />
          </Box>
        ) : error ? (
          <Alert severity="error" sx={{ m: 2 }}>
            {error}
          </Alert>
        ) : (
          <>
            <TableContainer sx={{ maxHeight: 600 }}>
              <Table stickyHeader>
                <TableHead>
                  <TableRow>
                    <TableCell>GLN</TableCell>
                    <TableCell>Eczane Adı</TableCell>
                    <TableCell>Yetkili</TableCell>
                    <TableCell>Şehir</TableCell>
                    <TableCell>İlçe</TableCell>
                    <TableCell>İletişim</TableCell>
                    <TableCell align="center">Durum</TableCell>
                    <TableCell>Kaynak</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {glnData.map((row) => (
                    <TableRow key={row.id} hover>
                      <TableCell>
                        <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                          {row.gln}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Box>
                          <Typography variant="body2" sx={{ fontWeight: 500 }}>
                            {row.companyName || '-'}
                          </Typography>
                          {row.address && (
                            <Typography variant="caption" color="text.secondary">
                              {row.address}
                            </Typography>
                          )}
                        </Box>
                      </TableCell>
                      <TableCell>
                        {row.authorized ? (
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                            <PersonIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
                            <Typography variant="body2">{row.authorized}</Typography>
                          </Box>
                        ) : '-'}
                      </TableCell>
                      <TableCell>
                        {row.city ? (
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                            <CityIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
                            {row.city}
                          </Box>
                        ) : '-'}
                      </TableCell>
                      <TableCell>{row.town || '-'}</TableCell>
                      <TableCell>
                        <Stack spacing={0.5}>
                          {row.email && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                              <EmailIcon sx={{ fontSize: 14, color: 'text.secondary' }} />
                              <Typography variant="caption">{row.email}</Typography>
                            </Box>
                          )}
                          {row.phone && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                              <PhoneIcon sx={{ fontSize: 14, color: 'text.secondary' }} />
                              <Typography variant="caption">{row.phone}</Typography>
                            </Box>
                          )}
                        </Stack>
                      </TableCell>
                      <TableCell align="center">
                        <Chip
                          label={row.active ? 'Aktif' : 'Pasif'}
                          color={row.active ? 'success' : 'default'}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        <Typography variant="caption" color="text.secondary">
                          {row.source || 'ITS'}
                        </Typography>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
            <TablePagination
              rowsPerPageOptions={[10, 25, 50, 100]}
              component="div"
              count={totalCount}
              rowsPerPage={rowsPerPage}
              page={page}
              onPageChange={handleChangePage}
              onRowsPerPageChange={handleChangeRowsPerPage}
              labelRowsPerPage="Sayfa başına:"
              labelDisplayedRows={({ from, to, count }) =>
                `${from}-${to} / ${count !== -1 ? count : `${to}+`}`
              }
            />
          </>
        )}
      </Paper>

      {/* Alt Bilgi */}
      <Box sx={{ mt: 2, textAlign: 'center' }}>
        <Typography variant="caption" color="text.secondary">
          Toplam {totalCount.toLocaleString('tr-TR')} eczane • 
          Sayfa {page + 1} / {totalPages}
        </Typography>
      </Box>
    </Box>
  )
}
