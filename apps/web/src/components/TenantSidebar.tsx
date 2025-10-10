'use client'

import {
  Box,
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Typography,
  Tooltip,
  useTheme,
  useMediaQuery,
  Chip,
  Avatar,
  Stack
} from '@mui/material'
import {
  Dashboard as DashboardIcon,
  Store as StoreIcon,
  Inventory as InventoryIcon,
  LocalPharmacy as PharmacyIcon,
  Speed,
  PointOfSale as SalesIcon,
  PersonAdd as PatientIcon
} from '@mui/icons-material'
import { styled } from '@mui/material/styles'

// Styled Components
const StyledDrawer = styled(Drawer)(() => ({
  '& .MuiDrawer-paper': {
    width: 280,
    background: 'linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(255,255,255,0.9) 100%)',
    backdropFilter: 'blur(20px)',
    border: 'none',
    boxShadow: '0 8px 32px rgba(0,0,0,0.1)',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  },
}))

const LogoContainer = styled(Box)(() => ({
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  p: 3,
  background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
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
    background: 'linear-gradient(45deg, transparent 30%, rgba(255,255,255,0.1) 50%, transparent 70%)',
    animation: 'shimmer 3s infinite',
  },
  '@keyframes shimmer': {
    '0%': { transform: 'translateX(-100%)' },
    '100%': { transform: 'translateX(100%)' },
  },
}))

const NavigationItem = styled(ListItemButton)<{ selected?: boolean }>(({ theme, selected }) => ({
  borderRadius: 12,
  margin: '4px 12px',
  minHeight: 48,
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  background: selected 
    ? 'linear-gradient(135deg, rgba(102, 126, 234, 0.15) 0%, rgba(118, 75, 162, 0.15) 100%)'
    : 'transparent',
  border: selected 
    ? '1px solid rgba(102, 126, 234, 0.3)'
    : '1px solid transparent',
  '&:hover': {
    background: 'linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%)',
    transform: 'translateX(4px)',
    boxShadow: '0 4px 20px rgba(102, 126, 234, 0.2)',
  },
  '& .MuiListItemIcon-root': {
    minWidth: 40,
    color: selected ? theme.palette.primary.main : theme.palette.text.secondary,
    transition: 'all 0.3s ease',
  },
  '& .MuiListItemText-primary': {
    fontWeight: selected ? 600 : 500,
    color: selected ? theme.palette.primary.main : theme.palette.text.primary,
    transition: 'all 0.3s ease',
  },
}))

const StatusIndicator = styled(Box)(() => ({
  width: 8,
  height: 8,
  borderRadius: '50%',
  background: 'linear-gradient(135deg, #4ade80 0%, #22c55e 100%)',
  boxShadow: '0 0 10px rgba(74, 222, 128, 0.5)',
  animation: 'pulse 2s infinite',
  '@keyframes pulse': {
    '0%, 100%': { opacity: 1 },
    '50%': { opacity: 0.5 },
  },
}))

// Navigation Items
const navigationItems = [
  { 
    id: 'dashboard', 
    text: 'Dashboard', 
    icon: <DashboardIcon />, 
    path: '/t-dashboard',
    badge: null 
  },
  { 
    id: 'sales', 
    text: 'Satış', 
    icon: <SalesIcon />, 
    path: '/satis',
    badge: null 
  },
  { 
    id: 'stock', 
    text: 'Stok', 
    icon: <InventoryIcon />, 
    path: '/stok/giris',
    badge: null 
  },
  { 
    id: 'patients', 
    text: 'Hasta', 
    icon: <PatientIcon />, 
    path: '/hasta/liste',
    badge: 'YENİ' 
  },
  { 
    id: 'stakeholders', 
    text: 'Paydaşlar', 
    icon: <StoreIcon />, 
    path: '/paydaslar',
    badge: null 
  },
  { 
    id: 'products', 
    text: 'Ürünler', 
    icon: <PharmacyIcon />, 
    path: '/productslist',
    badge: null 
  },
]

// Navigation item click handler with new tab support
const handleNavigationClick = (path: string, event: React.MouseEvent) => {
  // Middle click (wheel click) - open in new tab and prevent scroll
  if (event.button === 1) {
    event.preventDefault()
    event.stopPropagation()
    window.open(path, '_blank')
    return
  }
  
  // Normal click - navigate in same tab
  if (event.button === 0 && !event.ctrlKey && !event.metaKey && !event.shiftKey) {
    window.location.href = path
  }
  // Ctrl+Click, Cmd+Click, or Shift+Click - open in new tab
  else if (event.ctrlKey || event.metaKey || event.shiftKey) {
    event.preventDefault()
    window.open(path, '_blank')
  }
}

// Context menu handler for right-click
const handleContextMenu = (path: string, event: React.MouseEvent) => {
  event.preventDefault()
  // Show browser's default context menu with "Open in new tab" option
  // This will be handled by the browser automatically
}

interface TenantSidebarProps {
  open: boolean
  onToggle: () => void
  currentPath: string
}

export default function TenantSidebar({ open, onToggle, currentPath }: TenantSidebarProps) {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))

  // handleNavigation removed - using handleNavigationClick instead

  const sidebarContent = (
    <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      {/* Logo Section */}
      <LogoContainer>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, zIndex: 1 }}>
          <Avatar
            sx={{
              width: 40,
              height: 40,
              background: 'linear-gradient(135deg, rgba(255,255,255,0.2) 0%, rgba(255,255,255,0.1) 100%)',
              border: '2px solid rgba(255,255,255,0.3)',
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
      </LogoContainer>

      {/* System Status */}
      <Box sx={{ p: 2, borderBottom: '1px solid rgba(0,0,0,0.1)' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
          <Typography variant="caption" sx={{ fontWeight: 600, color: 'text.secondary' }}>
            Sistem Durumu
          </Typography>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
            <StatusIndicator />
            <Typography variant="caption" sx={{ color: 'success.main', fontWeight: 600 }}>
              Aktif
            </Typography>
          </Box>
        </Box>
        <Stack direction="row" spacing={1}>
          <Chip 
            label="ITS" 
            size="small" 
            sx={{ 
              background: 'linear-gradient(135deg, #4ade80 0%, #22c55e 100%)',
              color: 'white',
              fontWeight: 600,
              fontSize: '0.7rem'
            }} 
          />
          <Chip 
            label="Sync" 
            size="small" 
            sx={{ 
              background: 'linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)',
              color: 'white',
              fontWeight: 600,
              fontSize: '0.7rem'
            }} 
          />
        </Stack>
      </Box>

      {/* Navigation */}
      <Box sx={{ flex: 1, py: 2 }}>
        <List sx={{ px: 1 }}>
          {navigationItems.map((item) => {
            // Special handling for Stock menu - direct navigation
            if (item.id === 'stock') {
              const isSelected = currentPath.startsWith('/stok')
              return (
                <ListItem key={item.id} disablePadding>
                  <Tooltip title={item.text} placement="right">
                    <NavigationItem
                      selected={isSelected}
                      onClick={(e) => handleNavigationClick('/stok/liste', e)}
                      onMouseDown={(e) => {
                        if (e.button === 1) {
                          e.preventDefault()
                          e.stopPropagation()
                        }
                      }}
                      onMouseUp={(e) => handleNavigationClick('/stok/liste', e)}
                      onContextMenu={(e) => handleContextMenu('/stok/liste', e)}
                      sx={{ position: 'relative' }}
                    >
                      <ListItemIcon>{item.icon}</ListItemIcon>
                      <ListItemText 
                        primary={item.text}
                        primaryTypographyProps={{ fontSize: '0.9rem' }}
                      />
                      {item.badge && (
                        <Chip
                          label={item.badge}
                          size="small"
                          sx={{
                            ml: 1,
                            height: 20,
                            fontSize: '0.7rem',
                            background: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)',
                            color: 'white',
                            fontWeight: 600,
                          }}
                        />
                      )}
                    </NavigationItem>
                  </Tooltip>
                </ListItem>
              )
            }

            // Regular menu items
            const isSelected = currentPath === item.path
            return (
              <ListItem key={item.id} disablePadding>
                <Tooltip title={item.text} placement="right">
                  <NavigationItem
                    selected={isSelected}
                    onClick={(e) => handleNavigationClick(item.path, e)}
                    onMouseDown={(e) => {
                      if (e.button === 1) {
                        e.preventDefault()
                        e.stopPropagation()
                      }
                    }}
                    onMouseUp={(e) => handleNavigationClick(item.path, e)}
                    onContextMenu={(e) => handleContextMenu(item.path, e)}
                    sx={{ position: 'relative' }}
                  >
                    <ListItemIcon>
                      {item.icon}
                    </ListItemIcon>
                    <ListItemText 
                      primary={item.text}
                      primaryTypographyProps={{
                        fontSize: '0.9rem',
                      }}
                    />
                    {item.badge && (
                      <Chip
                        label={item.badge}
                        size="small"
                        sx={{
                          ml: 1,
                          height: 20,
                          fontSize: '0.7rem',
                          background: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)',
                          color: 'white',
                          fontWeight: 600,
                        }}
                      />
                    )}
                  </NavigationItem>
                </Tooltip>
              </ListItem>
            )
          })}
        </List>
      </Box>

      {/* Footer */}
      <Box sx={{ p: 2, borderTop: '1px solid rgba(0,0,0,0.1)' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
          <Avatar
            sx={{
              width: 32,
              height: 32,
              background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
            }}
          >
            <PharmacyIcon sx={{ fontSize: 18 }} />
          </Avatar>
          <Box>
            <Typography variant="body2" sx={{ fontWeight: 600, fontSize: '0.8rem' }}>
              {typeof window !== 'undefined' && localStorage.getItem('pharmacyName') || 'Eczane Adı'}
            </Typography>
            <Typography variant="caption" sx={{ color: 'text.secondary', fontSize: '0.7rem' }}>
              GLN: {typeof window !== 'undefined' && (localStorage.getItem('tenantId')?.replace('TNT_', '') || '...')}
            </Typography>
          </Box>
        </Box>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Speed sx={{ fontSize: 14, color: 'success.main' }} />
          <Typography variant="caption" sx={{ color: 'text.secondary', fontSize: '0.7rem' }}>
            Sistem performansı mükemmel
          </Typography>
        </Box>
      </Box>
    </Box>
  )

  return (
    <>
      {/* Mobile Drawer */}
      {isMobile && (
        <Drawer
          variant="temporary"
          open={open}
          onClose={onToggle}
          ModalProps={{
            keepMounted: true, // Better open performance on mobile
          }}
          sx={{
            '& .MuiDrawer-paper': {
              width: 280,
              background: 'linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(255,255,255,0.9) 100%)',
              backdropFilter: 'blur(20px)',
              border: 'none',
              boxShadow: '0 8px 32px rgba(0,0,0,0.1)',
            },
          }}
        >
          {sidebarContent}
        </Drawer>
      )}

      {/* Desktop Drawer */}
      {!isMobile && (
        <StyledDrawer
          variant="persistent"
          anchor="left"
          open={open}
          sx={{
            '& .MuiDrawer-paper': {
              width: open ? 280 : 0,
              transition: 'width 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
              overflowX: 'hidden',
            },
          }}
        >
          {sidebarContent}
        </StyledDrawer>
      )}
    </>
  )
}
