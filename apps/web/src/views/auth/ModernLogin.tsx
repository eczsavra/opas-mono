'use client'

import { useState, useEffect } from 'react'
import { 
  Box, 
  Paper, 
  Typography, 
  TextField, 
  Button, 
  IconButton, 
  InputAdornment,
  Fade,
  Slide,
  Zoom,
  useTheme,
  alpha,
  Chip,
  Stack
} from '@mui/material'
import { 
  Visibility, 
  VisibilityOff, 
  Login as LoginIcon,
  FingerprintOutlined,
  SecurityOutlined,
  CloudOutlined,
  AutoAwesome,
  Bolt,
  Shield
} from '@mui/icons-material'
import { keyframes } from '@emotion/react'
import { styled } from '@mui/material/styles'

// 🌟 STUNNING ANIMATIONS
const floatingAnimation = keyframes`
  0%, 100% { transform: translateY(0px) rotate(0deg); }
  25% { transform: translateY(-20px) rotate(1deg); }
  50% { transform: translateY(-10px) rotate(-1deg); }
  75% { transform: translateY(-15px) rotate(0.5deg); }
`

const glowPulse = keyframes`
  0%, 100% { box-shadow: 0 0 20px rgba(25, 118, 210, 0.3); }
  50% { box-shadow: 0 0 40px rgba(25, 118, 210, 0.8), 0 0 60px rgba(25, 118, 210, 0.4); }
`

const gradientShift = keyframes`
  0% { background-position: 0% 50%; }
  50% { background-position: 100% 50%; }
  100% { background-position: 0% 50%; }
`

const shimmer = keyframes`
  0% { transform: translateX(-100%); }
  100% { transform: translateX(100%); }
`

// 🎭 STYLED COMPONENTS
const ModernContainer = styled(Box)(({ theme }) => ({
  minHeight: '100vh',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  background: `
    linear-gradient(135deg, 
      ${alpha(theme.palette.primary.main, 0.1)} 0%,
      ${alpha(theme.palette.secondary.main, 0.05)} 25%,
      ${alpha(theme.palette.primary.light, 0.1)} 50%,
      ${alpha(theme.palette.secondary.light, 0.05)} 75%,
      ${alpha(theme.palette.primary.main, 0.1)} 100%
    )
  `,
  backgroundSize: '400% 400%',
  animation: `${gradientShift} 8s ease infinite`,
  position: 'relative',
  overflow: 'hidden',
  '&::before': {
    content: '""',
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    background: `
      radial-gradient(circle at 20% 80%, ${alpha(theme.palette.primary.main, 0.15)} 0%, transparent 50%),
      radial-gradient(circle at 80% 20%, ${alpha(theme.palette.secondary.main, 0.15)} 0%, transparent 50%),
      radial-gradient(circle at 40% 40%, ${alpha(theme.palette.info.main, 0.1)} 0%, transparent 50%)
    `,
  },
}))

const GlassPaper = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(4),
  borderRadius: 24,
  background: `
    linear-gradient(135deg, 
      ${alpha(theme.palette.background.paper, 0.9)} 0%,
      ${alpha(theme.palette.background.paper, 0.7)} 100%
    )
  `,
  backdropFilter: 'blur(20px)',
  border: `1px solid ${alpha(theme.palette.primary.main, 0.1)}`,
  boxShadow: `
    0 8px 32px ${alpha(theme.palette.common.black, 0.1)},
    0 1px 0px ${alpha(theme.palette.common.white, 0.2)} inset,
    0 -1px 0px ${alpha(theme.palette.common.black, 0.1)} inset
  `,
  position: 'relative',
  overflow: 'hidden',
  '&::before': {
    content: '""',
    position: 'absolute',
    top: 0,
    left: '-100%',
    width: '100%',
    height: '100%',
    background: `linear-gradient(90deg, transparent, ${alpha(theme.palette.common.white, 0.1)}, transparent)`,
    animation: `${shimmer} 3s infinite`,
  },
}))

const ModernTextField = styled(TextField)(({ theme }) => ({
  '& .MuiOutlinedInput-root': {
    borderRadius: 16,
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
      animation: `${glowPulse} 2s infinite`,
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

const UltraButton = styled(Button)(({ theme }) => ({
  borderRadius: 16,
  padding: '12px 32px',
  fontSize: '1.1rem',
  fontWeight: 600,
  textTransform: 'none',
  background: `
    linear-gradient(135deg, 
      ${theme.palette.primary.main} 0%, 
      ${theme.palette.primary.dark} 50%,
      ${theme.palette.secondary.main} 100%
    )
  `,
  backgroundSize: '200% 200%',
  animation: `${gradientShift} 3s ease infinite`,
  border: 'none',
  position: 'relative',
  overflow: 'hidden',
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  '&:hover': {
    transform: 'translateY(-2px)',
    boxShadow: `0 8px 25px ${alpha(theme.palette.primary.main, 0.4)}`,
    backgroundSize: '300% 300%',
  },
  '&:active': {
    transform: 'translateY(0)',
  },
  '&::before': {
    content: '""',
    position: 'absolute',
    top: 0,
    left: '-100%',
    width: '100%',
    height: '100%',
    background: `linear-gradient(90deg, transparent, ${alpha(theme.palette.common.white, 0.2)}, transparent)`,
    transition: 'left 0.5s',
  },
  '&:hover::before': {
    left: '100%',
  },
}))

const FloatingIcon = styled(Box)(({ theme }) => ({
  position: 'absolute',
  color: alpha(theme.palette.primary.main, 0.1),
  fontSize: '2rem',
  animation: `${floatingAnimation} 6s ease-in-out infinite`,
  zIndex: 0,
}))

const ModernChip = styled(Chip)(({ theme }) => ({
  background: alpha(theme.palette.primary.main, 0.1),
  border: `1px solid ${alpha(theme.palette.primary.main, 0.2)}`,
  borderRadius: 12,
  backdropFilter: 'blur(10px)',
  '& .MuiChip-label': {
    color: theme.palette.primary.main,
    fontWeight: 500,
  },
}))

export default function ModernLogin() {
  const theme = useTheme()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [mounted, setMounted] = useState(false)
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    setMounted(true)
  }, [])

  const handleLogin = async () => {
    setIsLoading(true)
    // Simüle et
    await new Promise(resolve => setTimeout(resolve, 2000))
    setIsLoading(false)
    console.log('Login attempt:', { email, password })
  }

  if (!mounted) return null

  return (
    <ModernContainer>
      {/* Floating Background Icons */}
      <FloatingIcon sx={{ top: '10%', left: '15%', animationDelay: '0s' }}>
        <SecurityOutlined sx={{ fontSize: '3rem' }} />
      </FloatingIcon>
      <FloatingIcon sx={{ top: '20%', right: '10%', animationDelay: '2s' }}>
        <CloudOutlined sx={{ fontSize: '2.5rem' }} />
      </FloatingIcon>
      <FloatingIcon sx={{ bottom: '15%', left: '10%', animationDelay: '4s' }}>
        <AutoAwesome sx={{ fontSize: '2rem' }} />
      </FloatingIcon>
      <FloatingIcon sx={{ bottom: '25%', right: '20%', animationDelay: '1s' }}>
        <Bolt sx={{ fontSize: '2.5rem' }} />
      </FloatingIcon>

      <Fade in={mounted} timeout={1000}>
        <Box sx={{ position: 'relative', zIndex: 1 }}>
          <Slide direction="up" in={mounted} timeout={800}>
            <GlassPaper
              elevation={0}
              sx={{
                width: { xs: '90vw', sm: 420 },
                maxWidth: 420,
              }}
            >
              {/* Header */}
              <Zoom in={mounted} timeout={1200}>
                <Box sx={{ textAlign: 'center', mb: 4 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'center', mb: 2 }}>
                    <Box
                      sx={{
                        p: 2,
                        borderRadius: '50%',
                        background: `linear-gradient(135deg, ${theme.palette.primary.main}, ${theme.palette.secondary.main})`,
                        animation: `${glowPulse} 3s infinite`,
                      }}
                    >
                      <Shield sx={{ fontSize: '2.5rem', color: 'white' }} />
                    </Box>
                  </Box>
                  <Typography
                    variant="h4"
                    sx={{
                      fontWeight: 700,
                      background: `linear-gradient(135deg, ${theme.palette.primary.main}, ${theme.palette.secondary.main})`,
                      backgroundClip: 'text',
                      WebkitBackgroundClip: 'text',
                      WebkitTextFillColor: 'transparent',
                      mb: 1,
                    }}
                  >
                    OPAS Giriş
                  </Typography>
                  <Typography
                    variant="body1"
                    sx={{
                      color: theme.palette.text.secondary,
                      mb: 3,
                    }}
                  >
                    Modern eczane yönetim sistemine hoş geldiniz
                  </Typography>
                  
                  <Stack direction="row" spacing={1} justifyContent="center" sx={{ mb: 2 }}>
                    <ModernChip
                      icon={<FingerprintOutlined />}
                      label="Güvenli"
                      size="small"
                    />
                    <ModernChip
                      icon={<Bolt />}
                      label="Hızlı"
                      size="small"
                    />
                    <ModernChip
                      icon={<AutoAwesome />}
                      label="Modern"
                      size="small"
                    />
                  </Stack>
                </Box>
              </Zoom>

              {/* Form */}
              <Fade in={mounted} timeout={1500}>
                <Box component="form" sx={{ mb: 3 }}>
                  <ModernTextField
                    fullWidth
                    label="E-posta Adresi"
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    sx={{ mb: 3 }}
                    InputProps={{
                      startAdornment: (
                        <InputAdornment position="start">
                          <Box
                            sx={{
                              p: 0.5,
                              borderRadius: 1,
                              bgcolor: alpha(theme.palette.primary.main, 0.1),
                            }}
                          >
                            📧
                          </Box>
                        </InputAdornment>
                      ),
                    }}
                  />

                  <ModernTextField
                    fullWidth
                    label="Şifre"
                    type={showPassword ? 'text' : 'password'}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    sx={{ mb: 4 }}
                    InputProps={{
                      startAdornment: (
                        <InputAdornment position="start">
                          <Box
                            sx={{
                              p: 0.5,
                              borderRadius: 1,
                              bgcolor: alpha(theme.palette.secondary.main, 0.1),
                            }}
                          >
                            🔐
                          </Box>
                        </InputAdornment>
                      ),
                      endAdornment: (
                        <InputAdornment position="end">
                          <IconButton
                            onClick={() => setShowPassword(!showPassword)}
                            edge="end"
                            sx={{
                              color: theme.palette.text.secondary,
                              '&:hover': {
                                color: theme.palette.primary.main,
                                transform: 'scale(1.1)',
                              },
                            }}
                          >
                            {showPassword ? <VisibilityOff /> : <Visibility />}
                          </IconButton>
                        </InputAdornment>
                      ),
                    }}
                  />

                  <UltraButton
                    fullWidth
                    size="large"
                    onClick={handleLogin}
                    disabled={isLoading}
                    startIcon={isLoading ? '⏳' : <LoginIcon />}
                    sx={{ mb: 2 }}
                  >
                    {isLoading ? 'Giriş Yapılıyor...' : 'Giriş Yap'}
                  </UltraButton>

                  <Box sx={{ textAlign: 'center', mb: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography
                      component="a"
                      href="/forgot-password"
                      sx={{
                        color: theme.palette.primary.main,
                        textDecoration: 'none',
                        fontSize: '0.875rem',
                        fontWeight: 500,
                        '&:hover': {
                          textDecoration: 'underline',
                          color: theme.palette.secondary.main,
                        },
                        transition: 'all 0.3s ease',
                      }}
                    >
                      🔑 Şifremi Unuttum
                    </Typography>
                    
                    <Typography
                      component="a"
                      href="/register"
                      sx={{
                        color: theme.palette.success.main,
                        textDecoration: 'none',
                        fontSize: '0.875rem',
                        fontWeight: 600,
                        '&:hover': {
                          textDecoration: 'underline',
                          color: theme.palette.success.dark,
                        },
                        transition: 'all 0.3s ease',
                      }}
                    >
                      🏪 Kayıt Ol
                    </Typography>
                  </Box>

                  <Box sx={{ textAlign: 'center' }}>
                    <Typography
                      variant="caption"
                      sx={{
                        color: theme.palette.text.secondary,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        gap: 1,
                      }}
                    >
                      <Shield sx={{ fontSize: '1rem' }} />
                      256-bit SSL şifreleme ile korunuyor
                    </Typography>
                  </Box>
                </Box>
              </Fade>
            </GlassPaper>
          </Slide>
        </Box>
      </Fade>
    </ModernContainer>
  )
}
