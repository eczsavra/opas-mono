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
  useMediaQuery
} from '@mui/material'
import { 
  TrendingUp,
  People,
  Inventory,
  Receipt,
  MedicalServices
} from '@mui/icons-material'
import Link from 'next/link'
import ModernSidebar from '@/components/ModernSidebar'
import TopNavbar from '@/components/TopNavbar'

const StatCard = ({ title, value, icon, color, change }: {
  title: string
  value: string | number
  icon: React.ReactNode
  color: string
  change?: string
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

export default function Dashboard() {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))

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
        {/* Modern Top Navbar */}
        <TopNavbar 
          sidebarCollapsed={false}
          userName="Muhammed E. Arvas"
          username="eczsavra"
        />

        <Container maxWidth="xl" sx={{ py: 3, px: 3 }}>
      {/* Welcome Message - Only for Mobile */}
      {isMobile && (
        <Box sx={{ mb: 4 }}>
          <Typography variant="h5" sx={{ fontWeight: 600, mb: 1 }}>
            Modern Eczane Yönetim Sistemi - Hoş geldiniz! 👋
          </Typography>
          <Typography variant="subtitle1" color="textSecondary">
            Günlük işlemlerinizi buradan yönetebilirsiniz
          </Typography>
        </Box>
      )}

      {/* Stats Grid */}
      <Box sx={{ 
        display: 'grid', 
        gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', md: '1fr 1fr 1fr 1fr' },
        gap: 3,
        mb: 4 
      }}>
        <StatCard 
          title="Günlük Satış"
          value="₺12,450"
          icon={<Receipt />}
          color="primary.main"
          change="+12.5%"
        />
        <StatCard 
          title="Müşteri Sayısı"
          value="1,247"
          icon={<People />}
          color="success.main"
          change="+8.2%"
        />
        <StatCard 
          title="Stok Kalemleri"
          value="3,892"
          icon={<Inventory />}
          color="warning.main"
          change="+5.1%"
        />
        <StatCard 
          title="Reçete Sayısı"
          value="187"
          icon={<MedicalServices />}
          color="info.main"
          change="+15.3%"
        />
      </Box>

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
            href="/sales"
            variant="outlined" 
            fullWidth 
            size="large"
            startIcon={<Receipt />}
            sx={{ py: 1.5 }}
          >
            Yeni Satış
          </Button>
          <Button 
            component={Link}
            href="/customers"
            variant="outlined" 
            fullWidth 
            size="large"
            startIcon={<People />}
            sx={{ py: 1.5 }}
          >
            Müşteri Ekle
          </Button>
          <Button 
            component={Link}
            href="/inventory"
            variant="outlined" 
            fullWidth 
            size="large"
            startIcon={<Inventory />}
            sx={{ py: 1.5 }}
          >
            Stok Kontrol
          </Button>
          <Button 
            component={Link}
            href="/login2"
            variant="contained" 
            fullWidth 
            size="large"
            startIcon={<MedicalServices />}
            sx={{ py: 1.5 }}
          >
            Modern Login
          </Button>
        </Box>
      </Paper>

      {/* Test Links */}
      <Paper sx={{ p: 3 }}>
        <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
          Test Sayfaları
        </Typography>
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
          <Button component={Link} href="/ping" variant="outlined">API Ping Test</Button>
          <Button component={Link} href="/flags" variant="outlined">Bayraklar</Button>
          <Button component={Link} href="/login2" variant="contained" color="secondary">
            🎨 Ultra Modern Login
          </Button>
        </Box>
      </Paper>
        </Container>
      </Box>
    </Box>
  )
}