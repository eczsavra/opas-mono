'use client'

import { useState, useEffect } from 'react'
import { 
  Box, 
  Paper, 
  Typography, 
  TextField, 
  Button, 
  Step,
  StepLabel,
  Stepper,
  Alert,
  Chip,
  Stack,
  IconButton,
  InputAdornment,
  LinearProgress
} from '@mui/material'
import { 
  Email,
  Sms,
  Security,
  Visibility,
  VisibilityOff,
  CheckCircle,
  Shield,
  AutoAwesome,
  ArrowBack,
  Send,
  Lock
} from '@mui/icons-material'
import { keyframes } from '@emotion/react'
import { styled } from '@mui/material/styles'
import { alpha } from '@mui/material'

// Animasyonlar
const floatingAnimation = keyframes`
  0%, 100% { transform: translateY(0px) rotate(0deg); }
  25% { transform: translateY(-15px) rotate(1deg); }
  50% { transform: translateY(-8px) rotate(-1deg); }
  75% { transform: translateY(-12px) rotate(0.5deg); }
`

const glowPulse = keyframes`
  0%, 100% { box-shadow: 0 0 20px rgba(25, 118, 210, 0.2); }
  50% { box-shadow: 0 0 40px rgba(25, 118, 210, 0.6), 0 0 60px rgba(25, 118, 210, 0.3); }
`

const gradientShift = keyframes`
  0% { background-position: 0% 50%; }
  50% { background-position: 100% 50%; }
  100% { background-position: 0% 50%; }
`

// Styled Components
const ModernContainer = styled(Box)(({ theme }) => ({
  minHeight: '100vh',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  background: `
    linear-gradient(135deg, 
      ${alpha(theme.palette.primary.main, 0.1)} 0%,
      ${alpha(theme.palette.secondary.main, 0.05)} 25%,
      ${alpha(theme.palette.error.light, 0.08)} 50%,
      ${alpha(theme.palette.warning.light, 0.05)} 75%,
      ${alpha(theme.palette.primary.main, 0.1)} 100%
    )
  `,
  backgroundSize: '400% 400%',
  animation: `${gradientShift} 10s ease infinite`,
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
      radial-gradient(circle at 20% 80%, ${alpha(theme.palette.primary.main, 0.12)} 0%, transparent 50%),
      radial-gradient(circle at 80% 20%, ${alpha(theme.palette.error.main, 0.1)} 0%, transparent 50%),
      radial-gradient(circle at 40% 40%, ${alpha(theme.palette.warning.main, 0.08)} 0%, transparent 50%)
    `,
  },
}))

const GlassPaper = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(4),
  borderRadius: 24,
  background: `
    linear-gradient(135deg, 
      ${alpha(theme.palette.background.paper, 0.95)} 0%,
      ${alpha(theme.palette.background.paper, 0.8)} 100%
    )
  `,
  backdropFilter: 'blur(20px)',
  border: `1px solid ${alpha(theme.palette.primary.main, 0.15)}`,
  boxShadow: `
    0 8px 32px ${alpha(theme.palette.common.black, 0.1)},
    0 1px 0px ${alpha(theme.palette.common.white, 0.2)} inset
  `,
  position: 'relative',
  overflow: 'hidden',
}))

const FloatingIcon = styled(Box)(({ theme }) => ({
  position: 'absolute',
  color: alpha(theme.palette.primary.main, 0.08),
  fontSize: '2.5rem',
  animation: `${floatingAnimation} 8s ease-in-out infinite`,
  zIndex: 0,
}))

const ModernTextField = styled(TextField)(({ theme }) => ({
  '& .MuiOutlinedInput-root': {
    borderRadius: 16,
    backgroundColor: alpha(theme.palette.background.paper, 0.9),
    backdropFilter: 'blur(10px)',
    border: `1px solid ${alpha(theme.palette.primary.main, 0.15)}`,
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    '&:hover': {
      borderColor: alpha(theme.palette.primary.main, 0.3),
      backgroundColor: alpha(theme.palette.background.paper, 1),
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
}))

const UltraButton = styled(Button)(({ theme }) => ({
  borderRadius: 16,
  padding: '12px 32px',
  fontSize: '1rem',
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
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  '&:hover': {
    transform: 'translateY(-2px)',
    boxShadow: `0 8px 25px ${alpha(theme.palette.primary.main, 0.4)}`,
    backgroundSize: '300% 300%',
  },
}))

// 2024 Güvenli Şifre Kuralları
const passwordRules = [
  { rule: 'En az 12 karakter', regex: /.{12,}/, icon: '📏' },
  { rule: 'Büyük harf (A-Z)', regex: /[A-Z]/, icon: '🔤' },
  { rule: 'Küçük harf (a-z)', regex: /[a-z]/, icon: '🔡' },
  { rule: 'Rakam (0-9)', regex: /[0-9]/, icon: '🔢' },
  { rule: 'Özel karakter (!@#$%^&*)', regex: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/, icon: '🔣' },
  { rule: 'Ardışık karakter yok (123, abc)', regex: /^(?!.*(?:123|234|345|456|567|678|789|890|abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)).*$/i, icon: '🚫' },
  { rule: 'Yaygın şifre değil', regex: /^(?!.*(password|123456|qwerty|admin|welcome|login)).*$/i, icon: '⚠️' }
]

interface Step {
  label: string
  description: string
}

const steps: Step[] = [
  { label: 'Kullanıcı Doğrulama', description: 'Admin eczacı kontrolü' },
  { label: 'SMS/Mail Doğrulama', description: 'Güvenlik kodu gönderimi' },
  { label: 'Yeni Şifre', description: 'Güvenli şifre belirleme' },
]

export default function ForgotPassword() {
  const [mounted, setMounted] = useState(false)
  const [activeStep, setActiveStep] = useState(0)
  const [loading, setLoading] = useState(false)
  
  // Step 1: User Validation
  const [email, setEmail] = useState('')
  const [pharmacyCode, setPharmacyCode] = useState('')
  
  // Step 2: Verification
  const [verificationMethod, setVerificationMethod] = useState<'sms' | 'email'>('email')
  const [verificationCode, setVerificationCode] = useState('')
  const [countdown, setCountdown] = useState(0)
  
  // Step 3: New Password
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)

  useEffect(() => {
    setMounted(true)
  }, [])

  useEffect(() => {
    if (countdown > 0) {
      const timer = setTimeout(() => setCountdown(countdown - 1), 1000)
      return () => clearTimeout(timer)
    }
  }, [countdown])

  const checkPasswordStrength = (password: string) => {
    return passwordRules.map(rule => ({
      ...rule,
      passed: rule.regex.test(password)
    }))
  }

  const getPasswordStrength = (password: string) => {
    const rules = checkPasswordStrength(password)
    const passed = rules.filter(r => r.passed).length
    if (passed <= 2) return { strength: 'Zayıf', color: 'error', progress: 25 }
    if (passed <= 4) return { strength: 'Orta', color: 'warning', progress: 50 }
    if (passed <= 6) return { strength: 'İyi', color: 'info', progress: 75 }
    return { strength: 'Güçlü', color: 'success', progress: 100 }
  }

  const handleNext = async () => {
    setLoading(true)
    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 2000))
    
    if (activeStep === 1 && verificationMethod === 'sms') {
      setCountdown(60) // 60 saniye countdown
    }
    
    setActiveStep((prevStep) => prevStep + 1)
    setLoading(false)
  }

  const handleBack = () => {
    setActiveStep((prevStep) => prevStep - 1)
  }

  const handleSendVerification = async () => {
    setLoading(true)
    // Simulate sending verification code
    await new Promise(resolve => setTimeout(resolve, 1500))
    setCountdown(60)
    setLoading(false)
  }

  const renderStep = () => {
    switch (activeStep) {
      case 0:
        return (
          <Box>
            <Typography variant="h6" sx={{ mb: 3, fontWeight: 600, textAlign: 'center' }}>
              Admin Eczacı Doğrulaması
            </Typography>
            <Stack spacing={3}>
              <ModernTextField
                fullWidth
                label="E-posta Adresi"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Email color="primary" />
                    </InputAdornment>
                  ),
                }}
              />
              <ModernTextField
                fullWidth
                label="Eczane GLN Kodu"
                value={pharmacyCode}
                onChange={(e) => setPharmacyCode(e.target.value)}
                placeholder="868..."
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Shield color="primary" />
                    </InputAdornment>
                  ),
                }}
              />
              <Alert severity="info" sx={{ borderRadius: 2 }}>
                <strong>Admin Eczacı Kontrolü:</strong> Sadece kayıtlı eczane admin kullanıcıları şifre sıfırlayabilir.
              </Alert>
            </Stack>
          </Box>
        )

      case 1:
        return (
          <Box>
            <Typography variant="h6" sx={{ mb: 3, fontWeight: 600, textAlign: 'center' }}>
              Güvenlik Doğrulaması
            </Typography>
            <Stack spacing={3}>
              <Box>
                <Typography variant="subtitle2" sx={{ mb: 2 }}>
                  Doğrulama Yöntemi Seçin:
                </Typography>
                <Stack direction="row" spacing={2}>
                  <Chip
                    icon={<Email />}
                    label="E-posta"
                    clickable
                    color={verificationMethod === 'email' ? 'primary' : 'default'}
                    onClick={() => setVerificationMethod('email')}
                  />
                  <Chip
                    icon={<Sms />}
                    label="SMS"
                    clickable
                    color={verificationMethod === 'sms' ? 'primary' : 'default'}
                    onClick={() => setVerificationMethod('sms')}
                  />
                </Stack>
              </Box>
              
              <ModernTextField
                fullWidth
                label="Doğrulama Kodu"
                value={verificationCode}
                onChange={(e) => setVerificationCode(e.target.value)}
                placeholder="6 haneli kod"
                inputProps={{ maxLength: 6 }}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Security color="primary" />
                    </InputAdornment>
                  ),
                }}
              />
              
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Button
                  variant="outlined"
                  onClick={handleSendVerification}
                  disabled={loading || countdown > 0}
                  startIcon={<Send />}
                  sx={{ borderRadius: 2 }}
                >
                  {countdown > 0 ? `${countdown}s` : 'Kod Gönder'}
                </Button>
                <Typography variant="caption" color="text.secondary">
                  {verificationMethod === 'email' ? '📧 E-posta' : '📱 SMS'} ile kod gönderilecek
                </Typography>
              </Box>
            </Stack>
          </Box>
        )

      case 2:
        const passwordStrength = getPasswordStrength(newPassword)
        const passwordRulesCheck = checkPasswordStrength(newPassword)
        
        return (
          <Box>
            <Typography variant="h6" sx={{ mb: 3, fontWeight: 600, textAlign: 'center' }}>
              Yeni Şifre Belirleme
            </Typography>
            <Stack spacing={3}>
              <ModernTextField
                fullWidth
                label="Yeni Şifre"
                type={showPassword ? 'text' : 'password'}
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Lock color="primary" />
                    </InputAdornment>
                  ),
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton onClick={() => setShowPassword(!showPassword)}>
                        {showPassword ? <VisibilityOff /> : <Visibility />}
                      </IconButton>
                    </InputAdornment>
                  ),
                }}
              />
              
              {newPassword && (
                <Box>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                    <Typography variant="subtitle2">Şifre Gücü:</Typography>
                    <Chip 
                      label={passwordStrength.strength} 
                      color={passwordStrength.color as 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning'}
                      size="small"
                    />
                  </Box>
                  <LinearProgress 
                    variant="determinate" 
                    value={passwordStrength.progress} 
                    color={passwordStrength.color as 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning'}
                    sx={{ borderRadius: 1, height: 6, mb: 2 }}
                  />
                  <Stack spacing={1}>
                    {passwordRulesCheck.map((rule, index) => (
                      <Box key={index} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Box sx={{ fontSize: '1.2rem' }}>
                          {rule.passed ? '✅' : rule.icon}
                        </Box>
                        <Typography 
                          variant="body2" 
                          sx={{ 
                            color: rule.passed ? 'success.main' : 'text.secondary',
                            textDecoration: rule.passed ? 'line-through' : 'none'
                          }}
                        >
                          {rule.rule}
                        </Typography>
                      </Box>
                    ))}
                  </Stack>
                </Box>
              )}

              <ModernTextField
                fullWidth
                label="Şifre Tekrarı"
                type={showConfirmPassword ? 'text' : 'password'}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                error={confirmPassword !== '' && newPassword !== confirmPassword}
                helperText={confirmPassword !== '' && newPassword !== confirmPassword ? 'Şifreler eşleşmiyor' : ''}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <CheckCircle color={newPassword === confirmPassword && confirmPassword !== '' ? 'success' : 'disabled'} />
                    </InputAdornment>
                  ),
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton onClick={() => setShowConfirmPassword(!showConfirmPassword)}>
                        {showConfirmPassword ? <VisibilityOff /> : <Visibility />}
                      </IconButton>
                    </InputAdornment>
                  ),
                }}
              />
            </Stack>
          </Box>
        )

      default:
        return (
          <Box sx={{ textAlign: 'center', py: 4 }}>
            <CheckCircle sx={{ fontSize: 64, color: 'success.main', mb: 2 }} />
            <Typography variant="h5" sx={{ fontWeight: 600, mb: 2 }}>
              Şifre Başarıyla Değiştirildi! 🎉
            </Typography>
            <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
              Artık yeni şifrenizle giriş yapabilirsiniz.
            </Typography>
            <Button
              component="a"
              href="/login2"
              variant="contained"
              startIcon={<ArrowBack />}
              sx={{ borderRadius: 2 }}
            >
              Giriş Sayfasına Dön
            </Button>
          </Box>
        )
    }
  }

  if (!mounted) return null

  return (
    <ModernContainer>
      {/* Floating Background Icons */}
      <FloatingIcon sx={{ top: '15%', left: '10%', animationDelay: '0s' }}>
        <Security />
      </FloatingIcon>
      <FloatingIcon sx={{ top: '25%', right: '15%', animationDelay: '2s' }}>
        <Email />
      </FloatingIcon>
      <FloatingIcon sx={{ bottom: '20%', left: '20%', animationDelay: '4s' }}>
        <Shield />
      </FloatingIcon>
      <FloatingIcon sx={{ bottom: '30%', right: '10%', animationDelay: '1s' }}>
        <AutoAwesome />
      </FloatingIcon>

      <Box sx={{ position: 'relative', zIndex: 1, width: '100%', maxWidth: 500 }}>
        <GlassPaper elevation={0}>
          {/* Header */}
          <Box sx={{ textAlign: 'center', mb: 4 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 2, mb: 2 }}>
              <IconButton 
                component="a" 
                href="/login2"
                sx={{ 
                  bgcolor: 'primary.main', 
                  color: 'white',
                  '&:hover': { bgcolor: 'primary.dark' }
                }}
              >
                <ArrowBack />
              </IconButton>
              <Typography variant="h4" sx={{ fontWeight: 700, color: 'primary.main' }}>
                🔑 Şifremi Unuttum
              </Typography>
            </Box>
            <Typography variant="body2" color="text.secondary">
              Admin eczacı şifre sıfırlama sistemi
            </Typography>
          </Box>

          {/* Progress Stepper */}
          {activeStep < 3 && (
            <Stepper activeStep={activeStep} sx={{ mb: 4 }}>
              {steps.map((step) => (
                <Step key={step.label}>
                  <StepLabel>
                    <Typography variant="caption">{step.label}</Typography>
                  </StepLabel>
                </Step>
              ))}
            </Stepper>
          )}

          {/* Step Content */}
          {renderStep()}

          {/* Navigation Buttons */}
          {activeStep < 3 && (
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 4 }}>
              <Button
                disabled={activeStep === 0}
                onClick={handleBack}
                startIcon={<ArrowBack />}
                sx={{ borderRadius: 2 }}
              >
                Geri
              </Button>
              <UltraButton
                variant="contained"
                onClick={handleNext}
                disabled={loading}
              >
                {loading ? 'İşleniyor...' : activeStep === 2 ? 'Şifreyi Değiştir' : 'İleri'}
              </UltraButton>
            </Box>
          )}
        </GlassPaper>
      </Box>
    </ModernContainer>
  )
}
