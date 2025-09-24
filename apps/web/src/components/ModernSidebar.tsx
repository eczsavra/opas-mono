'use client'

import { useState, useEffect } from 'react'
import {
  Box,
  Drawer,
  List,
  ListItemIcon,
  ListItemText,
  ListItemButton,
  IconButton,
  Typography,
  Divider,
  Tooltip,
  Avatar,
  Badge,
  alpha
} from '@mui/material'
import {
  Dashboard as DashboardIcon,
  ShoppingCart as SalesIcon,
  People as PeopleIcon,
  Inventory as InventoryIcon,
  Receipt as ReceiptIcon,
  MedicalServices as PrescriptionIcon,
  Analytics as AnalyticsIcon,
  MenuOpen as MenuOpenIcon,
  Menu as MenuIcon,
  PushPin as PinIcon,
  PushPinOutlined as UnpinIcon,
  Store as PharmacyIcon,
  LocalPharmacy as MedicineIcon
} from '@mui/icons-material'
import { styled, useTheme } from '@mui/material/styles'
import Link from 'next/link'

const SIDEBAR_WIDTH = 280
const SIDEBAR_WIDTH_COLLAPSED = 64

// Styled Components
const StyledDrawer = styled(Drawer, {
  shouldForwardProp: (prop) => prop !== 'collapsed',
})<{ collapsed: boolean }>(({ theme, collapsed }) => ({
  width: collapsed ? SIDEBAR_WIDTH_COLLAPSED : SIDEBAR_WIDTH,
  flexShrink: 0,
  whiteSpace: 'nowrap',
  boxSizing: 'border-box',
  '& .MuiDrawer-paper': {
    width: collapsed ? SIDEBAR_WIDTH_COLLAPSED : SIDEBAR_WIDTH,
    backgroundColor: theme.palette.mode === 'dark' ? '#1e293b' : '#ffffff',
    borderRight: `1px solid ${alpha(theme.palette.divider, 0.12)}`,
    transition: theme.transitions.create(['width'], {
      easing: theme.transitions.easing.easeInOut,
      duration: theme.transitions.duration.standard,
    }),
    overflowX: 'hidden',
    overflowY: 'auto',
    boxShadow: '4px 0 20px rgba(0,0,0,0.08)',
    backdropFilter: 'blur(10px)',
    height: '100vh',
    position: 'fixed',
    top: 0,
    left: 0,
    // Modern scroll styling - gizli scrollbar
    '&::-webkit-scrollbar': {
      width: '4px',
    },
    '&::-webkit-scrollbar-track': {
      background: 'transparent',
    },
    '&::-webkit-scrollbar-thumb': {
      background: alpha(theme.palette.primary.main, 0.2),
      borderRadius: '4px',
      transition: 'background 0.2s ease',
    },
    '&::-webkit-scrollbar-thumb:hover': {
      background: alpha(theme.palette.primary.main, 0.4),
    },
    // Firefox için modern scroll
    scrollbarWidth: 'thin',
    scrollbarColor: `${alpha(theme.palette.primary.main, 0.2)} transparent`,
  },
}))

const ModernListItem = styled(ListItemButton)(({ theme }) => ({
  margin: '4px 8px',
  borderRadius: '12px',
  transition: 'all 0.2s ease-in-out',
  '&:hover': {
    backgroundColor: alpha(theme.palette.primary.main, 0.08),
    transform: 'translateX(4px)',
  },
  '&.Mui-selected': {
    backgroundColor: alpha(theme.palette.primary.main, 0.12),
    borderLeft: `3px solid ${theme.palette.primary.main}`,
    '&:hover': {
      backgroundColor: alpha(theme.palette.primary.main, 0.16),
    },
  },
}))

const HeaderSection = styled(Box)(({ theme }) => ({
  padding: theme.spacing(2),
  background: `linear-gradient(135deg, ${theme.palette.primary.main} 0%, ${theme.palette.primary.dark} 100%)`,
  color: 'white',
  position: 'relative',
  overflow: 'hidden',
  '&::before': {
    content: '""',
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    background: 'url("data:image/svg+xml,%3Csvg xmlns=\'http://www.w3.org/2000/svg\' width=\'60\' height=\'60\' viewBox=\'0 0 60 60\'%3E%3Cg fill-rule=\'evenodd\'%3E%3Cg fill=\'%23ffffff\' fill-opacity=\'0.05\'%3E%3Cpath d=\'M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z\'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")',
  },
}))

// Navigation Items
const navigationItems = [
  { id: 'dashboard', text: 'Dashboard', icon: <DashboardIcon />, path: '/', badge: null },
  { id: 'sales', text: 'Satış İşlemleri', icon: <SalesIcon />, path: '/sales', badge: null },
  { id: 'prescriptions', text: 'Reçete Yönetimi', icon: <PrescriptionIcon />, path: '/prescriptions', badge: 5 },
  { id: 'inventory', text: 'Stok Yönetimi', icon: <InventoryIcon />, path: '/inventory', badge: null },
  { id: 'customers', text: 'Müşteri Yönetimi', icon: <PeopleIcon />, path: '/customers', badge: null },
  { id: 'reports', text: 'Raporlar', icon: <ReceiptIcon />, path: '/reports', badge: null },
  { id: 'analytics', text: 'Analitik', icon: <AnalyticsIcon />, path: '/analytics', badge: 'NEW' },
  { id: 'pharmacy', text: 'Eczane Bilgileri', icon: <PharmacyIcon />, path: '/pharmacy', badge: null },
  { id: 'medicines', text: 'İlaç Katalogu', icon: <MedicineIcon />, path: '/medicines', badge: null },
]

const bottomNavigationItems: Array<{
  id: string
  text: string
  icon: React.ReactNode
  path: string
  badge: number | string | null
}> = [
  // Removed: Bildirimler, Profil, Ayarlar (now in TopNavbar)
]

interface ModernSidebarProps {
  currentPath?: string
}

export default function ModernSidebar({ currentPath = '/' }: ModernSidebarProps) {
  const theme = useTheme()
  const [collapsed, setCollapsed] = useState(false)
  const [pinned, setPinned] = useState(false)
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    setMounted(true)
    // LocalStorage'dan ayarları yükle
    const savedCollapsed = localStorage.getItem('sidebar-collapsed') === 'true'
    const savedPinned = localStorage.getItem('sidebar-pinned') === 'true'
    setCollapsed(savedCollapsed)
    setPinned(savedPinned)
  }, [])

  useEffect(() => {
    if (mounted) {
      localStorage.setItem('sidebar-collapsed', collapsed.toString())
    }
  }, [collapsed, mounted])

  useEffect(() => {
    if (mounted) {
      localStorage.setItem('sidebar-pinned', pinned.toString())
    }
  }, [pinned, mounted])

  const handleToggleCollapse = () => {
    setCollapsed(!collapsed)
  }

  const handleTogglePin = () => {
    setPinned(!pinned)
  }

  if (!mounted) return null

  return (
    <StyledDrawer 
      variant="permanent"
      collapsed={collapsed}
    >
      {/* Header Section */}
      <HeaderSection>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          {!collapsed && (
            <Box sx={{ display: 'flex', alignItems: 'center' }}>
              <Avatar
                sx={{
                  width: 40,
                  height: 40,
                  bgcolor: 'rgba(255,255,255,0.2)',
                  mr: 2
                }}
              >
                <PharmacyIcon />
              </Avatar>
              <Box>
                <Typography variant="h6" sx={{ fontWeight: 700, fontSize: '1.1rem' }}>
                  OPAS
                </Typography>
                <Typography variant="caption" sx={{ opacity: 0.9, fontSize: '0.75rem' }}>
                  Eczane Yönetimi
                </Typography>
              </Box>
            </Box>
          )}
          
          <Box sx={{ display: 'flex', alignItems: 'center' }}>
            {!collapsed && (
              <Tooltip title={pinned ? "Sabitlemeyi Kaldır" : "Sabit Tut"}>
                <IconButton
                  size="small"
                  onClick={handleTogglePin}
                  sx={{ color: 'white', mr: 1 }}
                >
                  {pinned ? <PinIcon fontSize="small" /> : <UnpinIcon fontSize="small" />}
                </IconButton>
              </Tooltip>
            )}
            
            <Tooltip title={collapsed ? "Genişlet" : "Daralt"}>
              <IconButton
                size="small"
                onClick={handleToggleCollapse}
                sx={{ color: 'white' }}
              >
                {collapsed ? <MenuIcon /> : <MenuOpenIcon />}
              </IconButton>
            </Tooltip>
          </Box>
        </Box>

        {/* Collapsed Header */}
        {collapsed && (
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 1 }}>
            <Avatar sx={{ width: 32, height: 32, bgcolor: 'rgba(255,255,255,0.2)' }}>
              <PharmacyIcon fontSize="small" />
            </Avatar>
          </Box>
        )}
      </HeaderSection>

      {/* Main Navigation */}
      <Box 
        sx={{ 
          flex: 1, 
          py: 1, 
          display: 'flex', 
          flexDirection: 'column', 
          height: 'calc(100vh - 120px)',
          overflowY: 'auto',
          overflowX: 'hidden',
          // Modern scroll styling
          '&::-webkit-scrollbar': {
            width: '3px',
          },
          '&::-webkit-scrollbar-track': {
            background: 'transparent',
          },
          '&::-webkit-scrollbar-thumb': {
            background: alpha(theme.palette.primary.main, 0.1),
            borderRadius: '3px',
            transition: 'all 0.2s ease',
          },
          '&::-webkit-scrollbar-thumb:hover': {
            background: alpha(theme.palette.primary.main, 0.3),
          },
          // Firefox için
          scrollbarWidth: 'thin',
          scrollbarColor: `${alpha(theme.palette.primary.main, 0.1)} transparent`,
        }}
      >
        <List>
          {navigationItems.map((item) => (
            <Link key={item.id} href={item.path} style={{ textDecoration: 'none', color: 'inherit' }}>
              <ModernListItem
                selected={currentPath === item.path}
                sx={{ minHeight: 48 }}
              >
              <ListItemIcon sx={{ minWidth: collapsed ? 'auto' : 40, mr: collapsed ? 0 : 1 }}>
                {item.badge ? (
                  <Badge
                    badgeContent={item.badge}
                    color={typeof item.badge === 'string' ? 'secondary' : 'error'}
                    max={99}
                  >
                    {item.icon}
                  </Badge>
                ) : (
                  item.icon
                )}
              </ListItemIcon>
              {!collapsed && (
                <ListItemText
                  primary={item.text}
                  primaryTypographyProps={{
                    fontSize: '0.9rem',
                    fontWeight: currentPath === item.path ? 600 : 400,
                  }}
                />
              )}
              </ModernListItem>
            </Link>
          ))}
        </List>

        {/* Spacer to push bottom navigation down */}
        <Box sx={{ flexGrow: 1 }} />

        {/* Bottom Navigation - Currently empty (items moved to TopNavbar) */}
        {bottomNavigationItems.length > 0 && (
          <>
            <Divider sx={{ mx: 1, my: 2 }} />
            <List sx={{ mt: 'auto' }}>
              {bottomNavigationItems.map((item) => (
                <Link key={item.id} href={item.path} style={{ textDecoration: 'none', color: 'inherit' }}>
                  <ModernListItem
                    selected={currentPath === item.path}
                    sx={{ minHeight: 48 }}
                  >
                  <ListItemIcon sx={{ minWidth: collapsed ? 'auto' : 40, mr: collapsed ? 0 : 1 }}>
                    {item.badge ? (
                      <Badge
                        badgeContent={item.badge}
                        color={typeof item.badge === 'string' ? 'secondary' : 'error'}
                        max={99}
                      >
                        {item.icon}
                      </Badge>
                    ) : (
                      item.icon
                    )}
                  </ListItemIcon>
                  {!collapsed && (
                    <ListItemText
                      primary={item.text}
                      primaryTypographyProps={{
                        fontSize: '0.9rem',
                        fontWeight: currentPath === item.path ? 600 : 400,
                      }}
                    />
                  )}
                  </ModernListItem>
                </Link>
              ))}
            </List>
          </>
        )}
      </Box>
    </StyledDrawer>
  )
}
