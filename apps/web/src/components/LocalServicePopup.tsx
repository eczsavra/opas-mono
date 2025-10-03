'use client'

import { useState } from 'react'
import { 
  Dialog, 
  DialogTitle, 
  DialogContent, 
  DialogActions,
  Box, 
  Typography, 
  Button,
  Chip,
  Stack,
  Divider,
  IconButton
} from '@mui/material'
import { 
  Close as CloseIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  CreditCard as CreditCardIcon,
  Print as PrintIcon,
  LocalHospital as LocalHospitalIcon,
  Download as DownloadIcon,
  Help as HelpIcon,
  Settings as SettingsIcon,
  BugReport as BugReportIcon,
  Stop as StopIcon
} from '@mui/icons-material'
import { styled, alpha } from '@mui/material/styles'
import ProtocolConfirmDialog from './ProtocolConfirmDialog'

const StyledDialog = styled(Dialog)(({ theme }) => ({
  '& .MuiDialog-paper': {
    borderRadius: 16,
    minWidth: 400,
    background: `linear-gradient(135deg, 
      ${alpha(theme.palette.background.paper, 0.95)} 0%,
      ${alpha(theme.palette.background.paper, 0.9)} 100%
    )`,
    backdropFilter: 'blur(20px)',
    border: `1px solid ${alpha(theme.palette.primary.main, 0.1)}`,
  }
}))

const ServiceItem = styled(Box)(({ theme }) => ({
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  padding: theme.spacing(1, 0),
}))

interface LocalServicePopupProps {
  open: boolean
  onClose: () => void
  status: {
    isActive: boolean
    lastCheck: Date
    version?: string
    services?: {
      pos: boolean
      printer: boolean
      medula: boolean
    }
  }
}

export default function LocalServicePopup({ open, onClose, status }: LocalServicePopupProps) {
  const handleTestService = async () => {
    // TODO: Test local service functionality
    document.dispatchEvent(new CustomEvent('showToast', {
      detail: {
        type: 'info',
        title: 'Test Başlatıldı',
        message: 'Local service test ediliyor...',
        duration: 3000
      }
    }))
  }

  const [showProtocolDialog, setShowProtocolDialog] = useState(false)

  const handleStartService = () => {
    // Show beautiful confirmation dialog instead of browser's ugly prompt
    setShowProtocolDialog(true)
  }

  const handleProtocolConfirm = async () => {
    setShowProtocolDialog(false)
    
    try {
      const { startLocalService } = await import('@/utils/localServiceProtocol')
      
      const result = await startLocalService()
      
      if (result.success) {
        // Service başarıyla başlatıldı
        
        // Success toast with action
        document.dispatchEvent(new CustomEvent('showToast', {
          detail: {
            type: 'success',
            title: 'Local Service Başlatıldı',
            message: result.message,
            duration: 4000,
            action: {
              label: 'Tamam',
              onClick: () => {
                onClose()
                window.location.reload()
              }
            }
          }
        }))
        
        // Auto close popup after 2 seconds
        setTimeout(() => {
          onClose()
        }, 2000)
        
      } else {
        // Service başlatılamadı
        
        // Error toast with retry action
        document.dispatchEvent(new CustomEvent('showToast', {
          detail: {
            type: 'error',
            title: 'Servis Başlatılamadı',
            message: result.message || 'Local service başlatılırken bir sorun oluştu. Lütfen manuel olarak başlatmayı deneyin.',
            duration: 8000,
            action: {
              label: 'Tekrar Dene',
              onClick: () => handleStartService()
            }
          }
        }))
      }
    } catch {
      // Kritik hata oluştu
      
      // Critical error toast
      document.dispatchEvent(new CustomEvent('showToast', {
        detail: {
          type: 'error',
          title: 'Beklenmeyen Hata',
          message: 'Servis başlatılırken kritik bir hata oluştu. Sayfayı yenilemeyi deneyin.',
          duration: 10000,
          action: {
            label: 'Sayfayı Yenile',
            onClick: () => window.location.reload()
          }
        }
      }))
    }
  }

  const handleProtocolCancel = () => {
    setShowProtocolDialog(false)
  }

  const handleStopService = async () => {
    try {
      const { stopLocalService } = await import('@/utils/localServiceProtocol')
      
      const result = await stopLocalService()
      
      if (result.success) {
        // Success toast
        document.dispatchEvent(new CustomEvent('showToast', {
          detail: {
            type: 'success',
            title: 'Local Service Durduruldu',
            message: result.message,
            duration: 4000
          }
        }))
        
        // Auto close popup
        setTimeout(() => {
          onClose()
        }, 2000)
        
      } else {
        // Error toast
        document.dispatchEvent(new CustomEvent('showToast', {
          detail: {
            type: 'error',
            title: 'Durdurma Hatası',
            message: result.message,
            duration: 6000
          }
        }))
      }
    } catch {
      // Critical error toast
      document.dispatchEvent(new CustomEvent('showToast', {
        detail: {
          type: 'error',
          title: 'Beklenmeyen Hata',
          message: 'Servis durdurulurken hata oluştu.',
          duration: 8000
        }
      }))
    }
  }

  const formatTime = (date: Date) => {
    return date.toLocaleString('tr-TR', {
      day: '2-digit',
      month: '2-digit', 
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  return (
    <StyledDialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
    >
      <DialogTitle sx={{ 
        display: 'flex', 
        alignItems: 'center', 
        justifyContent: 'space-between',
        pb: 1
      }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          {status.isActive ? (
            <CheckCircleIcon color="success" />
          ) : (
            <ErrorIcon color="error" />
          )}
          <Typography variant="h6" component="div">
            {status.isActive ? 'OPAS Local Service' : 'Sadece Web Modu'}
          </Typography>
        </Box>
        <IconButton onClick={onClose} size="small">
          <CloseIcon />
        </IconButton>
      </DialogTitle>

      <DialogContent>
        {status.isActive ? (
          <Stack spacing={2}>
            <Typography variant="body2" color="text.secondary">
              Local service aktif ve çalışıyor. Tüm özellikler kullanılabilir.
            </Typography>

            <Divider />

            {/* Servis Durumları */}
            <Box>
              <Typography variant="subtitle2" gutterBottom>
                Servis Durumları:
              </Typography>
              
              <ServiceItem>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <CreditCardIcon fontSize="small" color="action" />
                  <Typography variant="body2">POS Cihazı</Typography>
                </Box>
                <Chip 
                  label="Hazır" 
                  color="success" 
                  size="small" 
                  variant="outlined"
                />
              </ServiceItem>

              <ServiceItem>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <PrintIcon fontSize="small" color="action" />
                  <Typography variant="body2">Yazıcı</Typography>
                </Box>
                <Chip 
                  label="Bağlı" 
                  color="success" 
                  size="small" 
                  variant="outlined"
                />
              </ServiceItem>

              <ServiceItem>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <LocalHospitalIcon fontSize="small" color="action" />
                  <Typography variant="body2">MEDULA</Typography>
                </Box>
                <Chip 
                  label="Oturum Açık" 
                  color="success" 
                  size="small" 
                  variant="outlined"
                />
              </ServiceItem>
            </Box>

            <Divider />

            {/* Bilgiler */}
            <Box>
              <Typography variant="body2" color="text.secondary">
                Son kontrol: {formatTime(status.lastCheck)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Versiyon: {status.version || '1.0.0'}
              </Typography>
            </Box>
          </Stack>
        ) : (
          <Stack spacing={2}>
            <Typography variant="body2" color="text.secondary">
              Local service aktif değil. Bazı özellikler kullanılamayacak.
            </Typography>

            <Box sx={{ 
              p: 2, 
              bgcolor: alpha('#ff9800', 0.1), 
              borderRadius: 2,
              border: '1px solid',
              borderColor: alpha('#ff9800', 0.3)
            }}>
              <Typography variant="body2" sx={{ fontWeight: 500, mb: 1 }}>
                ⚠️ Kullanılamayan Özellikler:
              </Typography>
              <Typography variant="body2" component="div">
                • POS cihazı ile ödeme alma<br/>
                • MEDULA sorguları<br/>
                • Otomatik fiş yazdırma
              </Typography>
            </Box>

            <Typography variant="body2" color="text.secondary">
              Son kontrol: {formatTime(status.lastCheck)}
            </Typography>
          </Stack>
        )}
      </DialogContent>

      <DialogActions sx={{ gap: 1, p: 2 }}>
             {status.isActive ? (
               <>
                 <Button 
                   startIcon={<StopIcon />}
                   onClick={handleStopService}
                   variant="outlined"
                   size="small"
                   color="error"
                 >
                   Durdur
                 </Button>
                 <Button 
                   startIcon={<BugReportIcon />}
                   onClick={handleTestService}
                   variant="outlined"
                   size="small"
                 >
                   Test Et
                 </Button>
                 <Button 
                   startIcon={<SettingsIcon />}
                   variant="outlined"
                   size="small"
                 >
                   Ayarlar
                 </Button>
               </>
             ) : (
          <>
            <Button 
              startIcon={<DownloadIcon />}
              onClick={handleStartService}
              variant="contained"
              size="small"
            >
              Servisi Başlat
            </Button>
            <Button 
              startIcon={<HelpIcon />}
              variant="outlined"
              size="small"
            >
              Yardım
            </Button>
          </>
        )}
        <Button onClick={onClose} size="small">
          Kapat
        </Button>
      </DialogActions>

      {/* Beautiful Protocol Confirmation Dialog */}
      <ProtocolConfirmDialog
        open={showProtocolDialog}
        onConfirm={handleProtocolConfirm}
        onCancel={handleProtocolCancel}
        title="OPAS Local Service Başlatılsın mı?"
        message="Bu işlem bilgisayarınızda OPAS Local Service uygulamasını başlatacak ve tüm özellikler aktif hale gelecek."
        protocolUrl="opas://start-service"
      />
    </StyledDialog>
  )
}
