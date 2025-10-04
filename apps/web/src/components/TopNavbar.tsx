'use client'

import { useState, useRef } from 'react'
import {
  AppBar,
  Toolbar,
  Box,
  IconButton,
  InputBase,
  Badge,
  Avatar,
  Menu,
  MenuItem,
  Divider,
  ListItemIcon,
  ListItemText,
  Typography,
  Paper,
  List,
  ListItem,
  Tooltip,
  alpha,
  useTheme,
  useMediaQuery
} from '@mui/material'
import {
  Search as SearchIcon,
  Notifications as NotificationsIcon,
  Settings as SettingsIcon,
  ExitToApp as LogoutIcon,
  DarkMode as DarkModeIcon,
  LightMode as LightModeIcon,
  Menu as MenuIcon,
  Person as PersonIcon,
  Security as SecurityIcon,
  Help as HelpIcon,
  AdminPanelSettings as AdminIcon
} from '@mui/icons-material'
import { styled } from '@mui/material/styles'

// Styled Components
const StyledAppBar = styled(AppBar)(({ theme }) => ({
  backgroundColor: theme.palette.background.paper,
  borderBottom: `1px solid ${alpha(theme.palette.divider, 0.12)}`,
  boxShadow: '0 1px 8px rgba(0,0,0,0.08)',
  backdropFilter: 'blur(10px)',
  position: 'sticky',
  top: 0,
  zIndex: theme.zIndex.appBar,
}))

const SearchContainer = styled(Box)(({ theme }) => ({
  position: 'relative',
  borderRadius: '24px',
  backgroundColor: alpha(theme.palette.action.selected, 0.08),
  border: `1px solid ${alpha(theme.palette.divider, 0.12)}`,
  '&:hover': {
    backgroundColor: alpha(theme.palette.action.selected, 0.12),
    borderColor: alpha(theme.palette.primary.main, 0.25),
  },
  '&:focus-within': {
    backgroundColor: alpha(theme.palette.action.selected, 0.16),
    borderColor: theme.palette.primary.main,
    boxShadow: `0 0 0 2px ${alpha(theme.palette.primary.main, 0.2)}`,
  },
  marginLeft: theme.spacing(2),
  marginRight: theme.spacing(2),
  width: '100%',
  maxWidth: '500px',
  minWidth: '200px',
  transition: 'all 0.2s ease',
}))

const SearchIconWrapper = styled('div')(({ theme }) => ({
  padding: theme.spacing(0, 2),
  height: '100%',
  position: 'absolute',
  pointerEvents: 'none',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  color: theme.palette.text.secondary,
}))

const StyledInputBase = styled(InputBase)(({ theme }) => ({
  color: 'inherit',
  width: '100%',
  '& .MuiInputBase-input': {
    padding: theme.spacing(1, 1, 1, 0),
    paddingLeft: `calc(1em + ${theme.spacing(4)})`,
    paddingRight: theme.spacing(2),
    transition: theme.transitions.create('width'),
    width: '100%',
    fontSize: '0.9rem',
    fontWeight: 500,
    '&::placeholder': {
      color: theme.palette.text.secondary,
      opacity: 0.8,
    }
  },
}))

const NotificationMenu = styled(Paper)(({ theme }) => ({
  width: '350px',
  maxHeight: '400px',
  borderRadius: '16px',
  border: `1px solid ${alpha(theme.palette.divider, 0.12)}`,
  boxShadow: '0 8px 32px rgba(0,0,0,0.12)',
}))

// Mock data
const notifications = [
  {
    id: 1,
    title: 'Yeni Reçete Talebi',
    message: 'Dr. Ahmet Yılmaz tarafından 3 reçete onayınızı bekliyor',
    time: '5 dk önce',
    unread: true,
    type: 'prescription'
  },
  {
    id: 2,
    title: 'Stok Uyarısı',
    message: 'Parol 500mg tablet stokta azaldı (12 adet kaldı)',
    time: '15 dk önce',
    unread: true,
    type: 'inventory'
  },
  {
    id: 3,
    title: 'Ödeme Alındı',
    message: '₺487.50 tutarındaki ödeme başarıyla alındı',
    time: '1 sa önce',
    unread: false,
    type: 'payment'
  },
]

interface TopNavbarProps {
  sidebarCollapsed?: boolean
  onToggleSidebar?: () => void
  userName?: string
  username?: string
  userAvatar?: string
}

export default function TopNavbar({ 
  sidebarCollapsed = false,
  onToggleSidebar,
  userName: userNameProp,
  username: usernameProp,
  userAvatar
}: TopNavbarProps) {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))
  
  // Get user info from localStorage (dynamic)
  const userName = userNameProp || 
    (typeof window !== 'undefined' 
      ? `${localStorage.getItem('firstName') || ''} ${localStorage.getItem('lastName') || ''}`.trim() || 'Kullanıcı'
      : 'Kullanıcı')
  
  const username = usernameProp || 
    (typeof window !== 'undefined' 
      ? localStorage.getItem('username') || 'user'
      : 'user')
  
  // States
  const [searchQuery, setSearchQuery] = useState('')
  const [profileMenuOpen, setProfileMenuOpen] = useState(false)
  const [notificationMenuOpen, setNotificationMenuOpen] = useState(false)
  const [darkMode, setDarkMode] = useState(false)
  
  console.log('Sidebar collapsed:', sidebarCollapsed) // Debug için
  
  // Refs
  const profileButtonRef = useRef<HTMLButtonElement>(null)
  const notificationButtonRef = useRef<HTMLButtonElement>(null)
  
  // Handlers
  const handleSearch = (event: React.FormEvent) => {
    event.preventDefault()
    if (searchQuery.trim()) {
      console.log('Arama:', searchQuery)
      // TODO: Implement search functionality
    }
  }

  const handleProfileMenuToggle = () => {
    setProfileMenuOpen(!profileMenuOpen)
  }

  const handleNotificationMenuToggle = () => {
    setNotificationMenuOpen(!notificationMenuOpen)
  }

  const handleLogout = async () => {
    try {
      // Kullanıcı bilgisini al
      const userData = localStorage.getItem('user')
      const username = userData ? JSON.parse(userData).username : 'unknown'

      // Backend'e logout isteği gönder
      await fetch('/api/opas/auth/logout', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ username }),
      })
    } catch (error) {
      console.error('Logout error:', error)
    } finally {
      // Her durumda localStorage temizle ve login'e yönlendir
      localStorage.removeItem('user')
      window.location.href = '/login2'
    }
  }

  const handleToggleDarkMode = () => {
    setDarkMode(!darkMode)
    // TODO: Implement theme toggle
  }

  const unreadNotificationCount = notifications.filter(n => n.unread).length

  return (
    <StyledAppBar elevation={0}>
      <Toolbar sx={{ 
        minHeight: { xs: 56, sm: 64 },
        px: { xs: 1, sm: 2 }
      }}>
        {/* Mobile Hamburger Menu */}
        {isMobile && (
          <IconButton
            edge="start"
            color="inherit"
            onClick={onToggleSidebar}
            sx={{ mr: 1 }}
          >
            <MenuIcon />
          </IconButton>
        )}

        {/* Search Bar */}
        <SearchContainer>
          <SearchIconWrapper>
            <SearchIcon fontSize="small" />
          </SearchIconWrapper>
          <form onSubmit={handleSearch} style={{ width: '100%' }}>
            <StyledInputBase
              placeholder="Ara, Sor, Keşfet!"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              inputProps={{ 'aria-label': 'search' }}
            />
          </form>
        </SearchContainer>

        {/* Spacer to push right items to the end */}
        <Box sx={{ flexGrow: 1 }} />

        {/* Right Side Actions */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          {/* Settings */}
          <Tooltip title="Ayarlar">
            <IconButton
              sx={{
                color: 'text.secondary',
                '&:hover': {
                  backgroundColor: alpha(theme.palette.primary.main, 0.08),
                  color: 'primary.main',
                }
              }}
            >
              <SettingsIcon />
            </IconButton>
          </Tooltip>

          {/* Dark Mode Toggle */}
          <Tooltip title={darkMode ? "Açık Tema" : "Koyu Tema"}>
            <IconButton
              onClick={handleToggleDarkMode}
              sx={{
                color: 'text.secondary',
                '&:hover': {
                  backgroundColor: alpha(theme.palette.primary.main, 0.08),
                  color: 'primary.main',
                }
              }}
            >
              {darkMode ? <LightModeIcon /> : <DarkModeIcon />}
            </IconButton>
          </Tooltip>

          {/* Notifications */}
          <Tooltip title="Bildirimler">
            <IconButton
              ref={notificationButtonRef}
              onClick={handleNotificationMenuToggle}
              sx={{
                color: 'text.secondary',
                '&:hover': {
                  backgroundColor: alpha(theme.palette.primary.main, 0.08),
                  color: 'primary.main',
                }
              }}
            >
              <Badge badgeContent={unreadNotificationCount} color="error" max={9}>
                <NotificationsIcon />
              </Badge>
            </IconButton>
          </Tooltip>

          {/* Profile Menu */}
          <Tooltip title="Profil">
            <IconButton
              ref={profileButtonRef}
              onClick={handleProfileMenuToggle}
              sx={{
                ml: 1,
                '&:hover': {
                  backgroundColor: alpha(theme.palette.primary.main, 0.08),
                }
              }}
            >
              <Avatar
                sx={{ width: 32, height: 32 }}
                src={userAvatar}
              >
                {userName.split(' ').map(n => n[0]).join('').slice(0, 2)}
              </Avatar>
            </IconButton>
          </Tooltip>

          {/* User Info - Desktop Only */}
          {!isMobile && (
            <Box sx={{ ml: 1, minWidth: 0 }}>
              <Typography
                variant="subtitle2"
                sx={{
                  fontWeight: 600,
                  color: 'text.primary',
                  lineHeight: 1.2,
                }}
                noWrap
              >
                {username}
              </Typography>
              <Typography
                variant="caption"
                sx={{
                  color: 'text.secondary',
                  display: 'block',
                  lineHeight: 1,
                }}
                noWrap
              >
                Eczacı
              </Typography>
            </Box>
          )}
        </Box>

        {/* Profile Menu */}
        <Menu
          anchorEl={profileButtonRef.current}
          open={profileMenuOpen}
          onClose={() => setProfileMenuOpen(false)}
          onClick={() => setProfileMenuOpen(false)}
          PaperProps={{
            sx: {
              mt: 1.5,
              minWidth: 220,
              borderRadius: 2,
              border: `1px solid ${alpha(theme.palette.divider, 0.12)}`,
              boxShadow: '0 8px 32px rgba(0,0,0,0.12)',
            }
          }}
          transformOrigin={{ horizontal: 'right', vertical: 'top' }}
          anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
        >
          <Box sx={{ px: 2, py: 1.5, borderBottom: `1px solid ${alpha(theme.palette.divider, 0.12)}` }}>
            <Typography variant="subtitle2" fontWeight={600}>
              {username}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {username}@hotmail.com
            </Typography>
          </Box>
          
          <MenuItem>
            <ListItemIcon>
              <PersonIcon fontSize="small" />
            </ListItemIcon>
            <ListItemText>Profil</ListItemText>
          </MenuItem>
          
          <MenuItem>
            <ListItemIcon>
              <SettingsIcon fontSize="small" />
            </ListItemIcon>
            <ListItemText>Ayarlar</ListItemText>
          </MenuItem>
          
          <MenuItem>
            <ListItemIcon>
              <SecurityIcon fontSize="small" />
            </ListItemIcon>
            <ListItemText>Güvenlik</ListItemText>
          </MenuItem>
          
          <MenuItem>
            <ListItemIcon>
              <AdminIcon fontSize="small" />
            </ListItemIcon>
            <ListItemText>Yönetici</ListItemText>
          </MenuItem>

          <Divider />
          
          <MenuItem>
            <ListItemIcon>
              <HelpIcon fontSize="small" />
            </ListItemIcon>
            <ListItemText>Yardım</ListItemText>
          </MenuItem>
          
          <MenuItem onClick={handleLogout} sx={{ color: 'error.main' }}>
            <ListItemIcon>
              <LogoutIcon fontSize="small" color="error" />
            </ListItemIcon>
            <ListItemText>Çıkış Yap</ListItemText>
          </MenuItem>
        </Menu>

        {/* Notifications Menu */}
        <Menu
          anchorEl={notificationButtonRef.current}
          open={notificationMenuOpen}
          onClose={() => setNotificationMenuOpen(false)}
          PaperProps={{
            sx: { p: 0, mt: 1.5 }
          }}
          transformOrigin={{ horizontal: 'right', vertical: 'top' }}
          anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
        >
          <NotificationMenu>
            <Box sx={{ p: 2, borderBottom: `1px solid ${alpha(theme.palette.divider, 0.12)}` }}>
              <Typography variant="h6" fontWeight={600}>
                Bildirimler
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {unreadNotificationCount} okunmamış bildirim
              </Typography>
            </Box>
            
            <List sx={{ p: 0, maxHeight: 300, overflow: 'auto' }}>
              {notifications.map((notification) => (
                <ListItem
                  key={notification.id}
                  sx={{
                    borderBottom: `1px solid ${alpha(theme.palette.divider, 0.08)}`,
                    backgroundColor: notification.unread ? alpha(theme.palette.primary.main, 0.04) : 'transparent',
                    '&:hover': {
                      backgroundColor: alpha(theme.palette.action.hover, 0.08),
                    },
                    cursor: 'pointer',
                  }}
                >
                  <Box sx={{ width: '100%' }}>
                    <Typography
                      variant="subtitle2"
                      fontWeight={notification.unread ? 600 : 400}
                      sx={{ mb: 0.5 }}
                    >
                      {notification.title}
                    </Typography>
                    <Typography
                      variant="body2"
                      color="text.secondary"
                      sx={{ mb: 0.5, fontSize: '0.8rem' }}
                    >
                      {notification.message}
                    </Typography>
                    <Typography
                      variant="caption"
                      color="text.secondary"
                    >
                      {notification.time}
                    </Typography>
                  </Box>
                </ListItem>
              ))}
            </List>
          </NotificationMenu>
        </Menu>
      </Toolbar>
    </StyledAppBar>
  )
}
