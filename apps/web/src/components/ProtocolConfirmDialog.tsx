'use client'

import { 
  Dialog, 
  DialogTitle, 
  DialogContent, 
  DialogActions, 
  Button, 
  Typography, 
  Box, 
  Stack,
  Chip,
  IconButton,
  Slide
} from '@mui/material'
import { 
  Close as CloseIcon,
  Security as SecurityIcon,
  Computer as ComputerIcon,
  PlayArrow as PlayIcon,
  Warning as WarningIcon
} from '@mui/icons-material'
import { styled, alpha } from '@mui/material/styles'
import { TransitionProps } from '@mui/material/transitions'
import React from 'react'

const StyledDialog = styled(Dialog)(({ theme }) => ({
  '& .MuiDialog-paper': {
    borderRadius: 20,
    minWidth: 480,
    maxWidth: 520,
    background: `linear-gradient(135deg, 
      ${alpha(theme.palette.background.paper, 0.95)} 0%,
      ${alpha(theme.palette.background.paper, 0.9)} 100%
    )`,
    backdropFilter: 'blur(20px)',
    border: `1px solid ${alpha(theme.palette.primary.main, 0.2)}`,
    boxShadow: `0 24px 48px ${alpha(theme.palette.common.black, 0.15)}`,
  }
}))

const IconContainer = styled(Box)(({ theme }) => ({
  width: 80,
  height: 80,
  borderRadius: '50%',
  background: `linear-gradient(135deg, ${theme.palette.primary.main}, ${theme.palette.primary.dark})`,
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  margin: '0 auto 16px',
  boxShadow: `0 8px 24px ${alpha(theme.palette.primary.main, 0.3)}`,
  animation: 'pulse 2s infinite',
  '@keyframes pulse': {
    '0%': {
      boxShadow: `0 8px 24px ${alpha(theme.palette.primary.main, 0.3)}`,
    },
    '50%': {
      boxShadow: `0 12px 32px ${alpha(theme.palette.primary.main, 0.5)}`,
    },
    '100%': {
      boxShadow: `0 8px 24px ${alpha(theme.palette.primary.main, 0.3)}`,
    }
  }
}))

const Transition = React.forwardRef(function Transition(
  props: TransitionProps & {
    children: React.ReactElement;
  },
  ref: React.Ref<unknown>,
) {
  return <Slide direction="up" ref={ref} {...props} />;
});

interface ProtocolConfirmDialogProps {
  open: boolean
  onConfirm: () => void
  onCancel: () => void
  title?: string
  message?: string
  protocolUrl?: string
}

export default function ProtocolConfirmDialog({
  open,
  onConfirm,
  onCancel,
  title = "OPAS Local Service Başlatılsın mı?",
  message = "Bu işlem bilgisayarınızda OPAS Local Service uygulamasını başlatacak.",
  protocolUrl = "opas://start-service"
}: ProtocolConfirmDialogProps) {

  return (
    <StyledDialog
      open={open}
      onClose={onCancel}
      TransitionComponent={Transition}
      keepMounted
      maxWidth="sm"
      fullWidth
    >
      <DialogTitle sx={{ 
        textAlign: 'center', 
        pt: 4, 
        pb: 2,
        position: 'relative'
      }}>
        <IconButton
          onClick={onCancel}
          sx={{ 
            position: 'absolute',
            right: 16,
            top: 16,
            color: 'text.secondary'
          }}
        >
          <CloseIcon />
        </IconButton>
        
        <IconContainer>
          <ComputerIcon sx={{ fontSize: 40, color: 'white' }} />
        </IconContainer>
        
        <Typography 
          variant="h5" 
          component="div"
          sx={{ 
            fontWeight: 700,
            mb: 1,
            background: 'linear-gradient(135deg, #1976d2, #42a5f5)',
            backgroundClip: 'text',
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent'
          }}
        >
          {title}
        </Typography>
      </DialogTitle>

      <DialogContent sx={{ px: 4, pb: 2 }}>
        <Stack spacing={3} alignItems="center">
          <Typography 
            variant="body1" 
            sx={{ 
              textAlign: 'center',
              color: 'text.primary',
              fontSize: '1.1rem',
              lineHeight: 1.6
            }}
          >
            {message}
          </Typography>

          <Box sx={{ 
            p: 2, 
            bgcolor: alpha('#2196f3', 0.1), 
            borderRadius: 3,
            border: '1px solid',
            borderColor: alpha('#2196f3', 0.2),
            width: '100%'
          }}>
            <Stack direction="row" spacing={2} alignItems="center">
              <SecurityIcon color="primary" />
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 0.5 }}>
                  Güvenlik Bilgisi
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Bu işlem sadece OPAS uygulamasını başlatır. Sisteminize zarar vermez.
                </Typography>
              </Box>
            </Stack>
          </Box>

          <Box sx={{ 
            p: 2, 
            bgcolor: alpha('#ff9800', 0.1), 
            borderRadius: 3,
            border: '1px solid',
            borderColor: alpha('#ff9800', 0.2),
            width: '100%'
          }}>
            <Stack direction="row" spacing={2} alignItems="center">
              <WarningIcon color="warning" />
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 0.5 }}>
                  Protokol Bilgisi
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  <code style={{ 
                    background: alpha('#ff9800', 0.1), 
                    padding: '2px 6px', 
                    borderRadius: 4,
                    fontSize: '0.85rem'
                  }}>
                    {protocolUrl}
                  </code>
                </Typography>
              </Box>
            </Stack>
          </Box>

          <Stack direction="row" spacing={1}>
            <Chip 
              icon={<PlayIcon />}
              label="Otomatik Başlatma" 
              color="primary" 
              variant="outlined"
              size="small"
            />
            <Chip 
              icon={<SecurityIcon />}
              label="Güvenli İşlem" 
              color="success" 
              variant="outlined"
              size="small"
            />
          </Stack>
        </Stack>
      </DialogContent>

      <DialogActions sx={{ p: 4, pt: 2, gap: 2 }}>
        <Button
          onClick={onCancel}
          variant="outlined"
          size="large"
          sx={{ 
            minWidth: 120,
            borderRadius: 3,
            textTransform: 'none',
            fontWeight: 600
          }}
        >
          İptal
        </Button>
        <Button
          onClick={onConfirm}
          variant="contained"
          size="large"
          startIcon={<PlayIcon />}
          sx={{ 
            minWidth: 160,
            borderRadius: 3,
            textTransform: 'none',
            fontWeight: 600,
            background: 'linear-gradient(135deg, #1976d2, #42a5f5)',
            '&:hover': {
              background: 'linear-gradient(135deg, #1565c0, #1976d2)',
            }
          }}
        >
          Servisi Başlat
        </Button>
      </DialogActions>
    </StyledDialog>
  )
}
