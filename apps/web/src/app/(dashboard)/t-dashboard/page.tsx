'use client'

import { useState, useEffect } from 'react'
import '@/utils/consoleFilter' // LocalService console hatalarını filtrele
import { 
  Box, 
  Container, 
  Typography, 
  Card, 
  CardContent, 
  Paper,
  useTheme,
  useMediaQuery,
  LinearProgress,
  Alert,
  Chip,
  Stack
} from '@mui/material'
import { 
  Dashboard as DashboardIcon,
  TrendingUp,
  Inventory,
  Sync,
  Analytics,
  People,
  Receipt,
  LocalPharmacy,
  Speed,
  Security,
  CloudDone,
  CheckCircle,
  Warning,
  Info
} from '@mui/icons-material'
import TenantSidebar from '@/components/TenantSidebar'
import TenantNavbar from '@/components/TenantNavbar'
import StatusBar from '@/components/StatusBar'
import { ToastProvider } from '@/contexts/ToastContext'

// Dashboard Data Types
interface DashboardStats {
  totalProducts: number
  activeProducts: number
  totalCustomers: number
  todaySales: number
  monthlySales: number
  lowStockItems: number
  pendingPrescriptions: number
  lastSyncDate: string | null
}

interface RecentActivity {
  id: string
  type: 'sale' | 'prescription' | 'stock' | 'sync'
  title: string
  description: string
  timestamp: string
  status: 'success' | 'warning' | 'error' | 'info'
}

// interface QuickStats {
//   sales: {
//     today: number
//     thisWeek: number
//     thisMonth: number
//     growth: number
//   }
//   inventory: {
//     totalItems: number
//     lowStock: number
//     outOfStock: number
//     value: number
//   }
//   customers: {
//     total: number
//     newThisMonth: number
//     active: number
//   }
// }

// Stat Card Component
const StatCard = ({ 
  title, 
  value, 
  subtitle, 
  icon, 
  color, 
  trend, 
  trendValue 
}: {
  title: string
  value: string | number
  subtitle?: string
  icon: React.ReactNode
  color: string
  trend?: 'up' | 'down' | 'neutral'
  trendValue?: string
}) => (
  <Card sx={{ 
    height: '100%', 
    position: 'relative', 
    overflow: 'visible',
    background: 'linear-gradient(135deg, rgba(255,255,255,0.9) 0%, rgba(255,255,255,0.7) 100%)',
    backdropFilter: 'blur(10px)',
    border: '1px solid rgba(255,255,255,0.2)',
    boxShadow: '0 8px 32px rgba(0,0,0,0.1)',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    '&:hover': {
      transform: 'translateY(-4px)',
      boxShadow: '0 12px 40px rgba(0,0,0,0.15)',
    }
  }}>
    <CardContent sx={{ p: 3 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
        <Box>
          <Typography color="textSecondary" gutterBottom variant="overline" sx={{ fontWeight: 600 }}>
            {title}
          </Typography>
          <Typography variant="h4" sx={{ fontWeight: 700, mb: 1, color: 'text.primary' }}>
            {value}
          </Typography>
          {subtitle && (
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              {subtitle}
            </Typography>
          )}
          {trend && trendValue && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
              <TrendingUp 
                sx={{ 
                  fontSize: 16, 
                  color: trend === 'up' ? 'success.main' : trend === 'down' ? 'error.main' : 'text.secondary',
                  transform: trend === 'down' ? 'rotate(180deg)' : 'none'
                }} 
              />
              <Typography 
                variant="body2" 
                sx={{ 
                  color: trend === 'up' ? 'success.main' : trend === 'down' ? 'error.main' : 'text.secondary',
                  fontWeight: 600
                }}
              >
                {trendValue}
              </Typography>
            </Box>
          )}
        </Box>
        <Box
          sx={{
            p: 2,
            borderRadius: '50%',
            background: `linear-gradient(135deg, ${color} 0%, ${color}dd 100%)`,
            color: 'white',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: `0 4px 20px ${color}40`,
            width: 60,
            height: 60,
          }}
        >
          {icon}
        </Box>
      </Box>
    </CardContent>
  </Card>
)

// Activity Item Component
const ActivityItem = ({ activity }: { activity: RecentActivity }) => {
  const getStatusIcon = () => {
    switch (activity.status) {
      case 'success': return <CheckCircle sx={{ color: 'success.main', fontSize: 20 }} />
      case 'warning': return <Warning sx={{ color: 'warning.main', fontSize: 20 }} />
      case 'error': return <Warning sx={{ color: 'error.main', fontSize: 20 }} />
      default: return <Info sx={{ color: 'info.main', fontSize: 20 }} />
    }
  }

  const getTypeIcon = () => {
    switch (activity.type) {
      case 'sale': return <Receipt />
      case 'prescription': return <LocalPharmacy />
      case 'stock': return <Inventory />
      case 'sync': return <Sync />
      default: return <Info />
    }
  }

  return (
    <Box sx={{ 
      display: 'flex', 
      alignItems: 'center', 
      gap: 2, 
      p: 2, 
      borderRadius: 2,
      background: 'rgba(255,255,255,0.5)',
      backdropFilter: 'blur(10px)',
      border: '1px solid rgba(255,255,255,0.2)',
      transition: 'all 0.2s ease',
      '&:hover': {
        background: 'rgba(255,255,255,0.8)',
        transform: 'translateX(4px)',
      }
    }}>
      <Box sx={{ 
        p: 1, 
        borderRadius: '50%', 
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        color: 'white',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center'
      }}>
        {getTypeIcon()}
      </Box>
      <Box sx={{ flex: 1 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 0.5 }}>
          {activity.title}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {activity.description}
        </Typography>
      </Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        {getStatusIcon()}
        <Typography variant="caption" color="text.secondary">
          {new Date(activity.timestamp).toLocaleTimeString('tr-TR', { 
            hour: '2-digit', 
            minute: '2-digit' 
          })}
        </Typography>
      </Box>
    </Box>
  )
}

export default function TenantDashboard() {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))
  
  // State
  const [sidebarOpen, setSidebarOpen] = useState(!isMobile)
  const [navbarOpen, setNavbarOpen] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [dashboardStats, setDashboardStats] = useState<DashboardStats | null>(null)
  const [recentActivity, setRecentActivity] = useState<RecentActivity[]>([])
  // const [quickStats, setQuickStats] = useState<QuickStats | null>(null)

  // Mock data - will be replaced with real API calls
  useEffect(() => {
    const loadDashboardData = async () => {
      try {
        setLoading(true)
        
        // Simulate API call delay
        await new Promise(resolve => setTimeout(resolve, 1000))
        
        // Mock dashboard stats
        setDashboardStats({
          totalProducts: 1247,
          activeProducts: 1189,
          totalCustomers: 342,
          todaySales: 2847.50,
          monthlySales: 45678.90,
          lowStockItems: 23,
          pendingPrescriptions: 8,
          lastSyncDate: new Date().toISOString()
        })

        // Mock recent activity
        setRecentActivity([
          {
            id: '1',
            type: 'sale',
            title: 'Yeni Satış',
            description: 'Paracetamol 500mg - 2 kutu satıldı',
            timestamp: new Date(Date.now() - 5 * 60000).toISOString(),
            status: 'success'
          },
          {
            id: '2',
            type: 'prescription',
            title: 'Reçete İşlendi',
            description: 'Dr. Ahmet Yılmaz - Antibiyotik reçetesi',
            timestamp: new Date(Date.now() - 15 * 60000).toISOString(),
            status: 'success'
          },
          {
            id: '3',
            type: 'stock',
            title: 'Stok Uyarısı',
            description: 'Aspirin 100mg stok seviyesi düşük',
            timestamp: new Date(Date.now() - 30 * 60000).toISOString(),
            status: 'warning'
          },
          {
            id: '4',
            type: 'sync',
            title: 'Veri Senkronizasyonu',
            description: 'ITS ürün listesi güncellendi',
            timestamp: new Date(Date.now() - 60 * 60000).toISOString(),
            status: 'success'
          }
        ])

        // Mock quick stats - commented out for now
        // setQuickStats({
        //   sales: {
        //     today: 2847.50,
        //     thisWeek: 18945.30,
        //     thisMonth: 45678.90,
        //     growth: 12.5
        //   },
        //   inventory: {
        //     totalItems: 1247,
        //     lowStock: 23,
        //     outOfStock: 5,
        //     value: 125000
        //   },
        //   customers: {
        //     total: 342,
        //     newThisMonth: 28,
        //     active: 298
        //   }
        // })

      } catch (err) {
        console.error('Dashboard data load error:', err)
        setError('Dashboard verileri yüklenirken hata oluştu')
      } finally {
        setLoading(false)
      }
    }

    loadDashboardData()
  }, [])

  // Handle sidebar toggle
  const handleSidebarToggle = () => {
    setSidebarOpen(!sidebarOpen)
  }

  // Handle navbar toggle
  const handleNavbarToggle = () => {
    setNavbarOpen(!navbarOpen)
  }

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh' }}>
        <Box sx={{ textAlign: 'center' }}>
          <LinearProgress sx={{ width: 200, mb: 2 }} />
          <Typography>Dashboard yükleniyor...</Typography>
        </Box>
      </Box>
    )
  }

  return (
    <ToastProvider>
      <Box sx={{ display: 'flex', minHeight: '100vh', backgroundColor: '#f8fafc' }}>
        {/* Tenant Sidebar */}
        <TenantSidebar 
          open={sidebarOpen} 
          onToggle={handleSidebarToggle}
          currentPath="/t-dashboard"
        />
      
      {/* Main Content */}
      <Box 
        component="main" 
        sx={{ 
          flexGrow: 1,
          minHeight: '100vh',
          position: 'relative',
          transition: 'margin 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
          marginLeft: { 
            xs: 0, // Mobile'da margin yok (overlay)
            md: sidebarOpen ? '280px' : 0 // Desktop'ta sidebar genişliği kadar margin
          },
          paddingBottom: '60px' // Status bar için alan bırak
        }}
      >
        {/* Tenant Navbar */}
        <TenantNavbar 
          open={navbarOpen}
          onToggle={handleNavbarToggle}
          onSidebarToggle={handleSidebarToggle}
        />

        {/* Dashboard Content */}
        <Container maxWidth="xl" sx={{ py: 3, px: 3 }}>
          {/* Header */}
          <Box sx={{ mb: 4 }}>
            <Typography variant="h4" sx={{ fontWeight: 700, mb: 1, display: 'flex', alignItems: 'center' }}>
              <DashboardIcon sx={{ mr: 2, color: 'primary.main' }} />
              Eczane Dashboard
            </Typography>
            <Typography variant="subtitle1" color="textSecondary">
              Hoş geldiniz! Eczanenizin güncel durumunu buradan takip edebilirsiniz.
            </Typography>
          </Box>

          {/* Error Alert */}
          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          {/* Quick Stats */}
          {dashboardStats && (
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(4, 1fr)' }, gap: 3, mb: 4 }}>
              <Box>
                <StatCard 
                  title="Toplam Ürün"
                  value={dashboardStats.totalProducts.toLocaleString('tr-TR')}
                  subtitle={`${dashboardStats.activeProducts} aktif`}
                  icon={<Inventory />}
                  color="#667eea"
                  trend="up"
                  trendValue="+5.2%"
                />
              </Box>
              <Box>
                <StatCard 
                  title="Bugünkü Satış"
                  value={`₺${dashboardStats.todaySales.toLocaleString('tr-TR')}`}
                  subtitle="Günlük ciro"
                  icon={<TrendingUp />}
                  color="#f093fb"
                  trend="up"
                  trendValue="+12.5%"
                />
              </Box>
              <Box>
                <StatCard 
                  title="Müşteri Sayısı"
                  value={dashboardStats.totalCustomers.toLocaleString('tr-TR')}
                  subtitle="Kayıtlı müşteri"
                  icon={<People />}
                  color="#4facfe"
                  trend="up"
                  trendValue="+8.1%"
                />
              </Box>
              <Box>
                <StatCard 
                  title="Düşük Stok"
                  value={dashboardStats.lowStockItems}
                  subtitle="Ürün uyarısı"
                  icon={<Warning />}
                  color="#fa709a"
                  trend="down"
                  trendValue="-3.2%"
                />
              </Box>
            </Box>
          )}

          {/* Main Content Grid */}
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '2fr 1fr' }, gap: 3 }}>
            {/* Recent Activity */}
            <Box>
              <Paper sx={{ 
                p: 3, 
                borderRadius: 3,
                background: 'linear-gradient(135deg, rgba(255,255,255,0.9) 0%, rgba(255,255,255,0.7) 100%)',
                backdropFilter: 'blur(10px)',
                border: '1px solid rgba(255,255,255,0.2)',
                boxShadow: '0 8px 32px rgba(0,0,0,0.1)'
              }}>
                <Typography variant="h6" sx={{ mb: 3, fontWeight: 600, display: 'flex', alignItems: 'center' }}>
                  <Speed sx={{ mr: 1, color: 'primary.main' }} />
                  Son Aktiviteler
                </Typography>
                <Stack spacing={1}>
                  {recentActivity.map((activity) => (
                    <ActivityItem key={activity.id} activity={activity} />
                  ))}
                </Stack>
              </Paper>
            </Box>

            {/* Quick Actions & Status */}
            <Box>
              <Stack spacing={3}>
                {/* System Status */}
                <Paper sx={{ 
                  p: 3, 
                  borderRadius: 3,
                  background: 'linear-gradient(135deg, rgba(255,255,255,0.9) 0%, rgba(255,255,255,0.7) 100%)',
                  backdropFilter: 'blur(10px)',
                  border: '1px solid rgba(255,255,255,0.2)',
                  boxShadow: '0 8px 32px rgba(0,0,0,0.1)'
                }}>
                  <Typography variant="h6" sx={{ mb: 3, fontWeight: 600, display: 'flex', alignItems: 'center' }}>
                    <Security sx={{ mr: 1, color: 'primary.main' }} />
                    Sistem Durumu
                  </Typography>
                  <Stack spacing={2}>
                    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                      <Typography variant="body2">ITS Bağlantısı</Typography>
                      <Chip label="Aktif" color="success" size="small" icon={<CheckCircle />} />
                    </Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                      <Typography variant="body2">Veri Senkronizasyonu</Typography>
                      <Chip label="Güncel" color="success" size="small" icon={<CloudDone />} />
                    </Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                      <Typography variant="body2">Sistem Performansı</Typography>
                      <Chip label="Mükemmel" color="success" size="small" icon={<Speed />} />
                    </Box>
                  </Stack>
                </Paper>

                {/* Quick Actions */}
                <Paper sx={{ 
                  p: 3, 
                  borderRadius: 3,
                  background: 'linear-gradient(135deg, rgba(255,255,255,0.9) 0%, rgba(255,255,255,0.7) 100%)',
                  backdropFilter: 'blur(10px)',
                  border: '1px solid rgba(255,255,255,0.2)',
                  boxShadow: '0 8px 32px rgba(0,0,0,0.1)'
                }}>
                  <Typography variant="h6" sx={{ mb: 3, fontWeight: 600, display: 'flex', alignItems: 'center' }}>
                    <Analytics sx={{ mr: 1, color: 'primary.main' }} />
                    Hızlı İşlemler
                  </Typography>
                  <Stack spacing={2}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, p: 2, borderRadius: 2, background: 'rgba(102, 126, 234, 0.1)', cursor: 'pointer', transition: 'all 0.2s ease', '&:hover': { background: 'rgba(102, 126, 234, 0.2)' } }}>
                      <Receipt sx={{ color: 'primary.main' }} />
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>Yeni Satış</Typography>
                    </Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, p: 2, borderRadius: 2, background: 'rgba(240, 147, 251, 0.1)', cursor: 'pointer', transition: 'all 0.2s ease', '&:hover': { background: 'rgba(240, 147, 251, 0.2)' } }}>
                      <LocalPharmacy sx={{ color: 'secondary.main' }} />
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>Reçete İşle</Typography>
                    </Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, p: 2, borderRadius: 2, background: 'rgba(79, 172, 254, 0.1)', cursor: 'pointer', transition: 'all 0.2s ease', '&:hover': { background: 'rgba(79, 172, 254, 0.2)' } }}>
                      <Inventory sx={{ color: 'info.main' }} />
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>Stok Kontrol</Typography>
                    </Box>
                  </Stack>
                </Paper>
              </Stack>
            </Box>
          </Box>
        </Container>
      </Box>
      
        {/* Status Bar */}
        <StatusBar sidebarOpen={sidebarOpen} />
      </Box>
    </ToastProvider>
  )
}
