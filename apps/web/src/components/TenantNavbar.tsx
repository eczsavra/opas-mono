'use client'

import { useState } from 'react'
import {
  AppBar,
  Toolbar,
  Typography,
  IconButton,
  Box,
  TextField,
  InputAdornment,
  Menu,
  MenuItem,
  Avatar,
  Badge,
  Tooltip,
  useTheme,
  useMediaQuery,
  alpha,
  Stack,
  Divider,
  ListItemIcon,
  ListItemText
} from '@mui/material'
import {
  Menu as MenuIcon,
  Search as SearchIcon,
  Notifications as NotificationsIcon,
  ExitToApp as LogoutIcon,
  Brightness4 as DarkModeIcon,
  Brightness7 as LightModeIcon,
  Settings as SettingsIcon,
  Help as HelpIcon,
  Person as PersonIcon,
  Security as SecurityIcon,
  TrendingUp as TrendingUpIcon,
  Speed as SpeedIcon
} from '@mui/icons-material'
import { styled } from '@mui/material/styles'

// Styled Components
const StyledAppBar = styled(AppBar)(({ theme }) => ({
  background: 'linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(255,255,255,0.9) 100%)',
  backdropFilter: 'blur(20px)',
  border: 'none',
  boxShadow: '0 4px 20px rgba(0,0,0,0.1)',
  color: theme.palette.text.primary,
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
}))

const SearchField = styled(TextField)(({ theme }) => ({
  '& .MuiOutlinedInput-root': {
    borderRadius: 25,
    backgroundColor: alpha(theme.palette.background.paper, 0.8),
    backdropFilter: 'blur(10px)',
    border: `1px solid ${alpha(theme.palette.primary.main, 0.1)}`,
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    '&:hover': {
      borderColor: alpha(theme.palette.primary.main, 0.3),
      backgroundColor: alpha(theme.palette.background.paper, 0.9),
      transform: 'translateY(-1px)',
      boxShadow: `0 4px 20px ${alpha(theme.palette.primary.main, 0.1)}`,
    },
    '&.Mui-focused': {
      borderColor: theme.palette.primary.main,
      backgroundColor: alpha(theme.palette.background.paper, 1),
      boxShadow: `0 0 0 3px ${alpha(theme.palette.primary.main, 0.1)}`,
    },
  },
  '& .MuiOutlinedInput-notchedOutline': {
    border: 'none',
  },
  '& .MuiInputLabel-root': {
    color: theme.palette.text.secondary,
    '&.Mui-focused': {
      color: theme.palette.primary.main,
    },
  },
}))

const ActionButton = styled(IconButton)(({ theme }) => ({
  borderRadius: 12,
  background: alpha(theme.palette.background.paper, 0.8),
  backdropFilter: 'blur(10px)',
  border: `1px solid ${alpha(theme.palette.primary.main, 0.1)}`,
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  '&:hover': {
    background: alpha(theme.palette.primary.main, 0.1),
    borderColor: alpha(theme.palette.primary.main, 0.3),
    transform: 'translateY(-2px)',
    boxShadow: `0 4px 20px ${alpha(theme.palette.primary.main, 0.2)}`,
  },
  '&:active': {
    transform: 'translateY(0)',
  },
}))

const LogoutButton = styled(IconButton)(({ theme }) => ({
  borderRadius: 12,
  background: alpha(theme.palette.error.main, 0.1),
  border: `1px solid ${alpha(theme.palette.error.main, 0.2)}`,
  color: theme.palette.error.main,
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  '&:hover': {
    background: alpha(theme.palette.error.main, 0.2),
    borderColor: alpha(theme.palette.error.main, 0.4),
    transform: 'translateY(-2px)',
    boxShadow: `0 4px 20px ${alpha(theme.palette.error.main, 0.3)}`,
  },
}))

interface TenantNavbarProps {
  open: boolean
  onToggle: () => void
  onSidebarToggle: () => void
}

export default function TenantNavbar({ open, onSidebarToggle }: TenantNavbarProps) {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))
  
  // State
  const [searchValue, setSearchValue] = useState('')
  const [accountMenuAnchor, setAccountMenuAnchor] = useState<null | HTMLElement>(null)
  const [notificationsMenuAnchor, setNotificationsMenuAnchor] = useState<null | HTMLElement>(null)
  const [darkMode, setDarkMode] = useState(false)

  // Handlers
  const handleAccountMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAccountMenuAnchor(event.currentTarget)
  }

  const handleAccountMenuClose = () => {
    setAccountMenuAnchor(null)
  }

  const handleNotificationsMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setNotificationsMenuAnchor(event.currentTarget)
  }

  const handleNotificationsMenuClose = () => {
    setNotificationsMenuAnchor(null)
  }

  const handleLogout = () => {
    // Logout logic
    window.location.href = '/t-login'
  }

  const handleDarkModeToggle = () => {
    setDarkMode(!darkMode)
    // Theme toggle logic will be implemented
  }

  const handleSearch = (value: string) => {
    console.log('Search:', value)
    // Search logic will be implemented
  }

  return (
    <StyledAppBar position="sticky" elevation={0}>
      <Toolbar sx={{ px: { xs: 2, md: 3 }, py: 1 }}>
        {/* Left Section */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          {/* Sidebar Toggle */}
          <ActionButton
            onClick={onSidebarToggle}
            size="small"
            sx={{ 
              display: { xs: 'flex', md: open ? 'flex' : 'none' },
              mr: 1
            }}
          >
            <MenuIcon />
          </ActionButton>

          {/* Search Bar */}
          <SearchField
            placeholder="Ara, Sor, Keşfet!"
            value={searchValue}
            onChange={(e) => setSearchValue(e.target.value)}
            onKeyPress={(e) => {
              if (e.key === 'Enter') {
                handleSearch(searchValue)
              }
            }}
            size="small"
            sx={{ 
              width: { xs: '100%', sm: 300, md: 400 },
              display: { xs: 'none', sm: 'block' }
            }}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon sx={{ color: 'text.secondary', fontSize: 20 }} />
                </InputAdornment>
              ),
            }}
          />
        </Box>

        {/* Spacer */}
        <Box sx={{ flexGrow: 1 }} />

        {/* Right Section */}
        <Stack direction="row" spacing={1} alignItems="center">
          {/* Mobile Search */}
          {isMobile && (
            <ActionButton size="small">
              <SearchIcon />
            </ActionButton>
          )}

          {/* Dark Mode Toggle */}
          <Tooltip title={darkMode ? 'Açık Mod' : 'Koyu Mod'}>
            <ActionButton size="small" onClick={handleDarkModeToggle}>
              {darkMode ? <LightModeIcon /> : <DarkModeIcon />}
            </ActionButton>
          </Tooltip>

          {/* Notifications */}
          <Tooltip title="Bildirimler">
            <ActionButton 
              size="small" 
              onClick={handleNotificationsMenuOpen}
            >
              <Badge badgeContent={3} color="error">
                <NotificationsIcon />
              </Badge>
            </ActionButton>
          </Tooltip>

          {/* Account Menu */}
          <Tooltip title="Hesap">
            <ActionButton 
              size="small" 
              onClick={handleAccountMenuOpen}
            >
              <Avatar 
                sx={{ 
                  width: 32, 
                  height: 32,
                  background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                  fontSize: '0.9rem'
                }}
              >
                E
              </Avatar>
            </ActionButton>
          </Tooltip>

          {/* Logout */}
          <Tooltip title="Çıkış Yap">
            <LogoutButton size="small" onClick={handleLogout}>
              <LogoutIcon />
            </LogoutButton>
          </Tooltip>
        </Stack>
      </Toolbar>

      {/* Notifications Menu */}
      <Menu
        anchorEl={notificationsMenuAnchor}
        open={Boolean(notificationsMenuAnchor)}
        onClose={handleNotificationsMenuClose}
        PaperProps={{
          sx: {
            mt: 1,
            minWidth: 300,
            borderRadius: 3,
            background: 'linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(255,255,255,0.9) 100%)',
            backdropFilter: 'blur(20px)',
            border: '1px solid rgba(255,255,255,0.2)',
            boxShadow: '0 8px 32px rgba(0,0,0,0.1)',
          }
        }}
      >
        <Box sx={{ p: 2, borderBottom: '1px solid rgba(0,0,0,0.1)' }}>
          <Typography variant="h6" sx={{ fontWeight: 600, display: 'flex', alignItems: 'center', gap: 1 }}>
            <NotificationsIcon sx={{ color: 'primary.main' }} />
            Bildirimler
          </Typography>
        </Box>
        <MenuItem onClick={handleNotificationsMenuClose}>
          <ListItemIcon>
            <TrendingUpIcon sx={{ color: 'success.main' }} />
          </ListItemIcon>
          <ListItemText 
            primary="Yeni Satış Tamamlandı"
            secondary="Paracetamol 500mg - 2 kutu"
          />
        </MenuItem>
        <MenuItem onClick={handleNotificationsMenuClose}>
          <ListItemIcon>
            <SpeedIcon sx={{ color: 'info.main' }} />
          </ListItemIcon>
          <ListItemText 
            primary="Stok Uyarısı"
            secondary="Aspirin 100mg stok seviyesi düşük"
          />
        </MenuItem>
        <MenuItem onClick={handleNotificationsMenuClose}>
          <ListItemIcon>
            <SecurityIcon sx={{ color: 'warning.main' }} />
          </ListItemIcon>
          <ListItemText 
            primary="Sistem Güncellemesi"
            secondary="ITS entegrasyonu güncellendi"
          />
        </MenuItem>
      </Menu>

      {/* Account Menu */}
      <Menu
        anchorEl={accountMenuAnchor}
        open={Boolean(accountMenuAnchor)}
        onClose={handleAccountMenuClose}
        PaperProps={{
          sx: {
            mt: 1,
            minWidth: 250,
            borderRadius: 3,
            background: 'linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(255,255,255,0.9) 100%)',
            backdropFilter: 'blur(20px)',
            border: '1px solid rgba(255,255,255,0.2)',
            boxShadow: '0 8px 32px rgba(0,0,0,0.1)',
          }
        }}
      >
        <Box sx={{ p: 2, borderBottom: '1px solid rgba(0,0,0,0.1)' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Avatar 
              sx={{ 
                width: 40, 
                height: 40,
                background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
              }}
            >
              E
            </Avatar>
            <Box>
              <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                Eczane Adı
              </Typography>
              <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                GLN: 8680001530144
              </Typography>
            </Box>
          </Box>
        </Box>
        <MenuItem onClick={handleAccountMenuClose}>
          <ListItemIcon>
            <PersonIcon />
          </ListItemIcon>
          <ListItemText primary="Profil" />
        </MenuItem>
        <MenuItem onClick={handleAccountMenuClose}>
          <ListItemIcon>
            <SettingsIcon />
          </ListItemIcon>
          <ListItemText primary="Ayarlar" />
        </MenuItem>
        <MenuItem onClick={handleAccountMenuClose}>
          <ListItemIcon>
            <HelpIcon />
          </ListItemIcon>
          <ListItemText primary="Yardım" />
        </MenuItem>
        <Divider />
        <MenuItem onClick={handleLogout} sx={{ color: 'error.main' }}>
          <ListItemIcon>
            <LogoutIcon sx={{ color: 'error.main' }} />
          </ListItemIcon>
          <ListItemText primary="Çıkış Yap" />
        </MenuItem>
      </Menu>
    </StyledAppBar>
  )
}
