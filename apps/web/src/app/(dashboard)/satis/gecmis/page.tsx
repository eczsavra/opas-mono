'use client'

import { useState } from 'react'
import { Box, Container, Typography } from '@mui/material'
import TenantSidebar from '@/components/TenantSidebar'
import TenantNavbar from '@/components/TenantNavbar'

export default function SatisGecmisPage() {
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const [navbarOpen, setNavbarOpen] = useState(false)

  const handleSidebarToggle = () => {
    setSidebarOpen(!sidebarOpen)
  }

  const handleNavbarToggle = () => {
    setNavbarOpen(!navbarOpen)
  }

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', backgroundColor: '#f8fafc' }}>
      {/* Tenant Sidebar */}
      <TenantSidebar 
        open={sidebarOpen} 
        onToggle={handleSidebarToggle}
        currentPath="/satis/gecmis"
      />

      {/* Main Content */}
      <Box 
        component="main" 
        sx={{ 
          flexGrow: 1,
          minHeight: '100vh',
          transition: 'margin 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
          marginLeft: { 
            xs: 0,
            md: sidebarOpen ? '280px' : 0
          }
        }}
      >
        {/* Tenant Navbar */}
        <TenantNavbar 
          open={navbarOpen}
          onToggle={handleNavbarToggle}
          onSidebarToggle={handleSidebarToggle}
        />

        {/* Page Content */}
        <Container maxWidth="xl" sx={{ py: 4 }}>
          <Typography variant="h4" component="h1" gutterBottom>
            Satış Geçmişi
          </Typography>
          
          {/* TODO: Satış geçmişi listesi buraya eklenecek */}
        </Container>
      </Box>
    </Box>
  )
}
