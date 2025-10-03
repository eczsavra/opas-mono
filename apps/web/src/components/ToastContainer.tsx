'use client'

import { 
  Box, 
  Alert, 
  AlertTitle, 
  IconButton, 
  Button,
  Fade,
  Stack,
  useTheme
} from '@mui/material'
import { 
  Close as CloseIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon
} from '@mui/icons-material'
import { styled, alpha } from '@mui/material/styles'
import { Toast } from '@/hooks/useToast'

const ToastWrapper = styled(Box)(({ theme }) => ({
  position: 'fixed',
  top: theme.spacing(3),
  right: theme.spacing(3),
  zIndex: 10000, // Higher than everything
  maxWidth: 400,
  width: '100%',
  [theme.breakpoints.down('sm')]: {
    top: theme.spacing(2),
    right: theme.spacing(2),
    left: theme.spacing(2),
    maxWidth: 'none',
  }
}))

const StyledAlert = styled(Alert)(({ theme }) => ({
  marginBottom: theme.spacing(1),
  borderRadius: 12,
  backdropFilter: 'blur(20px)',
  border: '1px solid',
  boxShadow: `0 8px 32px ${alpha(theme.palette.common.black, 0.12)}`,
  '&.MuiAlert-standardSuccess': {
    backgroundColor: alpha(theme.palette.success.main, 0.1),
    borderColor: alpha(theme.palette.success.main, 0.3),
    color: theme.palette.success.main,
  },
  '&.MuiAlert-standardError': {
    backgroundColor: alpha(theme.palette.error.main, 0.1),
    borderColor: alpha(theme.palette.error.main, 0.3),
    color: theme.palette.error.main,
  },
  '&.MuiAlert-standardWarning': {
    backgroundColor: alpha(theme.palette.warning.main, 0.1),
    borderColor: alpha(theme.palette.warning.main, 0.3),
    color: theme.palette.warning.main,
  },
  '&.MuiAlert-standardInfo': {
    backgroundColor: alpha(theme.palette.info.main, 0.1),
    borderColor: alpha(theme.palette.info.main, 0.3),
    color: theme.palette.info.main,
  },
}))

interface ToastItemProps {
  toast: Toast
  onRemove: (id: string) => void
}

const ToastItem = ({ toast, onRemove }: ToastItemProps) => {
  const theme = useTheme()

  const getIcon = () => {
    switch (toast.type) {
      case 'success': return <SuccessIcon />
      case 'error': return <ErrorIcon />
      case 'warning': return <WarningIcon />
      case 'info': return <InfoIcon />
      default: return <InfoIcon />
    }
  }

  return (
    <Fade in={true} timeout={300}>
      <StyledAlert
        severity={toast.type}
        icon={getIcon()}
        action={
          <Stack direction="row" spacing={1} alignItems="center">
            {toast.action && (
              <Button
                size="small"
                variant="outlined"
                onClick={toast.action.onClick}
                sx={{
                  borderColor: 'currentColor',
                  color: 'currentColor',
                  '&:hover': {
                    backgroundColor: alpha(theme.palette.common.white, 0.1),
                  }
                }}
              >
                {toast.action.label}
              </Button>
            )}
            <IconButton
              size="small"
              onClick={() => onRemove(toast.id)}
              sx={{ color: 'currentColor' }}
            >
              <CloseIcon fontSize="small" />
            </IconButton>
          </Stack>
        }
      >
        <AlertTitle sx={{ fontWeight: 600, mb: toast.message ? 0.5 : 0 }}>
          {toast.title}
        </AlertTitle>
        {toast.message && (
          <Box sx={{ fontSize: '0.875rem', opacity: 0.9 }}>
            {toast.message}
          </Box>
        )}
      </StyledAlert>
    </Fade>
  )
}

interface ToastContainerProps {
  toasts: Toast[]
  onRemove: (id: string) => void
}

export default function ToastContainer({ toasts, onRemove }: ToastContainerProps) {
  if (toasts.length === 0) return null

  return (
    <ToastWrapper>
      <Stack spacing={1}>
        {toasts.map((toast) => (
          <ToastItem
            key={toast.id}
            toast={toast}
            onRemove={onRemove}
          />
        ))}
      </Stack>
    </ToastWrapper>
  )
}
