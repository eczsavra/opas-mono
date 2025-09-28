'use client'

import { 
  Container, 
  Typography, 
  Card, 
  CardContent, 
  Box, 
  Button,
  Paper,
  Avatar,
  useTheme,
  useMediaQuery,
  LinearProgress,
  Alert
} from '@mui/material'
import { 
  TrendingUp,
  Inventory,
  Store,
  Sync,
  LocationOn,
  Dashboard as DashboardIcon,
  Analytics
} from '@mui/icons-material'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useAuth } from '@/contexts/AuthContext'
import ModernSidebar from '@/components/ModernSidebar'
import TopNavbar from '@/components/TopNavbar'
import RoleSwitcher from '@/components/RoleSwitcher'
import { useState, useEffect } from 'react'

const StatCard = ({ title, value, icon, color, change, subtitle }: {
  title: string
  value: string | number
  icon: React.ReactNode
  color: string
  change?: string
  subtitle?: string
}) => (
  <Card sx={{ height: '100%', position: 'relative', overflow: 'visible' }}>
    <CardContent>
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Box>
          <Typography color="textSecondary" gutterBottom variant="overline">
            {title}
          </Typography>
          <Typography variant="h4" sx={{ fontWeight: 700, mb: 1 }}>
            {value}
          </Typography>
          {subtitle && (
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              {subtitle}
            </Typography>
          )}
          {change && (
            <Typography variant="body2" sx={{ color: 'success.main', display: 'flex', alignItems: 'center' }}>
              <TrendingUp sx={{ fontSize: 16, mr: 0.5 }} />
              {change}
            </Typography>
          )}
        </Box>
        <Avatar
          sx={{
            bgcolor: color,
            width: 60,
            height: 60,
            position: 'absolute',
            top: -10,
            right: 20,
            boxShadow: 3
          }}
        >
          {icon}
        </Avatar>
      </Box>
    </CardContent>
  </Card>
)

interface DashboardData {
  products: {
    total: number
    active: number
    passive: number
    activePercentage: number
  }
  glns: {
    total: number
  }
  sync: {
    lastSyncDate: string | null
    recentSyncs: Array<{ date: string; count: number }>
  }
  distribution: {
    cities: Array<{ city: string; count: number }>
  }
}

interface TenantData {
  total: number
  active: number
  inactive: number
  cityDistribution: Array<{ city: string; count: number }>
}

export default function Dashboard() {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))
  const { user, isAuthenticated, isLoading } = useAuth()
  const router = useRouter()

  // SuperAdmin Dashboard State
  const [dashboardData, setDashboardData] = useState<DashboardData | null>(null)
  const [tenantData, setTenantData] = useState<TenantData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Fetch SuperAdmin Dashboard Data
  useEffect(() => {
    const fetchDashboardData = async () => {
      if (!isAuthenticated || user?.role !== 'superadmin') return

      try {
        setLoading(true)
        setError(null)

        // Fetch dashboard analytics
        const dashboardResponse = await fetch('/api/opas/superadmin/analytics/dashboard')
        if (!dashboardResponse.ok) {
          throw new Error('Dashboard data fetch failed')
        }
        const dashboardResult = await dashboardResponse.json()
        if (dashboardResult.success) {
          setDashboardData(dashboardResult.data)
        }

        // Fetch tenant analytics
        const tenantResponse = await fetch('/api/opas/superadmin/analytics/tenants')
        if (!tenantResponse.ok) {
          throw new Error('Tenant data fetch failed')
        }
        const tenantResult = await tenantResponse.json()
        if (tenantResult.success) {
          setTenantData(tenantResult.data)
        }

      } catch (err) {
        console.error('Dashboard fetch error:', err)
        setError(err instanceof Error ? err.message : 'Veri yüklenirken hata oluştu')
      } finally {
        setLoading(false)
      }
    }

    fetchDashboardData()
  }, [isAuthenticated, user?.role])

  // Redirect to login if not authenticated
  if (!isLoading && !isAuthenticated) {
    router.push('/login')
    return null
  }

  // Show loading state
  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh' }}>
        <Box sx={{ textAlign: 'center' }}>
          <LinearProgress sx={{ width: 200, mb: 2 }} />
          <Typography>Yükleniyor...</Typography>
        </Box>
      </Box>
    )
  }

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      {/* Modern Sidebar */}
      {!isMobile && <ModernSidebar currentPath="/" />}
      
      {/* Main Content */}
      <Box 
        component="main" 
        sx={{ 
          flexGrow: 1,
          backgroundColor: theme.palette.background.default,
          minHeight: '100vh',
          position: 'relative'
        }}
      >
        {/* Role Switcher */}
        <RoleSwitcher />
        
        {/* Modern Top Navbar */}
        <TopNavbar 
          sidebarCollapsed={false}
          userName={user?.fullName || 'User'}
          username={user?.username || 'user'}
        />

        <Container maxWidth="xl" sx={{ py: 3, px: 3 }}>
          {/* SuperAdmin Dashboard Header */}
          <Box sx={{ mb: 4 }}>
            <Typography variant="h4" sx={{ fontWeight: 700, mb: 1, display: 'flex', alignItems: 'center' }}>
              <DashboardIcon sx={{ mr: 2, color: 'primary.main' }} />
              SuperAdmin Dashboard
            </Typography>
            <Typography variant="subtitle1" color="textSecondary">
              Sistem geneli analytics ve yönetim merkezi
            </Typography>
          </Box>

          {/* Error Alert */}
          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          {/* Loading State */}
          {loading && (
            <Box sx={{ mb: 3 }}>
              <LinearProgress />
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1, textAlign: 'center' }}>
                Dashboard verileri yükleniyor...
              </Typography>
            </Box>
          )}

          {/* System Overview Cards */}
          {dashboardData && (
            <Box sx={{ 
              display: 'grid', 
              gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', md: '1fr 1fr 1fr 1fr' },
              gap: 3,
              mb: 4 
            }}>
              <StatCard 
                title="Toplam Ürün"
                value={dashboardData.products.total.toLocaleString('tr-TR')}
                subtitle={`${dashboardData.products.active} aktif, ${dashboardData.products.passive} pasif`}
                icon={<Inventory />}
                color="primary.main"
              />
              <StatCard 
                title="Aktif Ürün Oranı"
                value={`%${dashboardData.products.activePercentage}`}
                subtitle={`${dashboardData.products.active} / ${dashboardData.products.total}`}
                icon={<TrendingUp />}
                color="success.main"
              />
              <StatCard 
                title="Toplam GLN"
                value={dashboardData.glns.total.toLocaleString('tr-TR')}
                subtitle="Kayıtlı paydaş sayısı"
                icon={<Store />}
                color="info.main"
              />
              <StatCard 
                title="Son Sync"
                value={dashboardData.sync.lastSyncDate ? 
                  new Date(dashboardData.sync.lastSyncDate).toLocaleDateString('tr-TR') : 
                  'Bilinmiyor'
                }
                subtitle="ITS senkronizasyonu"
                icon={<Sync />}
                color="warning.main"
              />
            </Box>
          )}

          {/* Tenant Overview Cards */}
          {tenantData && (
            <Box sx={{ 
              display: 'grid', 
              gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', md: '1fr 1fr 1fr 1fr' },
              gap: 3,
              mb: 4 
            }}>
              <StatCard 
                title="Toplam Tenant"
                value={tenantData.total.toLocaleString('tr-TR')}
                subtitle="Kayıtlı eczane sayısı"
                icon={<Store />}
                color="primary.main"
              />
              <StatCard 
                title="Aktif Tenant"
                value={tenantData.active.toLocaleString('tr-TR')}
                subtitle={`${tenantData.inactive} pasif`}
                icon={<TrendingUp />}
                color="success.main"
              />
              <StatCard 
                title="Aktif Oranı"
                value={`%${tenantData.total > 0 ? Math.round((tenantData.active / tenantData.total) * 100) : 0}`}
                subtitle="Son 30 gün içinde sync olan"
                icon={<Analytics />}
                color="info.main"
              />
              <StatCard 
                title="Şehir Sayısı"
                value={tenantData.cityDistribution.length}
                subtitle="Farklı şehir sayısı"
                icon={<LocationOn />}
                color="warning.main"
              />
            </Box>
          )}

          {/* City Distribution */}
          {tenantData && tenantData.cityDistribution.length > 0 && (
            <Paper sx={{ p: 3, mb: 4 }}>
              <Typography variant="h6" sx={{ mb: 3, fontWeight: 600, display: 'flex', alignItems: 'center' }}>
                <LocationOn sx={{ mr: 1 }} />
                Şehir Bazında Dağılım
              </Typography>
              <Box sx={{ 
                display: 'grid', 
                gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', md: '1fr 1fr 1fr 1fr' },
                gap: 2 
              }}>
                {tenantData.cityDistribution.slice(0, 8).map((city) => (
                  <Box 
                    key={city.city}
                    sx={{ 
                      p: 2, 
                      border: 1, 
                      borderColor: 'divider', 
                      borderRadius: 2,
                      textAlign: 'center'
                    }}
                  >
                    <Typography variant="h6" sx={{ fontWeight: 600 }}>
                      {city.count}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {city.city}
                    </Typography>
                  </Box>
                ))}
              </Box>
            </Paper>
          )}

          {/* Quick Actions */}
          <Paper sx={{ p: 3, mb: 4 }}>
            <Typography variant="h6" sx={{ mb: 3, fontWeight: 600 }}>
              Hızlı Erişim
            </Typography>
            <Box sx={{ 
              display: 'grid', 
              gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', md: '1fr 1fr 1fr 1fr' },
              gap: 2 
            }}>
              <Button 
                component={Link}
                href="/products"
                variant="outlined" 
                fullWidth 
                size="large"
                startIcon={<Inventory />}
                sx={{ py: 1.5 }}
              >
                Ürün Yönetimi
              </Button>
              <Button 
                component={Link}
                href="/stakeholders"
                variant="outlined" 
                fullWidth 
                size="large"
                startIcon={<Store />}
                sx={{ py: 1.5 }}
              >
                Paydaş Yönetimi
              </Button>
              <Button 
                component={Link}
                href="/logs"
                variant="outlined" 
                fullWidth 
                size="large"
                startIcon={<Analytics />}
                sx={{ py: 1.5 }}
              >
                Sistem Logları
              </Button>
              <Button 
                component={Link}
                href="/flags"
                variant="contained" 
                fullWidth 
                size="large"
                startIcon={<DashboardIcon />}
                sx={{ py: 1.5 }}
              >
                Sistem Ayarları
              </Button>
            </Box>
          </Paper>
        </Container>
      </Box>
    </Box>
  )
}