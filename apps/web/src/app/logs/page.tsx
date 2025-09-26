'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Container,
  Typography,
  Card,
  CardContent,
  Chip,
  TextField,
  Button,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Pagination,
  Alert,
  Tooltip,
  Switch,
  FormControlLabel,
  LinearProgress
} from '@mui/material';
import {
  Search as SearchIcon,
  Download as DownloadIcon,
  Refresh as RefreshIcon,
  Visibility as ViewIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Timeline as TimelineIcon,
  BarChart as ChartIcon,
  ArrowBack as ArrowBackIcon
} from '@mui/icons-material';
import { styled } from '@mui/material/styles';

// 🎨 PROFESYONEL STYLES

const ProfessionalCard = styled(Card)(() => ({
  backgroundColor: '#ffffff',
  border: '1px solid #e2e8f0',
  borderRadius: '8px',
  boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)',
  transition: 'all 0.2s ease-in-out',
  '&:hover': {
    boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)',
  },
}));

const ProfessionalButton = styled(Button)(() => ({
  backgroundColor: '#3b82f6',
  borderRadius: '6px',
  padding: '8px 16px',
  color: 'white',
  fontWeight: 500,
  textTransform: 'none',
  boxShadow: 'none',
  transition: 'all 0.2s ease',
  '&:hover': {
    backgroundColor: '#2563eb',
    boxShadow: '0 2px 4px rgba(0, 0, 0, 0.1)',
  },
}));

const LogLevelChip = styled(Chip)<{ level: string }>(({ level }) => ({
  borderRadius: '4px',
  fontWeight: 500,
  textTransform: 'uppercase',
  fontSize: '0.75rem',
  height: '24px',
  ...(level === 'error' && {
    backgroundColor: '#fef2f2',
    color: '#dc2626',
    border: '1px solid #fecaca',
  }),
  ...(level === 'warning' && {
    backgroundColor: '#fffbeb',
    color: '#d97706',
    border: '1px solid #fed7aa',
  }),
  ...(level === 'info' && {
    backgroundColor: '#eff6ff',
    color: '#2563eb',
    border: '1px solid #bfdbfe',
  }),
  ...(level === 'success' && {
    backgroundColor: '#f0fdf4',
    color: '#16a34a',
    border: '1px solid #bbf7d0',
  }),
}));

// 🎯 TYPES - EN SAĞLAMI
interface LogEntry {
  id: number;
  timestamp: string;
  level: 'error' | 'warning' | 'info' | 'success';
  message: string;
  user: string;
  ip: string;
  tenantId?: string;
  tenantName?: string;
  details?: Record<string, unknown>;
}

interface LogFilters {
  startDate: string;
  endDate: string;
  level: string;
  search: string;
  tenantId?: string;
}

interface LogAnalytics {
  totalLogs: number;
  activeUsers: number;
  totalTenants: number;
  dailyActivity: Array<{ date: string; count: number }>;
  levelDistribution: {
    info: number;
    warning: number;
    error: number;
    success: number;
  };
  topUsers: Array<{ username: string; count: number }>;
}

// 🚀 ULTIMATE LOG DASHBOARD - EN MÜKEMMELİ
export default function UltimateLogDashboard() {
  // 🎯 STATE MANAGEMENT - EN SAĞLAMI
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [analytics, setAnalytics] = useState<LogAnalytics | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // Bugün ve yarın - UTC timezone farkını kapsayacak şekilde
  const today = new Date();
  const tomorrow = new Date(today);
  tomorrow.setDate(tomorrow.getDate() + 1);
  
  const [filters, setFilters] = useState<LogFilters>({
    startDate: today.toISOString().split('T')[0],
    endDate: tomorrow.toISOString().split('T')[0],
    level: '',
    search: '',
  });
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 50,
    totalCount: 0,
    totalPages: 0,
  });
  const [realTime, setRealTime] = useState(false);

  // 📡 API CALLS - EN SAĞLAMI
  const fetchLogs = useCallback(async () => {
    setLoading(true);
    setError(null);
    
    try {
      // Tarih düzeltmesi: aynı gün seçilmişse ertesi güne al
      let adjustedEndDate = filters.endDate;
      if (filters.endDate && filters.startDate === filters.endDate) {
        const nextDay = new Date(filters.endDate);
        nextDay.setDate(nextDay.getDate() + 1);
        adjustedEndDate = nextDay.toISOString().split('T')[0];
      }
      
      const params = new URLSearchParams({
        page: pagination.page.toString(),
        pageSize: pagination.pageSize.toString(),
        ...(filters.startDate && { startDate: filters.startDate }),
        ...(adjustedEndDate && { endDate: adjustedEndDate }),
        ...(filters.level && { level: filters.level }),
        ...(filters.search && { search: filters.search }),
        ...(filters.tenantId && { tenantId: filters.tenantId }),
      });

      const response = await fetch(`/api/opas/logs/tenant/TNT_229714?${params}`);
      
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      
      if (data.success) {
        setLogs(data.data);
        setPagination(prev => ({
          ...prev,
          totalCount: data.pagination.totalCount,
          totalPages: data.pagination.totalPages,
        }));
      } else {
        throw new Error(data.error || 'Failed to fetch logs');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
      console.error('Error fetching logs:', err);
    } finally {
      setLoading(false);
    }
  }, [filters, pagination.page, pagination.pageSize]);

  const fetchAnalytics = useCallback(async () => {
    try {
      // Tarih düzeltmesi: aynı gün seçilmişse ertesi güne al
      let adjustedEndDate = filters.endDate;
      if (filters.endDate && filters.startDate === filters.endDate) {
        const nextDay = new Date(filters.endDate);
        nextDay.setDate(nextDay.getDate() + 1);
        adjustedEndDate = nextDay.toISOString().split('T')[0];
      }
      
      const params = new URLSearchParams({
        ...(filters.startDate && { startDate: filters.startDate }),
        ...(adjustedEndDate && { endDate: adjustedEndDate }),
        ...(filters.tenantId && { tenantId: filters.tenantId }),
      });

      const response = await fetch(`/api/opas/logs/analytics?${params}`);
      
      if (response.ok) {
        const data = await response.json();
        if (data.success) {
          setAnalytics(data.data);
        }
      }
    } catch (err) {
      console.error('Error fetching analytics:', err);
    }
  }, [filters]);

  // 🔄 REAL-TIME UPDATES - EN ÇARPICI
  useEffect(() => {
    if (realTime) {
      const interval = setInterval(() => {
        fetchLogs();
        fetchAnalytics();
      }, 5000); // 5 saniyede bir güncelle

      return () => clearInterval(interval);
    }
  }, [realTime, filters, fetchLogs, fetchAnalytics]);

  // 🎯 EFFECTS - EN MÜKEMMELİ
  useEffect(() => {
    fetchLogs();
    fetchAnalytics();
  }, [fetchLogs, fetchAnalytics]);

  // 🎨 HANDLERS - EN KULLANIŞLI
  const handleFilterChange = (field: keyof LogFilters, value: string) => {
    setFilters(prev => ({ ...prev, [field]: value }));
    setPagination(prev => ({ ...prev, page: 1 }));
  };

  const handlePageChange = (event: React.ChangeEvent<unknown>, page: number) => {
    setPagination(prev => ({ ...prev, page }));
  };

  const handleExport = async (format: 'json' | 'csv') => {
    try {
      const params = new URLSearchParams({
        format,
        ...(filters.startDate && { startDate: filters.startDate }),
        ...(filters.endDate && { endDate: filters.endDate }),
        ...(filters.level && { level: filters.level }),
        ...(filters.tenantId && { tenantId: filters.tenantId }),
      });

      const response = await fetch(`/api/opas/logs/export?${params}`);
      
      if (response.ok) {
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `logs_export_${new Date().toISOString().split('T')[0]}.${format}`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
      }
    } catch (err) {
      console.error('Export failed:', err);
    }
  };

  const getLevelIcon = (level: string) => {
    switch (level) {
      case 'error': return <ErrorIcon color="error" />;
      case 'warning': return <WarningIcon color="warning" />;
      case 'info': return <InfoIcon color="info" />;
      case 'success': return <SuccessIcon color="success" />;
      default: return <InfoIcon />;
    }
  };


  // 🎨 RENDER - PROFESYONEL
  return (
    <Box sx={{ 
      minHeight: '100vh', 
      backgroundColor: '#f8fafc',
      padding: 3,
    }}>
      <Container maxWidth="xl">
        {/* 🎯 HEADER - PROFESYONEL */}
        <Box sx={{ mb: 4 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
            <Button
              variant="outlined"
              startIcon={<ArrowBackIcon />}
              onClick={() => window.location.href = '/'}
              sx={{
                borderColor: '#d1d5db',
                color: '#374151',
                '&:hover': {
                  borderColor: '#9ca3af',
                  backgroundColor: '#f9fafb',
                }
              }}
            >
              Ana Ekrana Dön
            </Button>
            <Typography 
              variant="h4" 
              component="h1" 
              sx={{ 
                fontWeight: 600, 
                color: '#1e293b',
                flex: 1,
              }}
            >
              Sistem Logları
            </Typography>
          </Box>
          <Typography variant="body1" sx={{ color: '#64748b' }}>
            Eczane işlemleri ve sistem olaylarının detaylı kayıtları
          </Typography>
        </Box>

        {/* 🎛️ CONTROLS - PROFESYONEL */}
        <ProfessionalCard sx={{ mb: 3 }}>
            <CardContent>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3, alignItems: 'center' }}>
                <Box sx={{ flex: '1 1 300px', minWidth: 300 }}>
                  <TextField
                    fullWidth
                    label="Arama"
                    value={filters.search}
                    onChange={(e) => handleFilterChange('search', e.target.value)}
                    InputProps={{
                      startAdornment: <SearchIcon sx={{ mr: 1, color: 'text.secondary' }} />,
                    }}
                    sx={{ '& .MuiOutlinedInput-root': { borderRadius: '6px' } }}
                  />
                </Box>
                <Box sx={{ flex: '1 1 200px', minWidth: 200 }}>
                  <FormControl fullWidth>
                    <InputLabel>Seviye</InputLabel>
                    <Select
                      value={filters.level}
                      onChange={(e) => handleFilterChange('level', e.target.value)}
                      sx={{ borderRadius: '6px' }}
                    >
                      <MenuItem value="">Tümü</MenuItem>
                      <MenuItem value="error">Hata</MenuItem>
                      <MenuItem value="warning">Uyarı</MenuItem>
                      <MenuItem value="info">Bilgi</MenuItem>
                      <MenuItem value="success">Başarı</MenuItem>
                    </Select>
                  </FormControl>
                </Box>
                <Box sx={{ flex: '1 1 200px', minWidth: 200 }}>
                  <TextField
                    fullWidth
                    type="date"
                    label="Başlangıç"
                    value={filters.startDate}
                    onChange={(e) => handleFilterChange('startDate', e.target.value)}
                    InputLabelProps={{ shrink: true }}
                    sx={{ '& .MuiOutlinedInput-root': { borderRadius: '6px' } }}
                  />
                </Box>
                <Box sx={{ flex: '1 1 200px', minWidth: 200 }}>
                  <TextField
                    fullWidth
                    type="date"
                    label="Bitiş"
                    value={filters.endDate}
                    onChange={(e) => handleFilterChange('endDate', e.target.value)}
                    InputLabelProps={{ shrink: true }}
                    sx={{ '& .MuiOutlinedInput-root': { borderRadius: '6px' } }}
                  />
                </Box>
                <Box sx={{ flex: '1 1 300px', minWidth: 300 }}>
                  <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                    <ProfessionalButton
                      startIcon={<RefreshIcon />}
                      onClick={fetchLogs}
                      disabled={loading}
                    >
                      Yenile
                    </ProfessionalButton>
                    <Button
                      startIcon={<DownloadIcon />}
                      onClick={() => handleExport('json')}
                      variant="outlined"
                      sx={{ 
                        borderColor: '#d1d5db',
                        color: '#374151',
                        '&:hover': {
                          borderColor: '#9ca3af',
                          backgroundColor: '#f9fafb',
                        }
                      }}
                    >
                      JSON
                    </Button>
                    <Button
                      startIcon={<DownloadIcon />}
                      onClick={() => handleExport('csv')}
                      variant="outlined"
                      sx={{ 
                        borderColor: '#d1d5db',
                        color: '#374151',
                        '&:hover': {
                          borderColor: '#9ca3af',
                          backgroundColor: '#f9fafb',
                        }
                      }}
                    >
                      CSV
                    </Button>
                  </Box>
                </Box>
              </Box>
            </CardContent>
          </ProfessionalCard>

        {/* 📊 ANALYTICS - PROFESYONEL */}
        {analytics && (
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3, mb: 3 }}>
            <Box sx={{ flex: '1 1 250px', minWidth: 250 }}>
              <ProfessionalCard>
                <CardContent sx={{ textAlign: 'center' }}>
                  <Typography variant="h4" sx={{ fontWeight: 600, color: '#1e293b' }}>
                    {analytics.totalLogs.toLocaleString()}
                  </Typography>
                  <Typography variant="body2" sx={{ color: '#64748b' }}>
                    Toplam Log
                  </Typography>
                </CardContent>
              </ProfessionalCard>
            </Box>
            <Box sx={{ flex: '1 1 250px', minWidth: 250 }}>
              <ProfessionalCard>
                <CardContent sx={{ textAlign: 'center' }}>
                  <Typography variant="h4" sx={{ fontWeight: 600, color: '#16a34a' }}>
                    {analytics.activeUsers}
                  </Typography>
                  <Typography variant="body2" sx={{ color: '#64748b' }}>
                    Aktif Kullanıcı
                  </Typography>
                </CardContent>
              </ProfessionalCard>
            </Box>
            <Box sx={{ flex: '1 1 250px', minWidth: 250 }}>
              <ProfessionalCard>
                <CardContent sx={{ textAlign: 'center' }}>
                  <Typography variant="h4" sx={{ fontWeight: 600, color: '#d97706' }}>
                    {analytics.totalTenants}
                  </Typography>
                  <Typography variant="body2" sx={{ color: '#64748b' }}>
                    Toplam Eczane
                  </Typography>
                </CardContent>
              </ProfessionalCard>
            </Box>
            <Box sx={{ flex: '1 1 250px', minWidth: 250 }}>
              <ProfessionalCard>
                <CardContent sx={{ textAlign: 'center' }}>
                  <Typography variant="h4" sx={{ fontWeight: 600, color: '#dc2626' }}>
                    {analytics.levelDistribution.error}
                  </Typography>
                  <Typography variant="body2" sx={{ color: '#64748b' }}>
                    Kritik Hata
                  </Typography>
                </CardContent>
              </ProfessionalCard>
            </Box>
          </Box>
        )}

        {/* 📋 LOG TABLE - PROFESYONEL */}
        <ProfessionalCard>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6" sx={{ fontWeight: 600, color: '#1e293b' }}>
                  Log Listesi ({pagination.totalCount.toLocaleString()} kayıt)
                </Typography>
                <Box sx={{ display: 'flex', gap: 1 }}>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={realTime}
                        onChange={(e) => setRealTime(e.target.checked)}
                        color="primary"
                      />
                    }
                    label="Canlı Güncelleme"
                  />
                  <Tooltip title="Tablo Görünümü">
                    <IconButton>
                      <ViewIcon />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Zaman Çizelgesi">
                    <IconButton>
                      <TimelineIcon />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Grafik Görünümü">
                    <IconButton>
                      <ChartIcon />
                    </IconButton>
                  </Tooltip>
                </Box>
              </Box>

              {loading && <LinearProgress sx={{ mb: 2 }} />}

              {error && (
                <Alert severity="error" sx={{ mb: 2 }}>
                  {error}
                </Alert>
              )}

              <TableContainer component={Paper} sx={{ borderRadius: '8px', overflow: 'hidden' }}>
                <Table>
                  <TableHead>
                    <TableRow sx={{ backgroundColor: '#f8fafc' }}>
                      <TableCell sx={{ color: '#374151', fontWeight: 600 }}>Zaman</TableCell>
                      <TableCell sx={{ color: '#374151', fontWeight: 600 }}>Seviye</TableCell>
                      <TableCell sx={{ color: '#374151', fontWeight: 600 }}>Mesaj</TableCell>
                      <TableCell sx={{ color: '#374151', fontWeight: 600 }}>Kullanıcı</TableCell>
                      <TableCell sx={{ color: '#374151', fontWeight: 600 }}>IP</TableCell>
                      <TableCell sx={{ color: '#374151', fontWeight: 600 }}>İşlem</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {logs.map((log) => (
                      <TableRow 
                        key={log.id}
                        sx={{ 
                          '&:hover': { 
                            backgroundColor: '#f8fafc',
                          },
                        }}
                      >
                        <TableCell>
                          <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                            {new Date(log.timestamp).toLocaleString('tr-TR')}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <LogLevelChip 
                            label={log.level.toUpperCase()} 
                            level={log.level}
                            icon={getLevelIcon(log.level)}
                          />
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" sx={{ maxWidth: 300 }}>
                            {log.message}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" sx={{ fontWeight: 500 }}>
                            {log.user}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                            {log.ip}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Tooltip title="Detayları Görüntüle">
                            <IconButton 
                              size="small"
                              sx={{ 
                                backgroundColor: '#f1f5f9',
                                '&:hover': { backgroundColor: '#e2e8f0' }
                              }}
                            >
                              <ViewIcon />
                            </IconButton>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>

              {/* 📄 PAGINATION - EN KULLANIŞLI */}
              <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
                <Pagination
                  count={pagination.totalPages}
                  page={pagination.page}
                  onChange={handlePageChange}
                  color="primary"
                  size="large"
                  sx={{
                    '& .MuiPaginationItem-root': {
                      borderRadius: '6px',
                      fontWeight: 500,
                    }
                  }}
                />
              </Box>
            </CardContent>
          </ProfessionalCard>
      </Container>
    </Box>
  );
}
