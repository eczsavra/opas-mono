'use client';

import React from 'react';
import {
  Box,
  Typography,
  Alert,
  AlertTitle,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  Stack
} from '@mui/material';
import {
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Warning as WarningIcon
} from '@mui/icons-material';

interface ImportResultScreenProps {
  result: {
    totalProcessed: number;
    successful: number;
    failed: number;
    errors?: Array<{ rowNumber: number; error: string }>;
  };
  onClose: () => void;
}

export default function ImportResultScreen({ result }: ImportResultScreenProps) {
  const successRate = (result.successful / result.totalProcessed) * 100;

  return (
    <Box>
      {/* Summary */}
      <Box textAlign="center" mb={4}>
        {result.failed === 0 ? (
          <SuccessIcon sx={{ fontSize: 80, color: 'success.main', mb: 2 }} />
        ) : result.successful === 0 ? (
          <ErrorIcon sx={{ fontSize: 80, color: 'error.main', mb: 2 }} />
        ) : (
          <WarningIcon sx={{ fontSize: 80, color: 'warning.main', mb: 2 }} />
        )}

        <Typography variant="h4" gutterBottom>
          {result.failed === 0 ? '🎉 Import Tamamlandı!' : 'Import Kısmen Tamamlandı'}
        </Typography>

        <Stack direction="row" spacing={2} justifyContent="center" mt={3}>
          <Chip
            icon={<SuccessIcon />}
            label={`${result.successful} Başarılı`}
            color="success"
            size="medium"
            sx={{ fontSize: '1rem', px: 2, py: 2.5 }}
          />
          {result.failed > 0 && (
            <Chip
              icon={<ErrorIcon />}
              label={`${result.failed} Başarısız`}
              color="error"
              size="medium"
              sx={{ fontSize: '1rem', px: 2, py: 2.5 }}
            />
          )}
        </Stack>

        <Box mt={2}>
          <Typography variant="body1" color="text.secondary">
            Başarı Oranı: <strong>{successRate.toFixed(1)}%</strong>
          </Typography>
        </Box>
      </Box>

      {/* Success Message */}
      {result.failed === 0 ? (
        <Alert severity="success">
          <AlertTitle>Tüm Ürünler Başarıyla Eklendi</AlertTitle>
          {result.successful} ürün stok sisteminize eklendi. Stok listesini güncelleyebilirsiniz.
        </Alert>
      ) : result.successful > 0 ? (
        <Alert severity="warning" sx={{ mb: 2 }}>
          <AlertTitle>Kısmen Başarılı</AlertTitle>
          {result.successful} ürün eklendi, ancak {result.failed} ürün eklenemedi.
        </Alert>
      ) : (
        <Alert severity="error" sx={{ mb: 2 }}>
          <AlertTitle>İmport Başarısız</AlertTitle>
          Hiçbir ürün eklenemedi. Lütfen hataları kontrol edin.
        </Alert>
      )}

      {/* Error Details */}
      {result.errors && result.errors.length > 0 && (
        <Box mt={3}>
          <Typography variant="h6" gutterBottom>
            Hata Detayları
          </Typography>
          <TableContainer component={Paper}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell width={100}>Satır No</TableCell>
                  <TableCell>Hata Mesajı</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {result.errors.map((error, idx) => (
                  <TableRow key={idx}>
                    <TableCell>{error.rowNumber}</TableCell>
                    <TableCell>
                      <Typography variant="body2" color="error">
                        {error.error}
                      </Typography>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Box>
      )}
    </Box>
  );
}

