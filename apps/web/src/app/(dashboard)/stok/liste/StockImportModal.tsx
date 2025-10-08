'use client';

import React, { useState, useCallback } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Box,
  Typography,
  Stepper,
  Step,
  StepLabel,
  CircularProgress,
  Alert,
  AlertTitle,
  LinearProgress,
  Chip,
  Stack
} from '@mui/material';
import { useDropzone } from 'react-dropzone';
import {
  CloudUpload as UploadIcon,
  Check as CheckIcon,
  Warning as WarningIcon,
  Close as CloseIcon
} from '@mui/icons-material';
import ImportPreviewTable from './ImportPreviewTable';
import ImportResultScreen from './ImportResultScreen';

interface StockImportModalProps {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

interface AnalyzeResponse {
  detectedColumns: DetectedColumn[];
  rows: ImportRow[];
  totalRows: number;
  matchedRows: number;
  unmatchedRows: number;
  warnings?: string[];
}

interface DetectedColumn {
  originalHeader: string;
  detectedField: string;
  columnIndex: number;
  confidenceScore: number;
}

interface ImportRow {
  rowNumber: number;
  rawData: Record<string, string | null>;
  match?: ProductMatch;
  errors?: string[];
  warnings?: string[];
  isNewProduct?: boolean;
  newProductInfo?: {
    productName: string;
    gtin?: string;
    manufacturer?: string;
    price?: number;
  };
}

interface ProductMatch {
  productId: string;
  productName: string;
  gtin?: string;
  matchType: string;
  matchScore: number;
  alternatives?: AlternativeMatch[];
}

interface AlternativeMatch {
  productId: string;
  productName: string;
  gtin?: string;
  matchScore: number;
}

interface ExecuteResponse {
  totalProcessed: number;
  successful: number;
  failed: number;
  errors?: Array<{ rowNumber: number; error: string }>;
}

const steps = ['Dosya Yükle', 'Analiz & Önizleme', 'Toplu Import'];

const funMessages = [
  '🤔 Dosyanızı inceliyoruz...',
  '🔍 Ürünleri arıyoruz...',
  '🧠 Akıllı eşleştirme yapılıyor...',
  '📊 Sonuçlar hazırlanıyor...',
  '✨ Neredeyse bitti...'
];

export default function StockImportModal({ open, onClose, onSuccess }: StockImportModalProps) {
  const [activeStep, setActiveStep] = useState(0);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [analyzing, setAnalyzing] = useState(false);
  const [analyzeResult, setAnalyzeResult] = useState<AnalyzeResponse | null>(null);
  const [importing, setImporting] = useState(false);
  const [importResult, setImportResult] = useState<ExecuteResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [funMessageIndex, setFunMessageIndex] = useState(0);

  const onDrop = useCallback((acceptedFiles: File[]) => {
    if (acceptedFiles.length > 0) {
      setSelectedFile(acceptedFiles[0]);
      setError(null);
    }
  }, []);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    multiple: false,
    maxSize: 10 * 1024 * 1024 // 10MB
  });

  const handleAnalyze = async () => {
    if (!selectedFile) return;

    setAnalyzing(true);
    setError(null);

    // Fun message rotation
    const interval = setInterval(() => {
      setFunMessageIndex((prev) => (prev + 1) % funMessages.length);
    }, 2000);

    try {
      const formData = new FormData();
      formData.append('file', selectedFile);

      const tenantId = localStorage.getItem('tenantId');
      const username = localStorage.getItem('username');

      const response = await fetch('/api/opas/tenant/stock/import/analyze', {
        method: 'POST',
        headers: {
          'X-TenantId': tenantId || '',
          'X-Username': username || ''
        },
        body: formData
      });

      clearInterval(interval);

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || errorData.detail || 'Analiz başarısız');
      }

      const data: AnalyzeResponse = await response.json();
      setAnalyzeResult(data);
      setActiveStep(1);
    } catch (err: unknown) {
      clearInterval(interval);
      setError(err instanceof Error ? err.message : 'Bilinmeyen hata');
    } finally {
      setAnalyzing(false);
    }
  };

  const handleExecuteImport = async (confirmedRows: ImportRow[]) => {
    setImporting(true);
    setError(null);

    try {
      const tenantId = localStorage.getItem('tenantId');
      const username = localStorage.getItem('username');

      const payload = {
        rows: confirmedRows
          .filter(r => r.match || r.isNewProduct)
          .map(r => ({
            rowNumber: r.rowNumber,
            productId: r.match?.productId || 'NEW_PRODUCT_PLACEHOLDER',
            quantity: parseInt(
              Object.values(r.rawData).find(v => v && /^\d+$/.test(v)) || '1'
            ),
            unitCost: null,
            bonusQuantity: null,
            serialNumber: null,
            expiryDate: null,
            notes: r.isNewProduct ? 'Yeni ürün - Dosyadan import' : 'Dosyadan toplu import',
            isNewProduct: r.isNewProduct || false,
            newProduct: r.isNewProduct ? {
              productName: r.newProductInfo!.productName,
              gtin: r.newProductInfo!.gtin,
              manufacturer: r.newProductInfo!.manufacturer,
              price: r.newProductInfo!.price,
              description: 'Dosyadan import edildi'
            } : null
          }))
      };

      const response = await fetch('/api/opas/tenant/stock/import/execute', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-TenantId': tenantId || '',
          'X-Username': username || ''
        },
        body: JSON.stringify(payload)
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || errorData.detail || 'Import başarısız');
      }

      const data: ExecuteResponse = await response.json();
      setImportResult(data);
      setActiveStep(2);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Bilinmeyen hata');
    } finally {
      setImporting(false);
    }
  };

  const handleClose = () => {
    setActiveStep(0);
    setSelectedFile(null);
    setAnalyzeResult(null);
    setImportResult(null);
    setError(null);
    setFunMessageIndex(0);
    onClose();
  };

  const handleSuccessClose = () => {
    handleClose();
    onSuccess();
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="lg" fullWidth>
      <DialogTitle>
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <Typography variant="h6">Dosyadan Stok İçe Aktarma</Typography>
          <Button onClick={handleClose} size="small">
            <CloseIcon />
          </Button>
        </Box>
      </DialogTitle>

      <DialogContent>
        <Stepper activeStep={activeStep} sx={{ mb: 4 }}>
          {steps.map((label) => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            <AlertTitle>Hata</AlertTitle>
            {error}
          </Alert>
        )}

        {/* Step 0: File Upload */}
        {activeStep === 0 && (
          <Box>
            <Box
              {...getRootProps()}
              sx={{
                border: '2px dashed',
                borderColor: isDragActive ? 'primary.main' : 'grey.400',
                borderRadius: 2,
                p: 6,
                textAlign: 'center',
                bgcolor: isDragActive ? 'action.hover' : 'background.default',
                cursor: 'pointer',
                transition: 'all 0.3s',
                '&:hover': {
                  borderColor: 'primary.main',
                  bgcolor: 'action.hover'
                }
              }}
            >
              <input {...getInputProps()} />
              <UploadIcon sx={{ fontSize: 64, color: 'primary.main', mb: 2 }} />
              <Typography variant="h6" gutterBottom>
                {isDragActive ? 'Dosyayı buraya bırakın...' : 'Dosya Sürükleyin veya Tıklayın'}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Desteklenen formatlar: Excel (.xlsx, .xls), CSV, TSV
              </Typography>
              <Typography variant="caption" color="text.secondary" display="block" mt={1}>
                Maksimum dosya boyutu: 10MB
              </Typography>
            </Box>

            {selectedFile && (
              <Alert severity="success" sx={{ mt: 2 }} icon={<CheckIcon />}>
                <AlertTitle>Dosya Seçildi</AlertTitle>
                <Typography variant="body2">
                  <strong>{selectedFile.name}</strong> ({(selectedFile.size / 1024).toFixed(2)} KB)
                </Typography>
              </Alert>
            )}

            {analyzing && (
              <Box sx={{ mt: 3 }}>
                <LinearProgress />
                <Typography variant="body2" color="text.secondary" align="center" mt={2}>
                  {funMessages[funMessageIndex]}
                </Typography>
              </Box>
            )}
          </Box>
        )}

        {/* Step 1: Preview & Edit */}
        {activeStep === 1 && analyzeResult && (
          <Box>
            <Stack direction="row" spacing={2} mb={3}>
              <Chip
                icon={<CheckIcon />}
                label={`${analyzeResult.matchedRows} Eşleşti`}
                color="success"
                variant="outlined"
              />
              <Chip
                icon={<WarningIcon />}
                label={`${analyzeResult.unmatchedRows} Eşleşmedi`}
                color="warning"
                variant="outlined"
              />
              <Chip
                label={`${analyzeResult.totalRows} Toplam Satır`}
                variant="outlined"
              />
            </Stack>

            {analyzeResult.warnings && analyzeResult.warnings.length > 0 && (
              <Alert severity="warning" sx={{ mb: 2 }}>
                <AlertTitle>Uyarılar</AlertTitle>
                {analyzeResult.warnings.map((warning, idx) => (
                  <Typography key={idx} variant="body2">
                    • {warning}
                  </Typography>
                ))}
              </Alert>
            )}

            <ImportPreviewTable
              analyzeResult={analyzeResult}
              onExecute={handleExecuteImport}
              importing={importing}
            />
          </Box>
        )}

        {/* Step 2: Results */}
        {activeStep === 2 && importResult && (
          <ImportResultScreen result={importResult} onClose={handleSuccessClose} />
        )}
      </DialogContent>

      <DialogActions>
        {activeStep === 0 && (
          <>
            <Button onClick={handleClose}>İptal</Button>
            <Button
              variant="contained"
              onClick={handleAnalyze}
              disabled={!selectedFile || analyzing}
              startIcon={analyzing ? <CircularProgress size={20} /> : <UploadIcon />}
            >
              {analyzing ? 'Analiz Ediliyor...' : 'Analiz Et'}
            </Button>
          </>
        )}

        {activeStep === 1 && (
          <Button onClick={handleClose}>Kapat</Button>
        )}

        {activeStep === 2 && (
          <Button variant="contained" onClick={handleSuccessClose}>
            Tamamla
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}

