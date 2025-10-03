'use client'

import { useState } from 'react'
import { Box, Typography, Fade } from '@mui/material'
import { styled, alpha } from '@mui/material/styles'
import { useLocalService } from '@/hooks/useLocalService'
import LocalServicePopup from './LocalServicePopup'

const StatusBarContainer = styled(Box, {
  shouldForwardProp: (prop) => prop !== 'sidebarOpen',
})<{ sidebarOpen: boolean }>(({ theme, sidebarOpen }) => ({
  position: 'fixed',
  bottom: 0,
  right: 0,
  height: 40,
  backgroundColor: alpha(theme.palette.background.paper, 0.98),
  backdropFilter: 'blur(20px)',
  borderTop: `2px solid ${alpha(theme.palette.primary.main, 0.3)}`,
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'flex-end', // Sağa dayalı
  paddingLeft: theme.spacing(3),
  paddingRight: theme.spacing(3),
  cursor: 'pointer',
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  zIndex: 9999,
  boxShadow: `0 -4px 20px ${alpha(theme.palette.common.black, 0.1)}`,
  // Responsive left margin using media queries
  [theme.breakpoints.up('md')]: {
    left: sidebarOpen ? 280 : 0,
  },
  [theme.breakpoints.down('md')]: {
    left: 0,
  },
  '&:hover': {
    backgroundColor: alpha(theme.palette.background.paper, 1),
    borderTopColor: theme.palette.primary.main,
    transform: 'translateY(-2px)',
    boxShadow: `0 -8px 30px ${alpha(theme.palette.common.black, 0.15)}`,
  }
}))

const StatusIndicator = styled(Box, {
  shouldForwardProp: (prop) => prop !== 'active',
})<{ active: boolean }>(({ theme, active }) => ({
  width: 12,
  height: 12,
  borderRadius: '50%',
  backgroundColor: active ? theme.palette.success.main : theme.palette.error.main,
  marginRight: theme.spacing(1.5),
  border: `2px solid ${alpha(active ? theme.palette.success.main : theme.palette.error.main, 0.3)}`,
  boxShadow: active 
    ? `0 0 12px ${alpha(theme.palette.success.main, 0.8)}, inset 0 0 4px ${alpha(theme.palette.success.light, 0.5)}`
    : `0 0 12px ${alpha(theme.palette.error.main, 0.8)}, inset 0 0 4px ${alpha(theme.palette.error.light, 0.5)}`,
  animation: active ? 'pulse 2s infinite' : 'blink 1s infinite',
  '@keyframes pulse': {
    '0%': {
      boxShadow: `0 0 12px ${alpha(theme.palette.success.main, 0.8)}, inset 0 0 4px ${alpha(theme.palette.success.light, 0.5)}`
    },
    '50%': {
      boxShadow: `0 0 20px ${alpha(theme.palette.success.main, 1)}, inset 0 0 6px ${alpha(theme.palette.success.light, 0.8)}`
    },
    '100%': {
      boxShadow: `0 0 12px ${alpha(theme.palette.success.main, 0.8)}, inset 0 0 4px ${alpha(theme.palette.success.light, 0.5)}`
    }
  },
  '@keyframes blink': {
    '0%': {
      opacity: 1
    },
    '50%': {
      opacity: 0.5
    },
    '100%': {
      opacity: 1
    }
  }
}))

interface StatusBarProps {
  sidebarOpen?: boolean
}

export default function StatusBar({ sidebarOpen = false }: StatusBarProps) {
  const { status, isLoading } = useLocalService()
  const [showPopup, setShowPopup] = useState(false)

  if (isLoading) {
    return null // İlk yüklenirken gösterme
  }

  const handleClick = () => {
    setShowPopup(true)
  }

  const formatTime = (date: Date) => {
    return date.toLocaleTimeString('tr-TR', { 
      hour: '2-digit', 
      minute: '2-digit' 
    })
  }

  return (
    <>
      <Fade in={true}>
        <StatusBarContainer sidebarOpen={sidebarOpen} onClick={handleClick}>
          <StatusIndicator active={status.isActive} />
          <Typography 
            variant="body2" 
            sx={{ 
              fontSize: '0.875rem',
              fontWeight: 500,
              color: 'text.primary',
              userSelect: 'none'
            }}
          >
            {status.isActive ? (
              <>Local Service Aktif | Son kontrol: {formatTime(status.lastCheck)}</>
            ) : (
              <>Local Service Aktif Değil | Son kontrol: {formatTime(status.lastCheck)}</>
            )}
          </Typography>
        </StatusBarContainer>
      </Fade>

      <LocalServicePopup 
        open={showPopup}
        onClose={() => setShowPopup(false)}
        status={status}
      />
    </>
  )
}
