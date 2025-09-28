'use client';

import React from 'react';
import {
  AppBar,
  Toolbar,
  Box,
  Typography,
  Chip,
  Avatar,
  Menu,
  MenuItem,
  ListItemIcon,
  ListItemText,
  Divider,
  IconButton,
} from '@mui/material';
import {
  SupervisorAccount as SuperAdminIcon,
  ExitToApp as LogoutIcon,
  Settings as SettingsIcon,
} from '@mui/icons-material';
import { useAuth } from '../contexts/AuthContext';


export default function RoleSwitcher() {
  const { user, logout } = useAuth();

  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = () => {
    logout();
    handleMenuClose();
  };

  if (!user) {
    return null;
  }

  return (
    <AppBar 
      position="static" 
      color="default" 
      elevation={1}
      sx={{ 
        borderBottom: 1, 
        borderColor: 'divider',
        backgroundColor: 'background.paper'
      }}
    >
      <Toolbar variant="dense" sx={{ minHeight: 56 }}>
        {/* Left Side - SuperAdmin Role */}
        <Box sx={{ display: 'flex', alignItems: 'center', flexGrow: 1 }}>
          <Chip
            icon={<SuperAdminIcon />}
            label="OPAS SuperAdmin"
            color="error"
            variant="outlined"
            sx={{ mr: 2, fontWeight: 600 }}
          />
          <Typography variant="body2" color="text.secondary">
            Sistem Yöneticisi - Tüm Erişim
          </Typography>
        </Box>

        {/* Right Side - User Menu */}
        <Box sx={{ display: 'flex', alignItems: 'center' }}>
          <Typography variant="body2" sx={{ mr: 1, display: { xs: 'none', sm: 'block' } }}>
            {user.fullName}
          </Typography>
          
          <IconButton
            size="small"
            onClick={handleMenuClick}
            sx={{ ml: 1 }}
          >
            <Avatar
              sx={{ width: 32, height: 32, bgcolor: 'error.main' }}
            >
              {user.fullName.charAt(0)}
            </Avatar>
          </IconButton>

          {/* User Menu */}
          <Menu
            anchorEl={anchorEl}
            open={Boolean(anchorEl)}
            onClose={handleMenuClose}
            PaperProps={{
              elevation: 8,
              sx: {
                minWidth: 280,
                mt: 1.5,
                '& .MuiMenuItem-root': {
                  px: 2,
                  py: 1,
                },
              },
            }}
          >
            {/* User Info Header */}
            <Box sx={{ px: 2, py: 2, borderBottom: 1, borderColor: 'divider' }}>
              <Typography variant="subtitle1" fontWeight={600}>
                {user.fullName}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {user.email}
              </Typography>
              <Chip
                size="small"
                icon={<SuperAdminIcon />}
                label="OPAS SuperAdmin"
                color="error"
                sx={{ mt: 1 }}
              />
            </Box>

            <MenuItem onClick={handleMenuClose}>
              <ListItemIcon>
                <SettingsIcon fontSize="small" />
              </ListItemIcon>
              <ListItemText primary="Sistem Ayarları" />
            </MenuItem>

            <Divider />

            <MenuItem onClick={handleLogout}>
              <ListItemIcon>
                <LogoutIcon fontSize="small" />
              </ListItemIcon>
              <ListItemText primary="Çıkış Yap" />
            </MenuItem>
          </Menu>
        </Box>
      </Toolbar>
    </AppBar>
  );
}