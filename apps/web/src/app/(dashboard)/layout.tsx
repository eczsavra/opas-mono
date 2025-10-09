'use client'

import { useState } from 'react'
import { Box } from '@mui/material'
import TenantSidebar from '@/components/TenantSidebar'
import TenantNavbar from '@/components/TenantNavbar'

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const [navbarOpen, setNavbarOpen] = useState(false)

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      {/* Sidebar - Tüm dashboard sayfaları için global */}
      <TenantSidebar
        open={sidebarOpen}
        onToggle={() => setSidebarOpen(!sidebarOpen)}
        currentPath="/"
      />

      {/* Main Content */}
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          minHeight: '100vh',
          width: '100%',
          maxWidth: '100vw',
          overflowX: 'hidden',
          transition: 'margin 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
          marginLeft: { 
            xs: 0,
            sm: sidebarOpen ? '280px' : '0px'
          },
        }}
      >
        {/* Navbar */}
        <TenantNavbar
          open={navbarOpen}
          onToggle={() => setNavbarOpen(!navbarOpen)}
          onSidebarToggle={() => setSidebarOpen(!sidebarOpen)}
        />

        {/* Page Content */}
        <Box sx={{ pt: 1 }}>
          {children}
        </Box>
      </Box>
    </Box>
  )
}

