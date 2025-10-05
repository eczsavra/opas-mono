'use client'

import { useState, useEffect } from 'react'
import { 
  Box, 
  Paper, 
  Typography, 
  TextField, 
  Button, 
  Alert,
  Chip,
  Stack,
  InputAdornment,
  CircularProgress,
  Fade,
  Zoom,
  Card,
  CardContent,
  IconButton
} from '@mui/material'
import { 
  Verified,
  Store,
  LocationOn,
  Person,
  Lock,
  Phone,
  ArrowBack,
  Search,
  CheckCircle,
  Error,
  AutoAwesome,
  Shield,
  Business,
  ThumbUp,
  ThumbDown,
  ContactSupport,
  Email,
  Warning,
  Visibility,
  VisibilityOff
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
  0%, 100% { box-shadow: 0 0 20px rgba(76, 175, 80, 0.2); }
  50% { box-shadow: 0 0 40px rgba(76, 175, 80, 0.6), 0 0 60px rgba(76, 175, 80, 0.3); }
`

const gradientShift = keyframes`
  0% { background-position: 0% 50%; }
  50% { background-position: 100% 50%; }
  100% { background-position: 0% 50%; }
`

const successPulse = keyframes`
  0% { transform: scale(1); }
  50% { transform: scale(1.05); }
  100% { transform: scale(1); }
`

// Styled Components
const ModernContainer = styled(Box)(({ theme }) => ({
  minHeight: '100vh',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  background: `
    linear-gradient(135deg, 
      ${alpha(theme.palette.success.main, 0.1)} 0%,
      ${alpha(theme.palette.primary.main, 0.08)} 25%,
      ${alpha(theme.palette.info.light, 0.1)} 50%,
      ${alpha(theme.palette.secondary.light, 0.05)} 75%,
      ${alpha(theme.palette.success.main, 0.08)} 100%
    )
  `,
  backgroundSize: '400% 400%',
  animation: `${gradientShift} 12s ease infinite`,
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
      radial-gradient(circle at 15% 85%, ${alpha(theme.palette.success.main, 0.12)} 0%, transparent 50%),
      radial-gradient(circle at 85% 15%, ${alpha(theme.palette.primary.main, 0.1)} 0%, transparent 50%),
      radial-gradient(circle at 50% 50%, ${alpha(theme.palette.info.main, 0.08)} 0%, transparent 50%)
    `,
  },
}))

const GlassPaper = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(4),
  borderRadius: 24,
  background: `
    linear-gradient(135deg, 
      ${alpha(theme.palette.background.paper, 0.95)} 0%,
      ${alpha(theme.palette.background.paper, 0.85)} 100%
    )
  `,
  backdropFilter: 'blur(20px)',
  border: `1px solid ${alpha(theme.palette.success.main, 0.2)}`,
  boxShadow: `
    0 8px 32px ${alpha(theme.palette.common.black, 0.1)},
    0 1px 0px ${alpha(theme.palette.common.white, 0.2)} inset
  `,
  position: 'relative',
  overflow: 'hidden',
}))

const FloatingIcon = styled(Box)(({ theme }) => ({
  position: 'absolute',
  color: alpha(theme.palette.success.main, 0.08),
  fontSize: '2.5rem',
  animation: `${floatingAnimation} 8s ease-in-out infinite`,
  zIndex: 0,
}))

const ModernTextField = styled(TextField)(({ theme }) => ({
  '& .MuiOutlinedInput-root': {
    borderRadius: 16,
    backgroundColor: alpha(theme.palette.background.paper, 0.9),
    backdropFilter: 'blur(10px)',
    border: `1px solid ${alpha(theme.palette.success.main, 0.2)}`,
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    '&:hover': {
      borderColor: alpha(theme.palette.success.main, 0.4),
      backgroundColor: alpha(theme.palette.background.paper, 1),
      transform: 'translateY(-1px)',
      boxShadow: `0 4px 20px ${alpha(theme.palette.success.main, 0.15)}`,
    },
    '&.Mui-focused': {
      borderColor: theme.palette.success.main,
      backgroundColor: alpha(theme.palette.background.paper, 1),
      boxShadow: `0 0 0 3px ${alpha(theme.palette.success.main, 0.1)}`,
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
      ${theme.palette.success.main} 0%, 
      ${theme.palette.success.dark} 50%,
      ${theme.palette.primary.main} 100%
    )
  `,
  backgroundSize: '200% 200%',
  animation: `${gradientShift} 3s ease infinite`,
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  '&:hover': {
    transform: 'translateY(-2px)',
    boxShadow: `0 8px 25px ${alpha(theme.palette.success.main, 0.4)}`,
    backgroundSize: '300% 300%',
  },
}))

const PharmacyCard = styled(Card)(({ theme }) => ({
  borderRadius: 16,
  border: `2px solid ${theme.palette.success.main}`,
  background: `
    linear-gradient(135deg, 
      ${alpha(theme.palette.success.light, 0.1)} 0%,
      ${alpha(theme.palette.background.paper, 0.95)} 100%
    )
  `,
  animation: `${successPulse} 2s ease infinite`,
  position: 'relative',
  overflow: 'hidden',
  '&::before': {
    content: '""',
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    background: `linear-gradient(45deg, transparent, ${alpha(theme.palette.success.main, 0.1)}, transparent)`,
    animation: `${gradientShift} 3s ease infinite`,
  }
}))

const ConfirmButton = styled(Button)(({ theme }) => ({
  borderRadius: 12,
  padding: '12px 24px',
  fontSize: '1rem',
  fontWeight: 600,
  textTransform: 'none',
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  '&:hover': {
    transform: 'translateY(-2px)',
    boxShadow: `0 8px 25px ${alpha(theme.palette.primary.main, 0.3)}`,
  },
}))

const RejectButton = styled(Button)(({ theme }) => ({
  borderRadius: 12,
  padding: '12px 24px',
  fontSize: '1rem',
  fontWeight: 600,
  textTransform: 'none',
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  '&:hover': {
    transform: 'translateY(-2px)',
    boxShadow: `0 8px 25px ${alpha(theme.palette.error.main, 0.3)}`,
  },
}))

interface PharmacyData {
  gln: string
  companyName: string | null
  city: string | null
  town: string | null
  address: string | null
  active: boolean | null
  pharmacyRegistrationNo?: string | null
}

interface Step {
  label: string
  description: string
  icon: React.ReactNode
}

const steps: Step[] = [
  { 
    label: 'GLN Doğrulama', 
    description: 'GLN numarası kontrolü',
    icon: <Verified />
  },
  { 
    label: 'Eczacı Bilgileri', 
    description: 'Kimlik bilgileri',
    icon: <Person />
  },
  { 
    label: 'Email Doğrulama', 
    description: 'Email doğrulama',
    icon: <Email />
  },
  { 
    label: 'Telefon Doğrulama', 
    description: 'SMS doğrulama',
    icon: <Phone />
  },
  { 
    label: 'Kullanıcı Bilgileri', 
    description: 'Kullanıcı adı ve parola',
    icon: <Shield />
  },
  { 
    label: 'Kayıt Tamamlandı', 
    description: 'Kayıt başarılı',
    icon: <CheckCircle />
  }
]

export default function Registration() {
  const [mounted, setMounted] = useState(false)
  const [activeStep, setActiveStep] = useState(0)
  const [loading, setLoading] = useState(false)
  const [showLongProcessWarning, setShowLongProcessWarning] = useState(false)
  
  // Step 1: GLN Validation
  const [gln, setGln] = useState('')
  const [glnValidation, setGlnValidation] = useState<{
    isValid: boolean | null
    data: PharmacyData | null
    error: string | null
    confirmed: boolean | null
  }>({
    isValid: null,
    data: null,
    error: null,
    confirmed: null
  })
  
  const [showContactMessage, setShowContactMessage] = useState(false)
  
  // Step 2: Pharmacist Information
  const [pharmacistInfo, setPharmacistInfo] = useState({
    firstName: '',
    lastName: '',
    tcNumber: '',
    birthYear: '',
    nviValidated: false
  })
  
  const [pharmacistValidation, setPharmacistValidation] = useState({
    firstName: { isValid: true, error: '' },
    lastName: { isValid: true, error: '' },
    tcNumber: { isValid: true, error: '' },
    birthYear: { isValid: true, error: '' }
  })

  // Step 3: Email Verification
  const [emailInfo, setEmailInfo] = useState({
    email: '',
    verificationCode: '',
    isEmailSent: false,
    isEmailVerified: false,
    canResend: true,
    resendCountdown: 0
  })
  
  const [emailValidation, setEmailValidation] = useState({
    email: { isValid: true, error: '' },
    verificationCode: { isValid: true, error: '' }
  })
  
  const [emailError, setEmailError] = useState<{
    show: boolean
    message: string
    type: 'error' | 'warning' | 'info'
  }>({
    show: false,
    message: '',
    type: 'error'
  })

  // Step 4: Phone Verification
  const [phoneInfo, setPhoneInfo] = useState({
    phone: '',
    verificationCode: '',
    isSmsSent: false,
    isSmsVerified: false,
    canResend: true,
    resendCountdown: 0
  })
  
  const [phoneValidation, setPhoneValidation] = useState({
    phone: { isValid: true, error: '' },
    verificationCode: { isValid: true, error: '' }
  })
  
  const [phoneError, setPhoneError] = useState<{
    show: boolean
    message: string
    type: 'error' | 'warning' | 'info'
  }>({
    show: false,
    message: '',
    type: 'error'
  })

  // Step 5: User Credentials
  const [credentialsInfo, setCredentialsInfo] = useState({
    username: '',
    password: '',
    confirmPassword: '',
    isUsernameAvailable: null as boolean | null,
    isUsernameChecking: false
  })

  // Password visibility states
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  
  const [credentialsValidation, setCredentialsValidation] = useState({
    username: { isValid: true, error: '' },
    password: { isValid: true, error: '' },
    confirmPassword: { isValid: true, error: '' }
  })
  
  const [credentialsError, setCredentialsError] = useState<{
    show: boolean
    message: string
    type: 'error' | 'warning' | 'info'
  }>({
    show: false,
    message: '',
    type: 'error'
  })

  // Registration final states
  const [registrationSuccess, setRegistrationSuccess] = useState({
    show: false,
    pharmacistId: '',
    tenantId: '',
    username: '',
    message: ''
  })

  // Countdown for redirect
  const [countdown, setCountdown] = useState(10)

  const [registrationError, setRegistrationError] = useState({
    show: false,
    message: ''
  })

  useEffect(() => {
    setMounted(true)
  }, [])

  // İlk 3 hane (868) otomatik doldur
  useEffect(() => {
    if (gln.length === 0) {
      setGln('868')
    }
  }, [gln.length])

  // Countdown and redirect after successful registration
  useEffect(() => {
    if (registrationSuccess.show && countdown > 0) {
      const timer = setTimeout(() => {
        setCountdown(countdown - 1)
      }, 1000)
      return () => clearTimeout(timer)
    } else if (registrationSuccess.show && countdown === 0) {
      // ⚠️ CRITICAL: Clear ALL storage for this GLN before redirecting to login
      // This prevents old tenant data from appearing for newly registered tenants with same GLN
      console.log(`🧹 Clearing all storage for new tenant registration: ${gln}`)
      
      // Clear everything (auth will be fresh after login anyway)
      localStorage.clear()
      sessionStorage.clear()
      
      // Also clear IndexedDB (cached products)
      try {
        if (typeof window !== 'undefined' && window.indexedDB) {
          window.indexedDB.deleteDatabase('opasDB')
          console.log('🧹 IndexedDB cleared')
        }
      } catch (err) {
        console.warn('IndexedDB clear failed (non-critical):', err)
      }
      
      window.location.href = '/t-login'
    }
  }, [registrationSuccess.show, countdown, gln])

  const validateGLN = async (glnValue: string) => {
    if (!glnValue || glnValue.length !== 13) {
      setGlnValidation({
        isValid: false,
        data: null,
        error: 'GLN 13 haneli olmalıdır',
        confirmed: null
      })
      return
    }

    if (!glnValue.startsWith('868')) {
      setGlnValidation({
        isValid: false,
        data: null,
        error: 'GLN 868 ile başlamalıdır',
        confirmed: null
      })
      return
    }

    setLoading(true)
    
    try {
      // GLN Doğrulama API çağrısı
      const response = await fetch(`/api/opas/validate-gln?gln=${glnValue}`)
      
      if (!response.ok) {
        throw new (globalThis.Error)('Doğrulama başarısız')
      }
      
      const result = await response.json()
      
      if (result.ok && result.data) {
        setGlnValidation({
          isValid: true,
          data: result.data,
          error: null,
          confirmed: null
        })
      } else {
        setGlnValidation({
          isValid: false,
          data: null,
          error: result.error || 'Bu GLN kayıtlı değil veya aktif değil',
          confirmed: null
        })
      }
    } catch {
      setGlnValidation({
        isValid: false,
        data: null,
        error: 'Doğrulama sırasında bir hata oluştu',
        confirmed: null
      })
    } finally {
      setLoading(false)
    }
  }

  // handleGLNChange removed - now using handleGLNDigitChange for individual digits

  // Yeni GLN digit handler'ları
  const handleGLNDigitChange = (index: number, value: string) => {
    // Sadece rakam kabul et
    const numericValue = value.replace(/\D/g, '')
    if (numericValue.length > 1) return
    
    // GLN string'ini güncelle
    const newGln = gln.split('')
    newGln[index] = numericValue
    setGln(newGln.join(''))
    
    // Reset validation state
    if (glnValidation.isValid !== null) {
      setGlnValidation({
        isValid: null,
        data: null,
        error: null,
        confirmed: null
      })
      setShowContactMessage(false)
    }
  }

  const handleGLNKeyDown = (index: number, e: React.KeyboardEvent) => {
    // Backspace tuşu
    if (e.key === 'Backspace' && !gln[index]) {
      if (index > 3) { // İlk 3 hane sabit
        const prevInput = document.querySelector(`input[data-index="${index - 1}"]`) as HTMLInputElement
        if (prevInput) {
          prevInput.focus()
          prevInput.select()
        }
      }
    }
    // Rakam tuşu
    else if (e.key >= '0' && e.key <= '9') {
      if (index < 12) { // Son kutucuk değilse
        setTimeout(() => {
          const nextInput = document.querySelector(`input[data-index="${index + 1}"]`) as HTMLInputElement
          if (nextInput) {
            nextInput.focus()
            nextInput.select()
          }
        }, 0)
      }
    }
  }

  const handleGLNValidation = () => {
    validateGLN(gln)
  }

  const handleConfirmPharmacy = () => {
    setGlnValidation(prev => ({
      ...prev,
      confirmed: true
    }))
    // Automatically proceed to next step after confirmation
    setTimeout(() => {
      setActiveStep(1)
    }, 800)
  }

  const handleRejectPharmacy = () => {
    setGlnValidation(prev => ({
      ...prev,
      confirmed: false
    }))
    setShowContactMessage(true)
  }

  // TC Kimlik validation algorithm
  const validateTCNumber = (tc: string): boolean => {
    if (tc.length !== 11) return false
    if (!/^\d{11}$/.test(tc)) return false
    if (tc[0] === '0') return false
    
    const digits = tc.split('').map(Number)
    const sum1 = digits[0] + digits[2] + digits[4] + digits[6] + digits[8]
    const sum2 = digits[1] + digits[3] + digits[5] + digits[7]
    const check1 = (sum1 * 7 - sum2) % 10
    const check2 = (digits.slice(0, 10).reduce((a, b) => a + b, 0)) % 10
    
    return check1 === digits[9] && check2 === digits[10]
  }

  const validatePharmacistField = (field: string, value: string) => {
    switch (field) {
      case 'firstName':
      case 'lastName':
        if (!value.trim()) {
          return { isValid: false, error: 'Bu alan zorunludur' }
        }
        if (value.trim().length < 2) {
          return { isValid: false, error: 'En az 2 karakter olmalı' }
        }
        if (!/^[a-zA-ZçÇğĞıİöÖşŞüÜ\s]+$/.test(value)) {
          return { isValid: false, error: 'Sadece harf kullanabilirsiniz' }
        }
        return { isValid: true, error: '' }
      
      case 'tcNumber':
        if (!value.trim()) {
          return { isValid: false, error: 'TC kimlik numarası zorunludur' }
        }
        if (!validateTCNumber(value)) {
          return { isValid: false, error: 'Geçerli bir TC kimlik numarası giriniz' }
        }
        return { isValid: true, error: '' }
      
      case 'birthYear':
        if (!value.trim()) {
          return { isValid: false, error: 'Doğum yılı zorunludur' }
        }
        const year = parseInt(value)
        const currentYear = new Date().getFullYear()
        if (year < 1940 || year > currentYear - 18) {
          return { isValid: false, error: 'Geçerli bir doğum yılı giriniz (18+ yaş)' }
        }
        return { isValid: true, error: '' }
      
      default:
        return { isValid: true, error: '' }
    }
  }

  const handlePharmacistInfoChange = (field: string, value: string) => {
    // Update the value
    setPharmacistInfo(prev => ({
      ...prev,
      [field]: value,
      nviValidated: false // Reset NVI validation on any change
    }))
    
    // Clear NVI error when user starts typing
    if (nviError.show) {
      setNviError({ show: false, message: '', type: 'error' })
    }
    
    // Validate the field
    const validation = validatePharmacistField(field, value)
    setPharmacistValidation(prev => ({
      ...prev,
      [field]: validation
    }))
  }

  const isPharmacistInfoValid = () => {
    const { firstName, lastName, tcNumber, birthYear } = pharmacistInfo
    const validation = pharmacistValidation
    
    return firstName.trim() && lastName.trim() && tcNumber.trim() && birthYear.trim() &&
           validation.firstName.isValid && validation.lastName.isValid && 
           validation.tcNumber.isValid && validation.birthYear.isValid
  }

  // Email validation functions
  const validateEmail = (email: string) => {
    if (!email.trim()) {
      return { isValid: false, error: 'Email adresi zorunludur' }
    }
    
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(email)) {
      return { isValid: false, error: 'Geçerli bir email adresi giriniz' }
    }
    
    return { isValid: true, error: '' }
  }

  const validateVerificationCode = (code: string) => {
    if (!code.trim()) {
      return { isValid: false, error: 'Doğrulama kodu zorunludur' }
    }
    
    if (code.length !== 6) {
      return { isValid: false, error: 'Doğrulama kodu 6 haneli olmalıdır' }
    }
    
    if (!/^\d{6}$/.test(code)) {
      return { isValid: false, error: 'Doğrulama kodu sadece rakamlardan oluşmalıdır' }
    }
    
    return { isValid: true, error: '' }
  }

  const handleEmailChange = (value: string) => {
    setEmailInfo(prev => ({
      ...prev,
      email: value,
      isEmailSent: false,
      isEmailVerified: false
    }))
    
    // Clear email error when user starts typing
    if (emailError.show) {
      setEmailError({ show: false, message: '', type: 'error' })
    }
    
    const validation = validateEmail(value)
    setEmailValidation(prev => ({
      ...prev,
      email: validation
    }))
  }

  // handleVerificationCodeChange removed - now using handleEmailCodeDigitChange for individual digits

  // Email code digit handlers
  const handleEmailCodeDigitChange = (index: number, value: string) => {
    // Sadece rakam kabul et
    const numericValue = value.replace(/\D/g, '')
    if (numericValue.length > 1) return
    
    // Email code string'ini güncelle
    const newCode = emailInfo.verificationCode.split('')
    newCode[index] = numericValue
    setEmailInfo(prev => ({
      ...prev,
      verificationCode: newCode.join('')
    }))
    
    // Validation'ı güncelle
    const validation = validateVerificationCode(newCode.join(''))
    setEmailValidation(prev => ({
      ...prev,
      verificationCode: validation
    }))
  }

  const handleEmailCodeKeyDown = (index: number, e: React.KeyboardEvent) => {
    // Backspace tuşu
    if (e.key === 'Backspace' && !emailInfo.verificationCode[index]) {
      if (index > 0) {
        const prevInput = document.querySelector(`input[data-index="${index - 1}"]`) as HTMLInputElement
        if (prevInput) {
          prevInput.focus()
          prevInput.select()
        }
      }
    }
    // Rakam tuşu
    else if (e.key >= '0' && e.key <= '9') {
      if (index < 5) { // Son kutucuk değilse
        setTimeout(() => {
          const nextInput = document.querySelector(`input[data-index="${index + 1}"]`) as HTMLInputElement
          if (nextInput) {
            nextInput.focus()
            nextInput.select()
          }
        }, 0)
      }
    }
  }

  const isEmailInfoValid = () => {
    const { email } = emailInfo
    const validation = emailValidation
    
    return email.trim() && validation.email.isValid
  }

         const [nviError, setNviError] = useState<{
           show: boolean
           message: string
           type: 'error' | 'warning' | 'info'
         }>({
           show: false,
           message: '',
           type: 'error'
         })

         const handleNviValidation = async () => {
           setLoading(true)
           setNviError({ show: false, message: '', type: 'error' }) // Clear previous errors
           
           try {
             const response = await fetch('/api/opas/nvi/validate', {
               method: 'POST',
               headers: {
                 'Content-Type': 'application/json',
               },
               body: JSON.stringify({
                 tcNumber: pharmacistInfo.tcNumber,
                 firstName: pharmacistInfo.firstName,
                 lastName: pharmacistInfo.lastName,
                 birthYear: parseInt(pharmacistInfo.birthYear)
               })
             })

             const result = await response.json()

             if (result.ok) {
               setPharmacistInfo(prev => ({
                 ...prev,
                 nviValidated: true
               }))
               setNviError({ show: false, message: '', type: 'error' })
             } else {
               // Şık hata mesajı göster
               const errorMessage = result.error || 'Kimlik bilgileri NVI kayıtları ile eşleşmiyor'
               let userFriendlyMessage = ''
               let errorType: 'error' | 'warning' | 'info' = 'error'

               if (errorMessage.includes('eşleşmiyor') || errorMessage.includes('bulunamadı')) {
                 userFriendlyMessage = '❌ Girdiğiniz kimlik bilgileri T.C. Nüfus Müdürlüğü kayıtları ile eşleşmiyor.\n\n✅ Lütfen bilgileri kontrol ediniz:\n• Adınız ve soyadınız kimlik kartınızdaki gibi mi?\n• TC kimlik numaranızı doğru girdiniz mi?\n• Doğum yılınız doğru mu?'
                 errorType = 'warning'
               } else if (errorMessage.includes('hata') || errorMessage.includes('servis')) {
                 userFriendlyMessage = '⚠️ T.C. Nüfus Müdürlüğü sistemine ulaşılamıyor.\n\n🔄 Lütfen birkaç dakika sonra tekrar deneyin.'
                 errorType = 'info'
               } else {
                 userFriendlyMessage = `❌ Kimlik doğrulama başarısız:\n${errorMessage}`
                 errorType = 'error'
               }

               setNviError({
                 show: true,
                 message: userFriendlyMessage,
                 type: errorType
               })
             }
           } catch (error) {
             console.error('NVI validation error:', error)
             setNviError({
               show: true,
               message: '🌐 İnternet bağlantınızı kontrol edin ve tekrar deneyin.\n\n💡 Sorun devam ederse, sistem yöneticinize başvurun.',
               type: 'error'
             })
           } finally {
             setLoading(false)
           }
         }

  // Email sending and verification functions
  const handleSendVerificationCode = async () => {
    setLoading(true)
    setEmailError({ show: false, message: '', type: 'error' })
    
    try {
      const response = await fetch('/api/opas/email/send-verification', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email: emailInfo.email,
          recipientName: `${pharmacistInfo.firstName} ${pharmacistInfo.lastName}`,
          pharmacyGln: glnValidation.data?.gln
        })
      })

      const responseText = await response.text()
      if (!responseText.trim()) {
        throw new (globalThis.Error)('Empty response from server')
      }

      const result = JSON.parse(responseText)

      if (result.ok) {
        setEmailInfo(prev => ({
          ...prev,
          isEmailSent: true,
          canResend: false,
          resendCountdown: 180 // 180 saniye (3 dakika) bekleme
        }))
        
        // Countdown timer
        const countdownInterval = setInterval(() => {
          setEmailInfo(prev => {
            if (prev.resendCountdown <= 1) {
              clearInterval(countdownInterval)
              return { ...prev, canResend: true, resendCountdown: 0 }
            }
            return { ...prev, resendCountdown: prev.resendCountdown - 1 }
          })
        }, 1000)
        
      } else {
        // API'den gelen error mesajını işle (email gönderimi için)
        setEmailError({
          show: true,
          message: result.error || 'Email gönderimi başarısız oldu',
          type: 'error'
        })
      }
    } catch (error) {
      console.error('Email send error:', error)
      setEmailError({
        show: true,
        message: '📧 Email gönderim sırasında bir hata oluştu.\n\n🔄 Lütfen internet bağlantınızı kontrol edin ve tekrar deneyin.',
        type: 'error'
      })
    } finally {
      setLoading(false)
    }
  }

  const handleVerifyCode = async () => {
    setLoading(true)
    setEmailError({ show: false, message: '', type: 'error' })
    
    try {
      const response = await fetch('/api/opas/email/verify-code', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email: emailInfo.email,
          code: emailInfo.verificationCode
        })
      })

      const responseText = await response.text()
      if (!responseText.trim()) {
        throw new (globalThis.Error)('Empty response from server')
      }

      const result = JSON.parse(responseText)

      if (result.ok) {
        setEmailInfo(prev => ({
          ...prev,
          isEmailVerified: true
        }))
        setEmailError({ show: false, message: '', type: 'error' })
      } else {
        // API'den gelen error mesajını işle (yanlış kod için)
        let userFriendlyMessage = ''
        let errorType: 'error' | 'warning' | 'info' = 'error'

        if (result.error?.includes('expired') || result.error?.includes('süresi')) {
          userFriendlyMessage = '⏰ Doğrulama kodunun süresi dolmuş.\n\n🔄 Yeni bir kod gönderilmesini isteyin.'
          errorType = 'warning'
        } else if (result.error?.includes('invalid') || result.error?.includes('yanlış')) {
          userFriendlyMessage = '❌ Doğrulama kodu hatalı.\n\n✅ Lütfen emailinizdeki 6 haneli kodu kontrol edin ve tekrar deneyin.'
          errorType = 'warning'
        } else {
          userFriendlyMessage = `❌ Doğrulama başarısız:\n${result.error || 'Bilinmeyen hata'}`
          errorType = 'error'
        }

        setEmailError({
          show: true,
          message: userFriendlyMessage,
          type: errorType
        })
      }
    } catch (error) {
      console.error('Email verify error:', error)
      setEmailError({
        show: true,
        message: '🌐 Doğrulama sırasında bir hata oluştu.\n\n💡 Lütfen tekrar deneyin.',
        type: 'error'
      })
    } finally {
      setLoading(false)
    }
  }

  // Phone validation functions
  const validatePhone = (phone: string) => {
    if (!phone.trim()) {
      return { isValid: false, error: 'Telefon numarası zorunludur' }
    }
    
    // Türkiye cep telefonu formatı: 5XX XXX XX XX
    const cleanPhone = phone.replace(/[\s\-\+()]/g, '').replace(/^90/, '').replace(/^0/, '')
    const phoneRegex = /^5[0-9]{9}$/
    
    if (!phoneRegex.test(cleanPhone)) {
      return { isValid: false, error: 'Geçerli bir cep telefonu numarası giriniz (5XX XXX XX XX)' }
    }
    
    return { isValid: true, error: '' }
  }

  const validateSmsCode = (code: string) => {
    if (!code.trim()) {
      return { isValid: false, error: 'SMS doğrulama kodu zorunludur' }
    }
    
    if (code.length !== 6) {
      return { isValid: false, error: 'SMS doğrulama kodu 6 haneli olmalıdır' }
    }
    
    if (!/^\d{6}$/.test(code)) {
      return { isValid: false, error: 'SMS doğrulama kodu sadece rakamlardan oluşmalıdır' }
    }
    
    return { isValid: true, error: '' }
  }

  const handlePhoneChange = (value: string) => {
    // Sadece rakamları al
    let digitsOnly = value.replace(/[^\d]/g, '')
    
    // Türkiye ülke kodu prefix'lerini temizle
    if (digitsOnly.startsWith('90')) {
      digitsOnly = digitsOnly.substring(2)
    }
    if (digitsOnly.startsWith('0')) {
      digitsOnly = digitsOnly.substring(1)
    }
    
    // 10 haneden fazla girişi engelle
    if (digitsOnly.length > 10) {
      digitsOnly = digitsOnly.substring(0, 10)
    }
    
    // Basit formatla - sadece space ekle
    let formattedPhone = digitsOnly
    if (digitsOnly.length > 3) {
      formattedPhone = digitsOnly.substring(0, 3) + ' ' + digitsOnly.substring(3)
    }
    if (digitsOnly.length > 6) {
      formattedPhone = digitsOnly.substring(0, 3) + ' ' + digitsOnly.substring(3, 6) + ' ' + digitsOnly.substring(6)
    }
    if (digitsOnly.length > 8) {
      formattedPhone = digitsOnly.substring(0, 3) + ' ' + digitsOnly.substring(3, 6) + ' ' + 
                       digitsOnly.substring(6, 8) + ' ' + digitsOnly.substring(8)
    }
    
    setPhoneInfo(prev => ({
      ...prev,
      phone: formattedPhone,
      isSmsSent: false,
      isSmsVerified: false
    }))
    
    // Clear phone error when user starts typing
    if (phoneError.show) {
      setPhoneError({ show: false, message: '', type: 'error' })
    }
    
    const validation = validatePhone(formattedPhone)
    setPhoneValidation(prev => ({
      ...prev,
      phone: validation
    }))
  }

  // handleSmsCodeChange removed - now using handleSmsCodeDigitChange for individual digits

  // SMS code digit handlers
  const handleSmsCodeDigitChange = (index: number, value: string) => {
    // Sadece rakam kabul et
    const numericValue = value.replace(/\D/g, '')
    if (numericValue.length > 1) return
    
    // SMS code string'ini güncelle
    const newCode = phoneInfo.verificationCode.split('')
    newCode[index] = numericValue
    setPhoneInfo(prev => ({
      ...prev,
      verificationCode: newCode.join('')
    }))
    
    // Validation'ı güncelle
    const validation = validateSmsCode(newCode.join(''))
    setPhoneValidation(prev => ({
      ...prev,
      verificationCode: validation
    }))
  }

  const handleSmsCodeKeyDown = (index: number, e: React.KeyboardEvent) => {
    // Backspace tuşu
    if (e.key === 'Backspace' && !phoneInfo.verificationCode[index]) {
      if (index > 0) {
        const prevInput = document.querySelector(`input[data-index="${index - 1}"]`) as HTMLInputElement
        if (prevInput) {
          prevInput.focus()
          prevInput.select()
        }
      }
    }
    // Rakam tuşu
    else if (e.key >= '0' && e.key <= '9') {
      if (index < 5) { // Son kutucuk değilse
        setTimeout(() => {
          const nextInput = document.querySelector(`input[data-index="${index + 1}"]`) as HTMLInputElement
          if (nextInput) {
            nextInput.focus()
            nextInput.select()
          }
        }, 0)
      }
    }
  }

  const isPhoneInfoValid = () => {
    const { phone } = phoneInfo
    const validation = phoneValidation
    
    return phone.trim() && validation.phone.isValid
  }

  // SMS sending and verification functions
  const handleSendSmsCode = async () => {
    setLoading(true)
    setPhoneError({ show: false, message: '', type: 'error' })
    
    try {
      const response = await fetch('/api/opas/sms/send-verification', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          phone: phoneInfo.phone,
          recipientName: `${pharmacistInfo.firstName} ${pharmacistInfo.lastName}`,
          pharmacyGln: glnValidation.data?.gln
        })
      })

      const responseText = await response.text()
      if (!responseText.trim()) {
        throw new (globalThis.Error)('Empty response from server')
      }

      const result = JSON.parse(responseText)

      if (result.ok) {
        setPhoneInfo(prev => ({
          ...prev,
          isSmsSent: true,
          canResend: false,
          resendCountdown: 180 // 180 saniye (3 dakika) bekleme
        }))
        
        // Countdown timer
        const countdownInterval = setInterval(() => {
          setPhoneInfo(prev => {
            if (prev.resendCountdown <= 1) {
              clearInterval(countdownInterval)
              return { ...prev, canResend: true, resendCountdown: 0 }
            }
            return { ...prev, resendCountdown: prev.resendCountdown - 1 }
          })
        }, 1000)
        
      } else {
        // API'den gelen error mesajını işle (SMS gönderimi için)
        setPhoneError({
          show: true,
          message: result.error || 'SMS gönderimi başarısız oldu',
          type: 'error'
        })
      }
    } catch (error) {
      console.error('SMS send error:', error)
      setPhoneError({
        show: true,
        message: '📱 SMS gönderim sırasında bir hata oluştu.\n\n🔄 Lütfen internet bağlantınızı kontrol edin ve tekrar deneyin.',
        type: 'error'
      })
    } finally {
      setLoading(false)
    }
  }

  const handleVerifySmsCode = async () => {
    setLoading(true)
    setPhoneError({ show: false, message: '', type: 'error' })
    
    try {
      const response = await fetch('/api/opas/sms/verify-code', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          phone: phoneInfo.phone,
          code: phoneInfo.verificationCode
        })
      })

      const responseText = await response.text()
      if (!responseText.trim()) {
        throw new (globalThis.Error)('Empty response from server')
      }

      const result = JSON.parse(responseText)

      if (result.ok) {
        setPhoneInfo(prev => ({
          ...prev,
          isSmsVerified: true
        }))
        setPhoneError({ show: false, message: '', type: 'error' })
      } else {
        // API'den gelen error mesajını işle (yanlış SMS kodu için)
        let userFriendlyMessage = ''
        let errorType: 'error' | 'warning' | 'info' = 'error'

        if (result.error?.includes('expired') || result.error?.includes('süresi')) {
          userFriendlyMessage = '⏰ SMS doğrulama kodunun süresi dolmuş.\n\n🔄 Yeni bir kod gönderilmesini isteyin.'
          errorType = 'warning'
        } else if (result.error?.includes('invalid') || result.error?.includes('yanlış')) {
          userFriendlyMessage = '❌ SMS doğrulama kodu hatalı.\n\n✅ Lütfen telefonunuzdaki 6 haneli kodu kontrol edin ve tekrar deneyin.'
          errorType = 'warning'
        } else if (result.error?.includes('fazla') || result.error?.includes('deneme')) {
          userFriendlyMessage = '🚫 Çok fazla yanlış deneme yapıldı.\n\n⏰ Lütfen 15 dakika bekleyin ve tekrar deneyin.'
          errorType = 'warning'
        } else if (result.error?.includes('servis') || result.error?.includes('kullanılamıyor')) {
          userFriendlyMessage = '📱 SMS servisi geçici olarak kullanılamıyor.\n\n🔄 Lütfen birkaç dakika sonra tekrar deneyin.'
          errorType = 'info'
        } else {
          userFriendlyMessage = `❌ SMS doğrulama başarısız:\n${result.error || 'Bilinmeyen hata'}`
          errorType = 'error'
        }

        setPhoneError({
          show: true,
          message: userFriendlyMessage,
          type: errorType
        })
      }
    } catch (error) {
      console.error('SMS verify error:', error)
      setPhoneError({
        show: true,
        message: '🌐 SMS doğrulama sırasında bir hata oluştu.\n\n💡 Lütfen tekrar deneyin.',
        type: 'error'
      })
    } finally {
      setLoading(false)
    }
  }

  // User credentials validation functions
  const validateUsername = (username: string) => {
    if (!username.trim()) {
      return { isValid: false, error: 'Kullanıcı adı zorunludur' }
    }
    
    if (username.length < 4 || username.length > 50) {
      return { isValid: false, error: 'Kullanıcı adı 4-50 karakter arasında olmalıdır' }
    }
    
    const usernameRegex = /^[a-zA-Z0-9._-]+$/
    if (!usernameRegex.test(username)) {
      return { isValid: false, error: 'Kullanıcı adı sadece harf, rakam, nokta, tire ve alt çizgi içerebilir' }
    }
    
    return { isValid: true, error: '' }
  }

  const validatePassword = (password: string) => {
    if (!password) {
      return { isValid: false, error: 'Parola zorunludur' }
    }

    // 2024 Uluslararası parola kuralları
    const rules = [
      { regex: /.{8,}/, message: 'En az 8 karakter olmalıdır' },
      { regex: /[A-Z]/, message: 'En az bir büyük harf içermelidir' },
      { regex: /[a-z]/, message: 'En az bir küçük harf içermelidir' },
      { regex: /[0-9]/, message: 'En az bir rakam içermelidir' },
      { regex: /[!@#$%^&*(),.?":{}|<>]/, message: 'En az bir özel karakter içermelidir (!@#$%^&*...)' },
      { regex: /^(?!.*(.)\1{2,}).*$/, message: 'Aynı karakteri 3 kez üst üste kullanamazsınız' },
      { regex: /^(?!.*(123|abc|password|qwerty|admin|user)).*$/i, message: 'Yaygın parola kalıpları kullanılamaz' }
    ]

    for (const rule of rules) {
      if (!rule.regex.test(password)) {
        return { isValid: false, error: rule.message }
      }
    }

    return { isValid: true, error: '' }
  }

  const validateConfirmPassword = (password: string, confirmPassword: string) => {
    if (!confirmPassword) {
      return { isValid: false, error: 'Parola tekrarı zorunludur' }
    }
    
    if (password !== confirmPassword) {
      return { isValid: false, error: 'Parolalar eşleşmiyor' }
    }
    
    return { isValid: true, error: '' }
  }

  const handleUsernameChange = async (value: string) => {
    const normalizedUsername = value.toLowerCase().trim()
    
    setCredentialsInfo(prev => ({
      ...prev,
      username: normalizedUsername,
      isUsernameAvailable: null,
      isUsernameChecking: false
    }))
    
    // Clear credentials error when user starts typing
    if (credentialsError.show) {
      setCredentialsError({ show: false, message: '', type: 'error' })
    }
    
    const validation = validateUsername(normalizedUsername)
    setCredentialsValidation(prev => ({
      ...prev,
      username: validation
    }))

    // Username availability check (debounced)
    if (validation.isValid && normalizedUsername.length >= 3) {
      setCredentialsInfo(prev => ({ ...prev, isUsernameChecking: true }))
      
      // Simple debounce - check username after 500ms
      setTimeout(async () => {
        try {
          const response = await fetch(`/api/opas/auth/check-username?username=${encodeURIComponent(normalizedUsername)}`)
          const result = await response.json()
          
          if (result.ok) {
            setCredentialsInfo(prev => ({
              ...prev,
              isUsernameAvailable: result.available,
              isUsernameChecking: false
            }))
          }
        } catch (error) {
          console.error('Username check error:', error)
          setCredentialsInfo(prev => ({
            ...prev,
            isUsernameChecking: false
          }))
        }
      }, 500)
    }
  }

  const handlePasswordChange = (value: string) => {
    setCredentialsInfo(prev => ({
      ...prev,
      password: value
    }))
    
    const validation = validatePassword(value)
    setCredentialsValidation(prev => ({
      ...prev,
      password: validation,
      // Re-validate confirm password when password changes
      confirmPassword: prev.confirmPassword.error ? 
        validateConfirmPassword(value, credentialsInfo.confirmPassword) : 
        prev.confirmPassword
    }))
  }

  const handleConfirmPasswordChange = (value: string) => {
    setCredentialsInfo(prev => ({
      ...prev,
      confirmPassword: value
    }))
    
    const validation = validateConfirmPassword(credentialsInfo.password, value)
    setCredentialsValidation(prev => ({
      ...prev,
      confirmPassword: validation
    }))
  }

  const isCredentialsValid = () => {
    const { username, password, confirmPassword } = credentialsInfo
    const validation = credentialsValidation
    
    return username.trim() && password && confirmPassword &&
           validation.username.isValid && validation.password.isValid && validation.confirmPassword.isValid &&
           credentialsInfo.isUsernameAvailable === true  // Username must be available
  }

  const handleNext = () => {
    if (activeStep === 0 && glnValidation.isValid && glnValidation.confirmed) {
      setActiveStep(1)
    } else if (activeStep === 1 && isPharmacistInfoValid() && pharmacistInfo.nviValidated) {
      setActiveStep(2)
    } else if (activeStep === 2 && isEmailInfoValid() && emailInfo.isEmailVerified) {
      setActiveStep(3)
    } else if (activeStep === 3 && isPhoneInfoValid() && phoneInfo.isSmsVerified) {
      setActiveStep(4)
    } else if (activeStep === 4 && isCredentialsValid()) {
      setActiveStep(5)
      // Son adım - kayıt tamamlanacak
      handleFinalRegistration()
    }
  }

  // Enter key handler for form submission
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault()
      handleNext()
    }
  }

  const handleBack = () => {
    setActiveStep((prevStep) => prevStep - 1)
  }

  // Final registration function - calls PharmacistAdmin API
  const handleFinalRegistration = async () => {
    let warningTimer: NodeJS.Timeout | null = null
    
    try {
      setLoading(true)
      // Don't reset warning at start - let it show after 4 seconds

      // Show warning after 4 seconds
      warningTimer = setTimeout(() => {
        console.log('⚠️ 4 SANIYE GEÇTİ - WARNING GÖSTER!')
        setShowLongProcessWarning(true)
        console.log('⚠️ showLongProcessWarning STATE = TRUE YAPILDI')
      }, 4000)

      const registrationData = {
        username: credentialsInfo.username,
        password: credentialsInfo.password,
        email: emailInfo.email,
        phone: phoneInfo.phone,
        personalGln: glnValidation.data?.gln || '', // GLN from step 1
        firstName: pharmacistInfo.firstName,
        lastName: pharmacistInfo.lastName,
        tcNumber: pharmacistInfo.tcNumber,
        birthYear: pharmacistInfo.birthYear,
        pharmacyRegistrationNo: glnValidation.data?.pharmacyRegistrationNo || null, // From GLN validation
        isEmailVerified: emailInfo.isEmailVerified,
        isPhoneVerified: phoneInfo.isSmsVerified,
        isNviVerified: pharmacistInfo.nviValidated
      }

      const response = await fetch('/api/opas/auth/pharmacist/register', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(registrationData)
      })

      const result = await response.json()

      if (result.success) {
        // Registration successful
        setRegistrationSuccess({
          show: true,
          pharmacistId: result.pharmacistId,
          tenantId: result.tenantId,
          username: result.username,
          message: result.message
        })
      } else {
        // Registration failed
        setRegistrationError({
          show: true,
          message: result.error || 'Kayıt işlemi başarısız oldu'
        })
      }
    } catch (error) {
      console.error('Registration error:', error)
      setRegistrationError({
        show: true,
        message: 'Kayıt işlemi sırasında bir hata oluştu'
      })
    } finally {
      if (warningTimer) {
        clearTimeout(warningTimer)
      }
      setLoading(false)
      setShowLongProcessWarning(false)
    }
  }

  const renderGLNValidationStep = () => (
    <Box>
      <Typography variant="h6" sx={{ mb: 3, fontWeight: 600, textAlign: 'center' }}>
        🏪 Eczane GLN Doğrulaması
      </Typography>
      
      <Stack spacing={3}>
        {/* GLN Input - 13 ayrı kutucuk */}
        <Box>
          <Typography variant="body2" sx={{ mb: 2, fontWeight: 500, color: 'text.primary' }}>
            GLN Numarası (13 hane)
          </Typography>
          <Box sx={{ display: 'flex', gap: 1, justifyContent: 'center', flexWrap: 'wrap' }}>
            {Array.from({ length: 13 }, (_, index) => (
              <TextField
                key={index}
                value={gln[index] || ''}
                onChange={(e) => handleGLNDigitChange(index, e.target.value)}
                onKeyDown={(e) => handleGLNKeyDown(index, e)}
                onFocus={(e) => e.target.select()}
                autoFocus={index === 0}
                inputProps={{
                  maxLength: 1,
                  inputMode: 'numeric',
                  pattern: '[0-9]',
                  'data-index': index,
                  style: { 
                    textAlign: 'center',
                    fontSize: '1.2rem',
                    fontWeight: 'bold',
                    padding: '8px 4px'
                  }
                }}
                sx={{
                  width: 40,
                  height: 50,
                  '& .MuiOutlinedInput-root': {
                    borderRadius: 2,
                    borderColor: index < 3 ? 'success.main' : 'primary.main',
                    '&:hover': {
                      borderColor: index < 3 ? 'success.dark' : 'primary.dark',
                    },
                    '&.Mui-focused': {
                      borderColor: index < 3 ? 'success.dark' : 'primary.dark',
                      boxShadow: `0 0 0 2px ${index < 3 ? 'rgba(76, 175, 80, 0.2)' : 'rgba(25, 118, 210, 0.2)'}`,
                    }
                  }
                }}
                placeholder={index < 3 ? ['8', '6', '8'][index] : ''}
                disabled={index < 3}
              />
            ))}
          </Box>
          <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block', textAlign: 'center' }}>
            İlk 3 hane (868) otomatik doldurulur
          </Typography>
          
          {/* Doğrulama Butonu */}
          {gln.length === 13 && (
            <Box sx={{ mt: 2, textAlign: 'center' }}>
              <Button
                onClick={handleGLNValidation}
                disabled={loading}
                startIcon={loading ? <CircularProgress size={16} /> : <Search />}
                variant="contained"
                sx={{ 
                  borderRadius: 2,
                  px: 3,
                  py: 1.5,
                  fontWeight: 600
                }}
              >
                {loading ? 'Kontrol Ediliyor...' : 'GLN Doğrula'}
              </Button>
            </Box>
          )}
        </Box>

        {/* GLN Format Help */}
        <Alert severity="info" sx={{ borderRadius: 2 }}>
          <Typography variant="body2">
            <strong>GLN Nedir?</strong> Global Location Number - Eczaneler için 13 haneli benzersiz kimlik numarası.
            <br />
            <strong>Format:</strong> 868XXXXXXXXXX (868 ile başlamalı)
          </Typography>
        </Alert>

        {/* Validation Results */}
        {glnValidation.isValid === true && glnValidation.data && (
          <Fade in timeout={500}>
            <PharmacyCard>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                  <CheckCircle sx={{ color: 'success.main', fontSize: 32 }} />
                  <Box>
                    <Typography variant="h6" sx={{ fontWeight: 600, color: 'success.main' }}>
                      ✅ GLN Doğrulandı!
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Bu eczane kayıtlarımızda mevcut
                    </Typography>
                  </Box>
                </Box>

                <Stack spacing={2}>
                  <Box>
                    <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1 }}>
                      🏪 Eczane Bilgileri:
                    </Typography>
                    <Stack spacing={1}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Business sx={{ fontSize: 20, color: 'text.secondary' }} />
                        <Typography variant="body1">
                          <strong>{glnValidation.data.companyName || 'Eczane Adı Belirtilmemiş'}</strong>
                        </Typography>
                      </Box>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <LocationOn sx={{ fontSize: 20, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">
                          {[glnValidation.data.city, glnValidation.data.town].filter(Boolean).join(', ') || 'Konum bilgisi yok'}
                        </Typography>
                      </Box>
                      {glnValidation.data.address && (
                        <Typography variant="body2" color="text.secondary" sx={{ ml: 3 }}>
                          📍 {glnValidation.data.address}
                        </Typography>
                      )}
                    </Stack>
                  </Box>

                  <Box sx={{ display: 'flex', gap: 2 }}>
                    <Chip
                      icon={<Verified />}
                      label={`GLN: ${glnValidation.data.gln}`}
                      color="success"
                      variant="outlined"
                    />
                    <Chip
                      label={glnValidation.data.active ? '🟢 Aktif' : '🔴 Pasif'}
                      color={glnValidation.data.active ? 'success' : 'error'}
                      variant="outlined"
                    />
                  </Box>
                </Stack>

                {/* Confirmation Buttons */}
                {glnValidation.confirmed === null && (
                  <Box sx={{ mt: 3, pt: 2, borderTop: '1px solid', borderColor: 'divider' }}>
                    <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 2, textAlign: 'center' }}>
                      ❓ Bu bilgiler size ait mi?
                    </Typography>
                    <Stack direction="row" spacing={2} justifyContent="center">
                      <ConfirmButton
                        variant="contained"
                        color="success"
                        startIcon={<ThumbUp />}
                        onClick={handleConfirmPharmacy}
                        sx={{
                          background: 'linear-gradient(45deg, #4CAF50 30%, #8BC34A 90%)',
                          '&:hover': {
                            background: 'linear-gradient(45deg, #45a049 30%, #7CB342 90%)',
                          }
                        }}
                      >
                        ✅ Bu Benim
                      </ConfirmButton>
                      <RejectButton
                        variant="outlined"
                        color="error"
                        startIcon={<ThumbDown />}
                        onClick={handleRejectPharmacy}
                        sx={{
                          borderColor: 'error.main',
                          color: 'error.main',
                          '&:hover': {
                            borderColor: 'error.dark',
                            backgroundColor: 'rgba(244, 67, 54, 0.04)',
                          }
                        }}
                      >
                        ❌ Bu Ben Değilim
                      </RejectButton>
                    </Stack>
                  </Box>
                )}

                {/* Confirmed State */}
                {glnValidation.confirmed === true && (
                  <Box sx={{ mt: 2, p: 2, borderRadius: 2, bgcolor: 'success.main', color: 'success.contrastText', textAlign: 'center' }}>
                    <Typography variant="body1" sx={{ fontWeight: 600 }}>
                      🎉 Harika! Bilgileriniz onaylandı. 2. adıma geçiliyor...
                    </Typography>
                  </Box>
                )}
              </CardContent>
            </PharmacyCard>
          </Fade>
        )}

        {/* Contact OPAS Message */}
        {showContactMessage && (
          <Fade in timeout={500}>
            <Alert 
              severity="warning" 
              sx={{ borderRadius: 2, border: '2px solid', borderColor: 'warning.main' }}
              icon={<Warning />}
            >
              <Box>
                <Typography variant="body1" sx={{ fontWeight: 600, mb: 2 }}>
                  📞 OPAS Yetkililerimizle İletişime Geçin
                </Typography>
                <Typography variant="body2" sx={{ mb: 2 }}>
                  Eğer bu bilgiler size ait değilse, lütfen OPAS yetkililerimizle iletişime geçin. 
                  GLN kayıtları ile ilgili düzeltmeleri birlikte yapabiliriz.
                </Typography>
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Email sx={{ fontSize: 18 }} />
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      destek@opas.com.tr
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Phone sx={{ fontSize: 18 }} />
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      0850 XXX XX XX
                    </Typography>
                  </Box>
                </Stack>
                <Box sx={{ mt: 2, textAlign: 'center' }}>
                  <Button
                    variant="contained"
                    color="warning"
                    startIcon={<ContactSupport />}
                    onClick={() => {
                      // Kayıt işlemini sonlandır
                      window.location.href = '/t-login'
                    }}
                    sx={{ borderRadius: 2 }}
                  >
                    Kayıt İşlemini Sonlandır
                  </Button>
                </Box>
              </Box>
            </Alert>
          </Fade>
        )}

        {glnValidation.isValid === false && (
          <Fade in timeout={500}>
            <Alert 
              severity="error" 
              sx={{ borderRadius: 2 }}
              icon={<Error />}
            >
              <Typography variant="body2">
                <strong>❌ GLN Doğrulanamadı:</strong> {glnValidation.error}
              </Typography>
            </Alert>
          </Fade>
        )}

        {/* Next Step Info */}
        {glnValidation.isValid === true && glnValidation.confirmed === null && (
          <Alert severity="info" sx={{ borderRadius: 2 }}>
            <Typography variant="body2">
              ✅ <strong>GLN Doğrulandı!</strong> Lütfen eczane bilgilerinizi onaylayın.
            </Typography>
          </Alert>
        )}
      </Stack>
    </Box>
  )

  const renderPharmacistInfoStep = () => (
    <Box>
      <Typography variant="h6" sx={{ mb: 3, fontWeight: 600, textAlign: 'center' }}>
        👨‍⚕️ Sorumlu Eczacı Bilgileri
      </Typography>
      
      <Stack spacing={3}>
        {/* İsim ve Soyisim */}
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
          <ModernTextField
            fullWidth
            label="Adı"
            value={pharmacistInfo.firstName}
            onChange={(e) => handlePharmacistInfoChange('firstName', e.target.value)}
            error={!pharmacistValidation.firstName.isValid}
            helperText={pharmacistValidation.firstName.error}
            autoFocus
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <Person color="success" />
                </InputAdornment>
              ),
            }}
          />
          <ModernTextField
            fullWidth
            label="Soyadı"
            value={pharmacistInfo.lastName}
            onChange={(e) => handlePharmacistInfoChange('lastName', e.target.value)}
            error={!pharmacistValidation.lastName.isValid}
            helperText={pharmacistValidation.lastName.error}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <Person color="success" />
                </InputAdornment>
              ),
            }}
          />
        </Box>

        {/* TC Kimlik Numarası */}
        <ModernTextField
          fullWidth
          label="TC Kimlik Numarası"
          value={pharmacistInfo.tcNumber}
          onChange={(e) => {
            const value = e.target.value.replace(/\D/g, '').slice(0, 11)
            handlePharmacistInfoChange('tcNumber', value)
          }}
          error={!pharmacistValidation.tcNumber.isValid}
          helperText={pharmacistValidation.tcNumber.error}
          inputProps={{ 
            maxLength: 11,
            inputMode: 'numeric',
            pattern: '[0-9]*'
          }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <Shield color="success" />
              </InputAdornment>
            ),
          }}
        />

        {/* Doğum Yılı */}
        <ModernTextField
          fullWidth
          label="Doğum Yılı"
          value={pharmacistInfo.birthYear}
          onChange={(e) => {
            const value = e.target.value.replace(/\D/g, '').slice(0, 4)
            handlePharmacistInfoChange('birthYear', value)
          }}
          error={!pharmacistValidation.birthYear.isValid}
          helperText={pharmacistValidation.birthYear.error}
          placeholder="1990"
          inputProps={{ 
            maxLength: 4,
            inputMode: 'numeric',
            pattern: '[0-9]*'
          }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                📅
              </InputAdornment>
            ),
          }}
        />

        {/* Bilgilendirme */}
        <Alert severity="info" sx={{ borderRadius: 2 }}>
          <Typography variant="body2">
            <strong>Kişisel Veriler:</strong> Bu bilgiler sadece kimlik doğrulama amacıyla kullanılır.
            <br />
            <strong>Güvenlik:</strong> TC kimlik numarası algoritması ile doğrulanır.
          </Typography>
        </Alert>

        {/* NVI Hata Mesajı */}
        {nviError.show && (
          <Box sx={{ mt: 2 }}>
            <Alert 
              severity={nviError.type} 
              sx={{ 
                borderRadius: 2,
                '& .MuiAlert-message': {
                  whiteSpace: 'pre-line',
                  fontSize: '0.95rem',
                  lineHeight: 1.6
                },
                boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
                border: nviError.type === 'warning' ? '1px solid #ff9800' : 
                        nviError.type === 'info' ? '1px solid #2196f3' : '1px solid #f44336'
              }}
              action={
                <IconButton
                  aria-label="close"
                  color="inherit"
                  size="small"
                  onClick={() => setNviError({ show: false, message: '', type: 'error' })}
                >
                  ✕
                </IconButton>
              }
            >
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
                  {nviError.type === 'warning' ? '⚠️ Kimlik Bilgisi Uyarısı' :
                   nviError.type === 'info' ? 'ℹ️ Sistem Bilgisi' : '❌ Doğrulama Hatası'}
                </Typography>
                <Typography variant="body2">
                  {nviError.message}
                </Typography>
              </Box>
            </Alert>
          </Box>
        )}

        {/* NVI Doğrulama Butonu */}
        {isPharmacistInfoValid() && !pharmacistInfo.nviValidated && (
          <Box sx={{ mt: 2 }}>
            <UltraButton
              fullWidth
              variant="contained"
              startIcon={<Shield />}
              onClick={handleNviValidation}
              disabled={loading}
              sx={{
                background: 'linear-gradient(45deg, #1976d2 30%, #42a5f5 90%)',
                '&:hover': {
                  background: 'linear-gradient(45deg, #1565c0 30%, #1976d2 90%)',
                },
                py: 2,
                fontSize: '1.1rem'
              }}
            >
              {loading ? 'NVI Doğrulanıyor...' : '🏛️ NVI ile Kimlik Doğrula'}
            </UltraButton>
          </Box>
        )}

        {/* Özet Bilgi */}
        {isPharmacistInfoValid() && pharmacistInfo.nviValidated && (
          <Fade in timeout={500}>
            <Card sx={{ borderRadius: 2, bgcolor: 'success.light', color: 'success.contrastText' }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  <CheckCircle sx={{ fontSize: 32 }} />
                  <Box>
                    <Typography variant="h6" sx={{ fontWeight: 600 }}>
                      ✅ NVI Kimlik Doğrulaması Başarılı
                    </Typography>
                    <Typography variant="body2">
                      {pharmacistInfo.firstName} {pharmacistInfo.lastName} - {pharmacistInfo.birthYear} doğumlu
                    </Typography>
                    <Typography variant="body2" sx={{ mt: 1, opacity: 0.8 }}>
                      🏛️ T.C. Nüfus ve Vatandaşlık İşleri tarafından doğrulandı
                    </Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Fade>
        )}
      </Stack>
    </Box>
  )

  const renderEmailVerificationStep = () => (
    <Box>
      <Typography variant="h6" sx={{ mb: 3, fontWeight: 600, textAlign: 'center' }}>
        📧 Email Doğrulama
      </Typography>
      
      <Stack spacing={3}>
        {/* Email Address Input */}
        <ModernTextField
          fullWidth
          label="Email Adresi"
          type="email"
          value={emailInfo.email}
          onChange={(e) => handleEmailChange(e.target.value)}
          error={!emailValidation.email.isValid}
          helperText={emailValidation.email.error}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <Email color="primary" />
              </InputAdornment>
            ),
          }}
        />

        {/* Send Verification Code Button */}
        {isEmailInfoValid() && !emailInfo.isEmailSent && (
          <UltraButton
            fullWidth
            variant="contained"
            startIcon={<Email />}
            onClick={handleSendVerificationCode}
            disabled={loading}
            sx={{
              background: 'linear-gradient(45deg, #2196f3 30%, #64b5f6 90%)',
              '&:hover': {
                background: 'linear-gradient(45deg, #1976d2 30%, #2196f3 90%)',
              },
              py: 2,
              fontSize: '1.1rem'
            }}
          >
            {loading ? 'Gönderiliyor...' : '📨 Doğrulama Kodu Gönder'}
          </UltraButton>
        )}

        {/* Email Error */}
        {emailError.show && (
          <Box sx={{ mt: 2 }}>
            <Alert 
              severity={emailError.type} 
              sx={{ 
                borderRadius: 2,
                '& .MuiAlert-message': {
                  whiteSpace: 'pre-line',
                  fontSize: '0.95rem',
                  lineHeight: 1.6
                },
                boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
                border: emailError.type === 'warning' ? '1px solid #ff9800' : 
                        emailError.type === 'info' ? '1px solid #2196f3' : '1px solid #f44336'
              }}
              action={
                <IconButton
                  aria-label="close"
                  color="inherit"
                  size="small"
                  onClick={() => setEmailError({ show: false, message: '', type: 'error' })}
                >
                  ✕
                </IconButton>
              }
            >
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
                  {emailError.type === 'warning' ? '⚠️ Email Uyarısı' :
                   emailError.type === 'info' ? 'ℹ️ Bilgi' : '❌ Email Hatası'}
                </Typography>
                <Typography variant="body2">
                  {emailError.message}
                </Typography>
              </Box>
            </Alert>
          </Box>
        )}

        {/* Email Sent Confirmation */}
        {emailInfo.isEmailSent && (
          <Fade in timeout={500}>
            <Alert severity="info" sx={{ borderRadius: 2 }}>
              <Typography variant="body2">
                <strong>📧 Doğrulama kodu gönderildi!</strong>
                <br />
                {emailInfo.email} adresine 6 haneli doğrulama kodu gönderildi.
                <br />
                <strong>⏰ Kod 3 dakika geçerlidir.</strong>
              </Typography>
            </Alert>
          </Fade>
        )}

        {/* Verification Code Input */}
        {emailInfo.isEmailSent && !emailInfo.isEmailVerified && (
          <Stack spacing={2}>
            {/* Email Verification Code - 6 ayrı kutucuk */}
            <Box>
              <Typography variant="body2" sx={{ mb: 2, fontWeight: 500, color: 'text.primary', textAlign: 'center' }}>
                🔑 Email Doğrulama Kodu (6 hane)
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, justifyContent: 'center', flexWrap: 'wrap' }}>
                {Array.from({ length: 6 }, (_, index) => (
                  <TextField
                    key={index}
                    value={emailInfo.verificationCode[index] || ''}
                    onChange={(e) => handleEmailCodeDigitChange(index, e.target.value)}
                    onKeyDown={(e) => handleEmailCodeKeyDown(index, e)}
                    onFocus={(e) => e.target.select()}
                    inputProps={{
                      maxLength: 1,
                      inputMode: 'numeric',
                      pattern: '[0-9]',
                      'data-index': index,
                      style: { 
                        textAlign: 'center',
                        fontSize: '1.2rem',
                        fontWeight: 'bold',
                        padding: '8px 4px'
                      }
                    }}
                    sx={{
                      width: 40,
                      height: 50,
                      '& .MuiOutlinedInput-root': {
                        borderRadius: 2,
                        borderColor: 'primary.main',
                        '&:hover': {
                          borderColor: 'primary.dark',
                        },
                        '&.Mui-focused': {
                          borderColor: 'primary.dark',
                          boxShadow: '0 0 0 2px rgba(25, 118, 210, 0.2)',
                        }
                      }
                    }}
                  />
                ))}
              </Box>
              {emailValidation.verificationCode.error && (
                <Typography variant="caption" color="error" sx={{ mt: 1, display: 'block', textAlign: 'center' }}>
                  {emailValidation.verificationCode.error}
                </Typography>
              )}
            </Box>

            {/* Verify Code Button */}
            <UltraButton
              fullWidth
              variant="contained"
              onClick={handleVerifyCode}
              disabled={loading || !validateVerificationCode(emailInfo.verificationCode).isValid}
              sx={{
                background: 'linear-gradient(45deg, #4caf50 30%, #81c784 90%)',
                '&:hover': {
                  background: 'linear-gradient(45deg, #388e3c 30%, #4caf50 90%)',
                },
                py: 2,
                fontSize: '1.1rem'
              }}
            >
              {loading ? 'Doğrulanıyor...' : '✅ Kodu Doğrula'}
            </UltraButton>

            {/* Resend Code */}
            <Box sx={{ textAlign: 'center' }}>
              {emailInfo.canResend ? (
                <Button 
                  variant="text" 
                  onClick={handleSendVerificationCode}
                  disabled={loading}
                  sx={{ textDecoration: 'underline' }}
                >
                  🔄 Kodu Tekrar Gönder
                </Button>
              ) : (
                <Typography variant="caption" color="text.secondary">
                  🔄 Tekrar gönderebilirsiniz: {Math.floor(emailInfo.resendCountdown / 60)}:{String(emailInfo.resendCountdown % 60).padStart(2, '0')}
                </Typography>
              )}
            </Box>
          </Stack>
        )}

        {/* Success Message */}
        {emailInfo.isEmailVerified && (
          <Fade in timeout={500}>
            <Card sx={{ borderRadius: 2, bgcolor: 'success.light', color: 'success.contrastText' }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  <CheckCircle sx={{ fontSize: 32 }} />
                  <Box>
                    <Typography variant="h6" sx={{ fontWeight: 600 }}>
                      ✅ Email Doğrulandı!
                    </Typography>
                    <Typography variant="body2">
                      {emailInfo.email} adresi başarıyla doğrulandı
                    </Typography>
                    <Typography variant="body2" sx={{ mt: 1, opacity: 0.8 }}>
                      📧 Email doğrulama tamamlandı, devam edebilirsiniz
                    </Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Fade>
        )}
      </Stack>
    </Box>
  )

  const renderPhoneVerificationStep = () => (
    <Box>
      <Typography variant="h6" sx={{ mb: 3, fontWeight: 600, textAlign: 'center' }}>
        📱 Telefon Doğrulama
      </Typography>
      
      <Stack spacing={3}>
        {/* Phone Number Input */}
        <ModernTextField
          fullWidth
          label="Cep Telefonu Numarası"
          type="tel"
          value={phoneInfo.phone}
          onChange={(e) => handlePhoneChange(e.target.value)}
          error={!phoneValidation.phone.isValid}
          helperText={phoneValidation.phone.error}
          placeholder="5XX XXX XX XX"
          inputProps={{ 
            maxLength: 15, // "5XX XXX XX XX" + spaces
            inputMode: 'tel'
          }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <Phone color="primary" />
              </InputAdornment>
            ),
          }}
        />

        {/* Send SMS Code Button */}
        {isPhoneInfoValid() && !phoneInfo.isSmsSent && (
          <UltraButton
            fullWidth
            variant="contained"
            startIcon={<Phone />}
            onClick={handleSendSmsCode}
            disabled={loading}
            sx={{
              background: 'linear-gradient(45deg, #ff9800 30%, #ffb74d 90%)',
              '&:hover': {
                background: 'linear-gradient(45deg, #f57c00 30%, #ff9800 90%)',
              },
              py: 2,
              fontSize: '1.1rem'
            }}
          >
            {loading ? 'Gönderiliyor...' : '📱 SMS Doğrulama Kodu Gönder'}
          </UltraButton>
        )}

        {/* Phone Error */}
        {phoneError.show && (
          <Box sx={{ mt: 2 }}>
            <Alert 
              severity={phoneError.type} 
              sx={{ 
                borderRadius: 2,
                '& .MuiAlert-message': {
                  whiteSpace: 'pre-line',
                  fontSize: '0.95rem',
                  lineHeight: 1.6
                },
                boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
                border: phoneError.type === 'warning' ? '1px solid #ff9800' : 
                        phoneError.type === 'info' ? '1px solid #2196f3' : '1px solid #f44336'
              }}
              action={
                <IconButton
                  aria-label="close"
                  color="inherit"
                  size="small"
                  onClick={() => setPhoneError({ show: false, message: '', type: 'error' })}
                >
                  ✕
                </IconButton>
              }
            >
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
                  {phoneError.type === 'warning' ? '⚠️ SMS Uyarısı' :
                   phoneError.type === 'info' ? 'ℹ️ Bilgi' : '❌ SMS Hatası'}
                </Typography>
                <Typography variant="body2">
                  {phoneError.message}
                </Typography>
              </Box>
            </Alert>
          </Box>
        )}

        {/* SMS Sent Confirmation */}
        {phoneInfo.isSmsSent && (
          <Fade in timeout={500}>
            <Alert severity="info" sx={{ borderRadius: 2 }}>
              <Typography variant="body2">
                <strong>📱 SMS doğrulama kodu gönderildi!</strong>
                <br />
                {phoneInfo.phone} numarasına 6 haneli doğrulama kodu gönderildi.
                <br />
                <strong>⏰ Kod 3 dakika geçerlidir.</strong>
              </Typography>
            </Alert>
          </Fade>
        )}

        {/* SMS Code Input */}
        {phoneInfo.isSmsSent && !phoneInfo.isSmsVerified && (
          <Stack spacing={2}>
            {/* SMS Verification Code - 6 ayrı kutucuk */}
            <Box>
              <Typography variant="body2" sx={{ mb: 2, fontWeight: 500, color: 'text.primary', textAlign: 'center' }}>
                📱 SMS Doğrulama Kodu (6 hane)
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, justifyContent: 'center', flexWrap: 'wrap' }}>
                {Array.from({ length: 6 }, (_, index) => (
                  <TextField
                    key={index}
                    value={phoneInfo.verificationCode[index] || ''}
                    onChange={(e) => handleSmsCodeDigitChange(index, e.target.value)}
                    onKeyDown={(e) => handleSmsCodeKeyDown(index, e)}
                    onFocus={(e) => e.target.select()}
                    inputProps={{
                      maxLength: 1,
                      inputMode: 'numeric',
                      pattern: '[0-9]',
                      'data-index': index,
                      style: { 
                        textAlign: 'center',
                        fontSize: '1.2rem',
                        fontWeight: 'bold',
                        padding: '8px 4px'
                      }
                    }}
                    sx={{
                      width: 40,
                      height: 50,
                      '& .MuiOutlinedInput-root': {
                        borderRadius: 2,
                        borderColor: 'primary.main',
                        '&:hover': {
                          borderColor: 'primary.dark',
                        },
                        '&.Mui-focused': {
                          borderColor: 'primary.dark',
                          boxShadow: '0 0 0 2px rgba(25, 118, 210, 0.2)',
                        }
                      }
                    }}
                  />
                ))}
              </Box>
              {phoneValidation.verificationCode.error && (
                <Typography variant="caption" color="error" sx={{ mt: 1, display: 'block', textAlign: 'center' }}>
                  {phoneValidation.verificationCode.error}
                </Typography>
              )}
            </Box>

            {/* Verify Code Button */}
            <UltraButton
              fullWidth
              variant="contained"
              onClick={handleVerifySmsCode}
              disabled={loading || !validateSmsCode(phoneInfo.verificationCode).isValid}
              sx={{
                background: 'linear-gradient(45deg, #4caf50 30%, #81c784 90%)',
                '&:hover': {
                  background: 'linear-gradient(45deg, #388e3c 30%, #4caf50 90%)',
                },
                py: 2,
                fontSize: '1.1rem'
              }}
            >
              {loading ? 'Doğrulanıyor...' : '✅ SMS Kodu Doğrula'}
            </UltraButton>

            {/* Resend SMS */}
            <Box sx={{ textAlign: 'center' }}>
              {phoneInfo.canResend ? (
                <Button 
                  variant="text" 
                  onClick={handleSendSmsCode}
                  disabled={loading}
                  sx={{ textDecoration: 'underline' }}
                >
                  🔄 SMS Kodu Tekrar Gönder
                </Button>
              ) : (
                <Typography variant="caption" color="text.secondary">
                  🔄 Tekrar gönderebilirsiniz: {Math.floor(phoneInfo.resendCountdown / 60)}:{String(phoneInfo.resendCountdown % 60).padStart(2, '0')}
                </Typography>
              )}
            </Box>
          </Stack>
        )}

        {/* Success Message */}
        {phoneInfo.isSmsVerified && (
          <Fade in timeout={500}>
            <Card sx={{ borderRadius: 2, bgcolor: 'success.light', color: 'success.contrastText' }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  <CheckCircle sx={{ fontSize: 32 }} />
                  <Box>
                    <Typography variant="h6" sx={{ fontWeight: 600 }}>
                      ✅ Telefon Doğrulandı!
                    </Typography>
                    <Typography variant="body2">
                      {phoneInfo.phone} numarası başarıyla doğrulandı
                    </Typography>
                    <Typography variant="body2" sx={{ mt: 1, opacity: 0.8 }}>
                      📱 SMS doğrulama tamamlandı, devam edebilirsiniz
                    </Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Fade>
        )}

        {/* Info Alert */}
        <Alert severity="info" sx={{ borderRadius: 2 }}>
          <Typography variant="body2">
            <strong>📱 SMS Doğrulama:</strong> Cep telefonunuza gönderilecek 6 haneli kod ile doğrulama yapılır.
            <br />
            <strong>🔒 Güvenlik:</strong> Bu numara sadece güvenlik amaçlı kullanılır.
          </Typography>
        </Alert>
      </Stack>
    </Box>
  )

  const renderRegistrationCompleteStep = () => (
    <Box>
      <Typography variant="h6" sx={{ mb: 3, fontWeight: 600, textAlign: 'center' }}>
        🎉 Kayıt Tamamlandı!
      </Typography>
      
      {registrationSuccess.show ? (
        <Box sx={{ textAlign: 'center' }}>
          <CheckCircle sx={{ fontSize: 80, color: 'success.main', mb: 2 }} />
          <Typography variant="h5" sx={{ mb: 2, color: 'success.main' }}>
            Başarılı!
          </Typography>
          <Typography variant="body1" sx={{ mb: 3 }}>
            {registrationSuccess.message}
          </Typography>
          
          <Box sx={{ p: 3, bgcolor: 'background.paper', borderRadius: 2, mb: 3 }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
              Kayıt Bilgileri:
            </Typography>
            <Box sx={{ textAlign: 'left' }}>
              <Typography variant="body2" sx={{ mb: 1 }}>
                <strong>Eczacı ID:</strong> {registrationSuccess.pharmacistId}
              </Typography>
              <Typography variant="body2" sx={{ mb: 1 }}>
                <strong>Tenant ID:</strong> {registrationSuccess.tenantId}
              </Typography>
              <Typography variant="body2" sx={{ mb: 1 }}>
                <strong>Kullanıcı Adı:</strong> {registrationSuccess.username}
              </Typography>
            </Box>
          </Box>

          <Box sx={{ p: 2, bgcolor: 'info.light', borderRadius: 2, mb: 3 }}>
            <Typography variant="body1" sx={{ color: 'info.contrastText', fontWeight: 600 }}>
              🔄 Giriş sayfasına yönlendiriliyorsunuz...
            </Typography>
            <Typography variant="h4" sx={{ color: 'info.contrastText', mt: 1 }}>
              {countdown} saniye
            </Typography>
          </Box>
          
          <Button
            variant="contained"
            size="large"
            onClick={() => window.location.href = '/t-login'}
            sx={{ mr: 2 }}
          >
            Hemen Giriş Yap
          </Button>
          <Button
            variant="outlined"
            size="large"
            onClick={() => window.location.href = '/'}
          >
            Ana Sayfa
          </Button>
        </Box>
      ) : registrationError.show ? (
        <Box sx={{ textAlign: 'center' }}>
          <Typography variant="h5" sx={{ mb: 2, color: 'error.main' }}>
            Hata!
          </Typography>
          <Typography variant="body1" sx={{ mb: 3 }}>
            {registrationError.message}
          </Typography>
          <Button
            variant="contained"
            size="large"
            onClick={() => setActiveStep(4)}
          >
            Tekrar Dene
          </Button>
        </Box>
      ) : (
        <Box sx={{ textAlign: 'center' }}>
          <Typography variant="body1" sx={{ mb: 3 }}>
            Kayıt işlemi tamamlanıyor...
          </Typography>
          <CircularProgress />
        </Box>
      )}
    </Box>
  )

  const renderCredentialsStep = () => (
    <Box>
      <Typography variant="h6" sx={{ mb: 3, fontWeight: 600, textAlign: 'center' }}>
        🔐 Kullanıcı Adı ve Parola
      </Typography>
      
      <Stack spacing={3}>
        {/* Username Input */}
        <ModernTextField
          fullWidth
          label="Kullanıcı Adı"
          value={credentialsInfo.username}
          onChange={(e) => handleUsernameChange(e.target.value)}
          error={!credentialsValidation.username.isValid || credentialsInfo.isUsernameAvailable === false}
          helperText={
            !credentialsValidation.username.isValid ? 
              credentialsValidation.username.error :
              credentialsInfo.isUsernameChecking ? 
                '🔍 Kontrol ediliyor...' :
                credentialsInfo.isUsernameAvailable === true ?
                  '✅ Kullanıcı adı kullanılabilir' :
                  credentialsInfo.isUsernameAvailable === false ?
                    '❌ Kullanıcı adı zaten alınmış' : ''
          }
          autoFocus
          inputProps={{ 
            maxLength: 50,
            style: { textTransform: 'lowercase' }
          }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <Person color={credentialsInfo.isUsernameAvailable === true ? "success" : "primary"} />
              </InputAdornment>
            ),
            endAdornment: credentialsInfo.isUsernameChecking && (
              <InputAdornment position="end">
                <Box sx={{ display: 'flex', alignItems: 'center' }}>
                  <Typography variant="caption" sx={{ mr: 1 }}>🔍</Typography>
                </Box>
              </InputAdornment>
            )
          }}
        />

        {/* Password Input */}
        <ModernTextField
          fullWidth
          type={showPassword ? "text" : "password"}
          label="Parola"
          value={credentialsInfo.password}
          onChange={(e) => handlePasswordChange(e.target.value)}
          error={!credentialsValidation.password.isValid}
          helperText={credentialsValidation.password.error}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <Lock color={credentialsValidation.password.isValid ? "success" : "primary"} />
              </InputAdornment>
            ),
            endAdornment: (
              <InputAdornment position="end">
                <IconButton
                  aria-label="toggle password visibility"
                  onClick={() => setShowPassword(!showPassword)}
                  edge="end"
                >
                  {showPassword ? <VisibilityOff /> : <Visibility />}
                </IconButton>
              </InputAdornment>
            ),
          }}
        />

        {/* Password Rules Display */}
        {credentialsInfo.password && (
          <Box sx={{ p: 2, borderRadius: 2, bgcolor: 'background.paper', border: '1px solid', borderColor: 'divider' }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1 }}>
              Parola Gereksinimleri:
            </Typography>
            <Stack spacing={0.5}>
              {[
                { test: credentialsInfo.password.length >= 8, text: 'En az 8 karakter' },
                { test: /[A-Z]/.test(credentialsInfo.password), text: 'En az bir büyük harf (A-Z)' },
                { test: /[a-z]/.test(credentialsInfo.password), text: 'En az bir küçük harf (a-z)' },
                { test: /[0-9]/.test(credentialsInfo.password), text: 'En az bir rakam (0-9)' },
                { test: /[!@#$%^&*(),.?":{}|<>]/.test(credentialsInfo.password), text: 'En az bir özel karakter (!@#$...)' },
                { test: !/(.)\1{2,}/.test(credentialsInfo.password), text: 'Aynı karakter 3 kez üst üste yok' },
                { test: !/123|abc|password|qwerty|admin|user/i.test(credentialsInfo.password), text: 'Yaygın kalıp içermiyor' }
              ].map((rule, index) => (
                <Box key={index} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  {rule.test ? (
                    <Typography sx={{ color: 'success.main', fontSize: '0.75rem' }}>✅</Typography>
                  ) : (
                    <Typography sx={{ color: 'error.main', fontSize: '0.75rem' }}>❌</Typography>
                  )}
                  <Typography 
                    variant="caption" 
                    sx={{ 
                      color: rule.test ? 'success.main' : 'text.secondary',
                      textDecoration: rule.test ? 'line-through' : 'none'
                    }}
                  >
                    {rule.text}
                  </Typography>
                </Box>
              ))}
            </Stack>
          </Box>
        )}

        {/* Confirm Password Input */}
        <ModernTextField
          fullWidth
          type={showConfirmPassword ? "text" : "password"}
          label="Parola Tekrarı"
          value={credentialsInfo.confirmPassword}
          onChange={(e) => handleConfirmPasswordChange(e.target.value)}
          error={!credentialsValidation.confirmPassword.isValid}
          helperText={
            credentialsValidation.confirmPassword.error ||
            (credentialsInfo.confirmPassword && credentialsInfo.password === credentialsInfo.confirmPassword ? '✅ Parolalar eşleşiyor' : '')
          }
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <Lock color={credentialsValidation.confirmPassword.isValid ? "success" : "primary"} />
              </InputAdornment>
            ),
            endAdornment: (
              <InputAdornment position="end">
                <IconButton
                  aria-label="toggle confirm password visibility"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  edge="end"
                >
                  {showConfirmPassword ? <VisibilityOff /> : <Visibility />}
                </IconButton>
              </InputAdornment>
            ),
          }}
        />

        {/* Error Display */}
        {credentialsError.show && (
          <Alert 
            severity={credentialsError.type as 'error' | 'warning' | 'info'} 
            onClose={() => setCredentialsError({ show: false, message: '', type: 'error' })}
            sx={{ whiteSpace: 'pre-line' }}
          >
            {credentialsError.message}
          </Alert>
        )}

        {/* Security Notice */}
        <Box sx={{ p: 2, borderRadius: 2, bgcolor: 'info.light', color: 'info.contrastText' }}>
          <Typography variant="body2" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Shield sx={{ fontSize: 18 }} />
            <strong>Güvenlik:</strong> Parolanız güvenli şekilde şifrelenerek saklanır.
          </Typography>
        </Box>

        {/* Loading State with Warning */}
        {loading && (
          <Fade in={loading}>
            <Box>
              <Box sx={{ 
                p: 3, 
                borderRadius: 2, 
                bgcolor: 'primary.main', 
                color: 'white',
                textAlign: 'center',
                boxShadow: '0 8px 32px rgba(25, 118, 210, 0.3)'
              }}>
                <CircularProgress 
                  size={48} 
                  thickness={4}
                  sx={{ 
                    color: 'white',
                    mb: 2,
                    filter: 'drop-shadow(0 4px 8px rgba(0,0,0,0.2))'
                  }} 
                />
                <Typography variant="h6" sx={{ fontWeight: 600, mb: 1 }}>
                  Kaydınız Oluşturuluyor...
                </Typography>
                <Typography variant="body2" sx={{ opacity: 0.9 }}>
                  Eczane veritabanınız hazırlanıyor
                </Typography>
              </Box>

              {/* Long Process Warning - shows after 4 seconds */}
              {showLongProcessWarning && (
                <Zoom in={showLongProcessWarning}>
                  <Box sx={{ 
                    mt: 2,
                    p: 2.5, 
                    borderRadius: 2, 
                    bgcolor: 'warning.light',
                    border: '2px solid',
                    borderColor: 'warning.main',
                    animation: 'pulse 2s ease-in-out infinite',
                    '@keyframes pulse': {
                      '0%, 100%': { 
                        transform: 'scale(1)',
                        boxShadow: '0 4px 16px rgba(237, 108, 2, 0.2)'
                      },
                      '50%': { 
                        transform: 'scale(1.02)',
                        boxShadow: '0 6px 24px rgba(237, 108, 2, 0.3)'
                      }
                    }
                  }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                      <Box sx={{ 
                        width: 40, 
                        height: 40, 
                        borderRadius: '50%', 
                        bgcolor: 'warning.main',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        flexShrink: 0
                      }}>
                        <Warning sx={{ color: 'white', fontSize: 24 }} />
                      </Box>
                      <Box sx={{ flex: 1 }}>
                        <Typography variant="subtitle1" sx={{ fontWeight: 700, color: 'warning.dark', mb: 0.5 }}>
                          Bu işlem biraz uzun sürebilir
                        </Typography>
                        <Typography variant="body2" sx={{ color: 'warning.dark', opacity: 0.9 }}>
                          Lütfen sayfayı kapatmayın...
                        </Typography>
                      </Box>
                    </Box>
                  </Box>
                </Zoom>
              )}
            </Box>
          </Fade>
        )}
      </Stack>
    </Box>
  )

  const renderStep = () => {
    switch (activeStep) {
      case 0:
        return renderGLNValidationStep()
      case 1:
        return renderPharmacistInfoStep()
      case 2:
        return renderEmailVerificationStep()
      case 3:
        return renderPhoneVerificationStep()
      case 4:
        return renderCredentialsStep()
      case 5:
        return renderRegistrationCompleteStep()
      default:
        return (
          <Box sx={{ textAlign: 'center', py: 4 }}>
            <AutoAwesome sx={{ fontSize: 64, color: 'primary.main', mb: 2 }} />
            <Typography variant="h5" sx={{ fontWeight: 600, mb: 2 }}>
              Kayıt Tamamlanıyor...
            </Typography>
            <Typography variant="body1" color="text.secondary">
              Son adım yakında eklenecek...
            </Typography>
          </Box>
        )
    }
  }

  if (!mounted) return null

  return (
    <ModernContainer onKeyDown={handleKeyDown}>
      {/* Floating Background Icons */}
      <FloatingIcon sx={{ top: '15%', left: '10%', animationDelay: '0s' }}>
        <Store />
      </FloatingIcon>
      <FloatingIcon sx={{ top: '25%', right: '15%', animationDelay: '2s' }}>
        <Verified />
      </FloatingIcon>
      <FloatingIcon sx={{ bottom: '20%', left: '20%', animationDelay: '4s' }}>
        <Person />
      </FloatingIcon>
      <FloatingIcon sx={{ bottom: '30%', right: '10%', animationDelay: '1s' }}>
        <Shield />
      </FloatingIcon>

      <Box sx={{ position: 'relative', zIndex: 1, width: '100%', maxWidth: { xs: '100%', sm: 800, md: 1000, lg: 1200 } }}>
        <GlassPaper elevation={0}>
          {/* Header */}
          <Box sx={{ textAlign: 'center', mb: 4 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 2, mb: 2 }}>
              <Button 
                component="a" 
                href="/t-login"
                startIcon={<ArrowBack />}
                sx={{ 
                  borderRadius: 2,
                  color: 'text.secondary',
                  '&:hover': { 
                    bgcolor: alpha('#000', 0.05),
                    color: 'primary.main'
                  }
                }}
              >
                Giriş&apos;e Dön
              </Button>
              <Box sx={{ flexGrow: 1 }} />
              <Typography variant="h4" sx={{ fontWeight: 700, color: 'success.main' }}>
                🏪 Eczane Kaydı
              </Typography>
            </Box>
            <Typography variant="body1" color="text.secondary">
              OPAS sistemine eczanenizi kaydetme süreci
            </Typography>
          </Box>

          {/* Modern Progress Indicator */}
          <Box sx={{ mb: 4 }}>
            {/* Desktop View */}
            <Box sx={{ display: { xs: 'none', lg: 'flex' }, justifyContent: 'space-between', alignItems: 'center', position: 'relative', px: 2 }}>
              {steps.map((step, index) => (
                <Box key={step.label} sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', flex: 1, position: 'relative' }}>
                  {/* Connection Line */}
                  {index < steps.length - 1 && (
                    <Box
                      sx={{
                        position: 'absolute',
                        top: 20,
                        left: '50%',
                        right: '-50%',
                        height: 2,
                        bgcolor: index < activeStep ? 'primary.main' : 'grey.300',
                        zIndex: 1,
                        transition: 'all 0.3s ease'
                      }}
                    />
                  )}
                  
                  {/* Step Circle */}
                  <Box
                    sx={{
                      width: 40,
                      height: 40,
                      borderRadius: '50%',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      bgcolor: index <= activeStep ? 'primary.main' : 'grey.100',
                      color: index <= activeStep ? 'white' : 'grey.500',
                      fontWeight: 'bold',
                      fontSize: '1rem',
                      transition: 'all 0.3s ease',
                      boxShadow: index <= activeStep ? '0 4px 16px rgba(25, 118, 210, 0.3)' : '0 2px 8px rgba(0,0,0,0.1)',
                      zIndex: 2,
                      position: 'relative',
                      border: index === activeStep ? '3px solid' : 'none',
                      borderColor: index === activeStep ? 'primary.light' : 'transparent'
                    }}
                  >
                    {index < activeStep ? <CheckCircle sx={{ fontSize: 20 }} /> : index + 1}
                  </Box>
                  
                  {/* Step Content */}
                  <Box sx={{ mt: 2, textAlign: 'center', maxWidth: 180, minWidth: 120 }}>
                    <Typography variant="subtitle2" fontWeight="600" sx={{ mb: 0.5, color: index <= activeStep ? 'text.primary' : 'text.secondary' }}>
                      {step.label}
                    </Typography>
                    <Typography variant="caption" color="text.secondary" sx={{ fontSize: '0.75rem', lineHeight: 1.2 }}>
                      {step.description}
                    </Typography>
                  </Box>
                </Box>
              ))}
            </Box>

            {/* Tablet View */}
            <Box sx={{ display: { xs: 'none', md: 'flex', lg: 'none' }, justifyContent: 'space-between', alignItems: 'center', position: 'relative', px: 1 }}>
              {steps.map((step, index) => (
                <Box key={step.label} sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', flex: 1, position: 'relative' }}>
                  {/* Connection Line */}
                  {index < steps.length - 1 && (
                    <Box
                      sx={{
                        position: 'absolute',
                        top: 18,
                        left: '50%',
                        right: '-50%',
                        height: 2,
                        bgcolor: index < activeStep ? 'primary.main' : 'grey.300',
                        zIndex: 1,
                        transition: 'all 0.3s ease'
                      }}
                    />
                  )}
                  
                  {/* Step Circle */}
                  <Box
                    sx={{
                      width: 36,
                      height: 36,
                      borderRadius: '50%',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      bgcolor: index <= activeStep ? 'primary.main' : 'grey.100',
                      color: index <= activeStep ? 'white' : 'grey.500',
                      fontWeight: 'bold',
                      fontSize: '0.9rem',
                      transition: 'all 0.3s ease',
                      boxShadow: index <= activeStep ? '0 3px 12px rgba(25, 118, 210, 0.3)' : '0 2px 6px rgba(0,0,0,0.1)',
                      zIndex: 2,
                      position: 'relative',
                      border: index === activeStep ? '2px solid' : 'none',
                      borderColor: index === activeStep ? 'primary.light' : 'transparent'
                    }}
                  >
                    {index < activeStep ? <CheckCircle sx={{ fontSize: 18 }} /> : index + 1}
                  </Box>
                  
                  {/* Step Content */}
                  <Box sx={{ mt: 1.5, textAlign: 'center', maxWidth: 140, minWidth: 100 }}>
                    <Typography variant="caption" fontWeight="600" sx={{ mb: 0.5, color: index <= activeStep ? 'text.primary' : 'text.secondary', fontSize: '0.8rem' }}>
                      {step.label}
                    </Typography>
                    <Typography variant="caption" color="text.secondary" sx={{ fontSize: '0.7rem', lineHeight: 1.1 }}>
                      {step.description}
                    </Typography>
                  </Box>
                </Box>
              ))}
            </Box>

            {/* Mobile View */}
            <Box sx={{ display: { xs: 'block', md: 'none' } }}>
              {/* Progress Bar */}
              <Box sx={{ mb: 3 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                  <Typography variant="body2" fontWeight="600">
                    Adım {activeStep + 1} / {steps.length}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    %{Math.round(((activeStep + 1) / steps.length) * 100)}
                  </Typography>
                </Box>
                <Box
                  sx={{
                    width: '100%',
                    height: 6,
                    bgcolor: 'grey.200',
                    borderRadius: 3,
                    overflow: 'hidden'
                  }}
                >
                  <Box
                    sx={{
                      width: `${((activeStep + 1) / steps.length) * 100}%`,
                      height: '100%',
                      bgcolor: 'primary.main',
                      borderRadius: 3,
                      transition: 'width 0.5s ease'
                    }}
                  />
                </Box>
              </Box>

              {/* Current Step Info */}
              <Box sx={{ textAlign: 'center', p: 2, bgcolor: 'background.paper', borderRadius: 2, border: '1px solid', borderColor: 'divider' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', mb: 1 }}>
                  <Box
                    sx={{
                      width: 32,
                      height: 32,
                      borderRadius: '50%',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      bgcolor: 'primary.main',
                      color: 'white',
                      fontWeight: 'bold',
                      fontSize: '0.9rem',
                      mr: 2
                    }}
                  >
                    {activeStep + 1}
                  </Box>
                  <Box sx={{ textAlign: 'left' }}>
                    <Typography variant="subtitle2" fontWeight="600">
                      {steps[activeStep].label}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {steps[activeStep].description}
                    </Typography>
                  </Box>
                </Box>
              </Box>
            </Box>
          </Box>

          {/* Step Content */}
          <Zoom in timeout={500}>
            <Box>
              {renderStep()}
            </Box>
          </Zoom>

          {/* Navigation Buttons */}
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
              disabled={
                (activeStep === 0 && (!glnValidation.isValid || !glnValidation.confirmed)) ||
                (activeStep === 1 && (!isPharmacistInfoValid() || !pharmacistInfo.nviValidated)) ||
                (activeStep === 2 && (!isEmailInfoValid() || !emailInfo.isEmailVerified)) ||
                (activeStep === 3 && (!isPhoneInfoValid() || !phoneInfo.isSmsVerified)) ||
                (activeStep === 4 && !isCredentialsValid())
              }
            >
              {activeStep === steps.length - 1 ? 'Kaydı Tamamla' : 'Devam Et'}
            </UltraButton>
          </Box>
        </GlassPaper>
      </Box>
    </ModernContainer>
  )
}
