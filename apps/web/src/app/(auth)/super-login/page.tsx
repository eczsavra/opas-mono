'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  Container,
  Paper,
  Box,
  Typography,
  TextField,
  Button,
  Alert,
  CircularProgress,
  Chip,
} from '@mui/material';
import {
  SupervisorAccount as SuperAdminIcon,
  Login as LoginIcon,
} from '@mui/icons-material';
import { useAuth } from '../../contexts/AuthContext';

export default function LoginPage() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const { loginAsSuperAdmin, isAuthenticated, logout } = useAuth();
  const router = useRouter();

  // Eğer zaten giriş yapılmışsa logout seçeneği sun
  const handleLogout = () => {
    logout();
    setError('');
    setUsername('');
    setPassword('');
  };

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!username.trim() || !password.trim()) {
      setError('Kullanıcı adı ve şifre zorunludur');
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      const success = await loginAsSuperAdmin(username.trim(), password);

      if (success) {
        router.push('/');
      } else {
        setError('Geçersiz kullanıcı adı veya şifre');
      }
    } catch (error) {
      setError('Giriş yapılırken bir hata oluştu');
      console.error('Login error:', error);
    } finally {
      setIsLoading(false);
    }
  };

  // Eğer zaten giriş yapılmışsa farklı UI göster
  if (isAuthenticated) {
    return (
      <Container maxWidth="sm" sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center' }}>
        <Paper elevation={8} sx={{ width: '100%', p: 4 }}>
          {/* Header */}
          <Box sx={{ textAlign: 'center', mb: 4 }}>
            <Typography variant="h3" fontWeight={700} color="primary" gutterBottom>
              OPAS
            </Typography>
            <Typography variant="h6" color="text.secondary" gutterBottom>
              Eczane Yönetim Sistemi
            </Typography>
            <Chip
              icon={<SuperAdminIcon />}
              label="Zaten Giriş Yapılmış"
              color="success"
              variant="outlined"
              sx={{ mt: 2, fontWeight: 600 }}
            />
          </Box>

          {/* Already Logged In Info */}
          <Box sx={{ mb: 4, p: 3, bgcolor: 'success.50', borderRadius: 1, border: 1, borderColor: 'success.200' }}>
            <Typography variant="h6" fontWeight={600} color="success.main" gutterBottom>
              ✅ Aktif Oturum
            </Typography>
            <Typography variant="body1" gutterBottom>
              Halihazırda SuperAdmin olarak giriş yapmışsınız.
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Dashboard&apos;a gitmek veya çıkış yapmak için aşağıdaki seçenekleri kullanın.
            </Typography>
          </Box>

          {/* Action Buttons */}
          <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
            <Button
              fullWidth
              variant="contained"
              size="large"
              onClick={() => router.push('/')}
              color="primary"
              sx={{ 
                py: 1.5,
                textTransform: 'none',
                fontWeight: 600,
                fontSize: '1.1rem',
              }}
            >
              📊 Dashboard&apos;a Git
            </Button>
            
            <Button
              fullWidth
              variant="outlined"
              size="large"
              onClick={handleLogout}
              color="error"
              sx={{ 
                py: 1.5,
                textTransform: 'none',
                fontWeight: 600,
                fontSize: '1.1rem',
              }}
            >
              🔓 Çıkış Yap
            </Button>
          </Box>

          {/* Footer */}
          <Box sx={{ mt: 4, textAlign: 'center' }}>
            <Typography variant="caption" color="text.secondary">
              © 2025 OPAS - Eczane Yönetim Sistemi
            </Typography>
          </Box>
        </Paper>
      </Container>
    );
  }

  return (
    <Container maxWidth="sm" sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center' }}>
      <Paper elevation={8} sx={{ width: '100%', p: 4 }}>
        {/* Header */}
        <Box sx={{ textAlign: 'center', mb: 4 }}>
          <Typography variant="h3" fontWeight={700} color="primary" gutterBottom>
            OPAS
          </Typography>
          <Typography variant="h6" color="text.secondary" gutterBottom>
            Eczane Yönetim Sistemi
          </Typography>
          <Chip
            icon={<SuperAdminIcon />}
            label="SuperAdmin Paneli"
            color="error"
            variant="outlined"
            sx={{ mt: 2, fontWeight: 600 }}
          />
        </Box>

        {/* SuperAdmin Info */}
        <Box sx={{ mb: 4, p: 2, bgcolor: 'error.50', borderRadius: 1, border: 1, borderColor: 'error.200' }}>
          <Typography variant="body1" fontWeight={600} color="error.main" gutterBottom>
            🔒 Yetkili Personel Girişi
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Bu panel sadece OPAS sistem yöneticileri içindir. Tüm eczaneleri ve sistem ayarlarını yönetebilirsiniz.
          </Typography>
        </Box>

        {/* Login Form */}
        <form onSubmit={handleLogin}>
          <TextField
            fullWidth
            label="Kullanıcı Adı"
            variant="outlined"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            margin="normal"
            required
            autoComplete="username"
            disabled={isLoading}
            sx={{ mb: 2 }}
          />

          <TextField
            fullWidth
            label="Şifre"
            type="password"
            variant="outlined"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            margin="normal"
            required
            autoComplete="current-password"
            disabled={isLoading}
            sx={{ mb: 3 }}
          />

          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          <Button
            type="submit"
            fullWidth
            variant="contained"
            size="large"
            startIcon={isLoading ? <CircularProgress size={20} /> : <LoginIcon />}
            disabled={isLoading}
            color="error"
            sx={{ 
              py: 1.5,
              textTransform: 'none',
              fontWeight: 600,
              fontSize: '1.1rem',
            }}
          >
            {isLoading ? 'Giriş Yapılıyor...' : 'SuperAdmin Girişi'}
          </Button>
        </form>

        {/* Demo Credentials */}
        <Box sx={{ mt: 4, p: 2, bgcolor: 'grey.50', borderRadius: 1 }}>
          <Typography variant="caption" color="text.secondary" display="block" gutterBottom>
            <strong>Demo Bilgileri:</strong>
          </Typography>
          <Typography variant="caption" color="text.secondary">
            Kullanıcı: <strong>admin</strong> | Şifre: <strong>Opas2024!</strong>
          </Typography>
        </Box>

        {/* Footer */}
        <Box sx={{ mt: 4, textAlign: 'center' }}>
          <Typography variant="caption" color="text.secondary">
            © 2025 OPAS - Eczane Yönetim Sistemi
          </Typography>
        </Box>
      </Paper>
    </Container>
  );
}