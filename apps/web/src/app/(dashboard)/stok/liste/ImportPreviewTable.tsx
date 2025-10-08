'use client';

import React, { useState } from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  IconButton,
  Tooltip,
  Button,
  Box,
  Typography,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Autocomplete,
  CircularProgress
} from '@mui/material';
import {
  Check as CheckIcon,
  Warning as WarningIcon,
  Error as ErrorIcon,
  Edit as EditIcon,
  Save as SaveIcon,
  Add as AddIcon
} from '@mui/icons-material';
import Checkbox from '@mui/material/Checkbox';

interface ImportPreviewTableProps {
  analyzeResult: {
    detectedColumns: DetectedColumn[];
    rows: ImportRow[];
    totalRows: number;
    matchedRows: number;
    unmatchedRows: number;
  };
  onExecute: (rows: ImportRow[]) => void;
  importing: boolean;
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

interface Product {
  product_id: string;
  drug_name: string;
  gtin?: string;
}

export default function ImportPreviewTable({
  analyzeResult,
  onExecute,
  importing
}: ImportPreviewTableProps) {
  const [rows, setRows] = useState<ImportRow[]>(analyzeResult.rows);
  const [editingRow, setEditingRow] = useState<number | null>(null);
  const [searchResults, setSearchResults] = useState<Product[]>([]);
  const [searching, setSearching] = useState(false);
  const [newProductRows, setNewProductRows] = useState<Set<number>>(new Set());
  const [editableProductInfo, setEditableProductInfo] = useState<Record<number, {
    productName: string;
    gtin: string;
    manufacturer: string;
    price: string;
  }>>({});

  const getProductNameFromRow = (rawData: Record<string, string | null>): string => {
    const productNameCol = analyzeResult.detectedColumns.find(c => c.detectedField === 'product_name');
    if (productNameCol) {
      return rawData[productNameCol.originalHeader] || 'Bilinmeyen';
    }
    return Object.values(rawData).find(v => v && v.length > 3) || 'Bilinmeyen';
  };

  const getMatchIcon = (row: ImportRow) => {
    if (row.errors && row.errors.length > 0) {
      return <ErrorIcon color="error" />;
    }
    if (!row.match) {
      return <WarningIcon color="warning" />;
    }
    if (row.match.matchType === 'exact_gtin' || row.match.matchScore === 100) {
      return <CheckIcon color="success" />;
    }
    if (row.match.matchScore >= 90) {
      return <CheckIcon color="success" />;
    }
    return <WarningIcon color="warning" />;
  };

  const getMatchStatus = (row: ImportRow): string => {
    if (row.errors && row.errors.length > 0) {
      return 'Hata';
    }
    if (!row.match) {
      return 'Eşleşmedi';
    }
    if (row.match.matchType === 'exact_gtin') {
      return 'Tam Eşleşme (GTIN)';
    }
    if (row.match.matchScore === 100) {
      return 'Tam Eşleşme';
    }
    if (row.match.matchScore >= 90) {
      return `Güçlü Eşleşme (${row.match.matchScore}%)`;
    }
    return `Belirsiz (${row.match.matchScore}%)`;
  };

  const handleEditRow = (rowNumber: number) => {
    setEditingRow(rowNumber);
    setSearchResults([]);
  };

  const handleSearchProduct = async (query: string) => {
    if (query.length < 2) {
      setSearchResults([]);
      return;
    }

    setSearching(true);
    try {
      const tenantId = localStorage.getItem('tenantId');
      const username = localStorage.getItem('username');

      const response = await fetch(
        `/api/opas/tenant/products/search?query=${encodeURIComponent(query)}`,
        {
          headers: {
            'X-TenantId': tenantId || '',
            'X-Username': username || ''
          }
        }
      );

      if (response.ok) {
        const data = await response.json();
        setSearchResults(data.products || []);
      }
    } catch (error) {
      console.error('Search error:', error);
    } finally {
      setSearching(false);
    }
  };

  const handleSaveManualMatch = (rowNumber: number, product: Product) => {
    setRows(prev =>
      prev.map(row =>
        row.rowNumber === rowNumber
          ? {
              ...row,
              match: {
                productId: product.product_id,
                productName: product.drug_name,
                gtin: product.gtin,
                matchType: 'manual',
                matchScore: 100
              }
            }
          : row
      )
    );
    setEditingRow(null);
    setSearchResults([]);
  };

  const handleNewProductToggle = (rowNumber: number) => {
    const newSet = new Set(newProductRows);
    if (newSet.has(rowNumber)) {
      newSet.delete(rowNumber);
      // Remove from editable info
      const newInfo = { ...editableProductInfo };
      delete newInfo[rowNumber];
      setEditableProductInfo(newInfo);
    } else {
      newSet.add(rowNumber);
      // Initialize with data from file
      const row = rows.find(r => r.rowNumber === rowNumber);
      if (row) {
        const productName = getProductNameFromRow(row.rawData);
        const gtinCol = analyzeResult.detectedColumns.find(c => c.detectedField === 'gtin');
        const gtin = gtinCol ? row.rawData[gtinCol.originalHeader] || '' : '';
        
        setEditableProductInfo({
          ...editableProductInfo,
          [rowNumber]: {
            productName,
            gtin,
            manufacturer: '',
            price: ''
          }
        });
      }
    }
    setNewProductRows(newSet);
  };

  const handleProductInfoChange = (rowNumber: number, field: string, value: string) => {
    setEditableProductInfo({
      ...editableProductInfo,
      [rowNumber]: {
        ...editableProductInfo[rowNumber],
        [field]: value
      }
    });
  };

  const matchedRowsCount = rows.filter(r => r.match).length;
  const newProductCount = newProductRows.size;
  const canExecute = matchedRowsCount > 0 || newProductCount > 0;

  return (
    <Box>
      <TableContainer component={Paper} sx={{ maxHeight: 500 }}>
        <Table stickyHeader size="small">
          <TableHead>
            <TableRow>
              <TableCell width={60}>Satır</TableCell>
              <TableCell>Ürün Adı (Dosyadan)</TableCell>
              <TableCell>Eşleşen Ürün</TableCell>
              <TableCell width={150}>Durum</TableCell>
              <TableCell width={80}>İşlem</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row) => (
              <React.Fragment key={row.rowNumber}>
                <TableRow
                  sx={{
                    bgcolor: !row.match
                      ? 'warning.lighter'
                      : row.match.matchScore < 90
                      ? 'warning.lighter'
                      : 'inherit'
                  }}
                >
                  <TableCell>{row.rowNumber}</TableCell>
                  <TableCell>
                    <Typography variant="body2" fontWeight="medium">
                      {getProductNameFromRow(row.rawData)}
                    </Typography>
                    {row.warnings && row.warnings.length > 0 && (
                      <Typography variant="caption" color="warning.main" display="block">
                        {row.warnings[0]}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    {row.match ? (
                      <Box>
                        <Typography variant="body2">{row.match.productName}</Typography>
                        {row.match.gtin && (
                          <Typography variant="caption" color="text.secondary">
                            GTIN: {row.match.gtin}
                          </Typography>
                        )}
                      </Box>
                    ) : (
                      <Box>
                        <Typography variant="body2" color="text.secondary">
                          Eşleşme bulunamadı
                        </Typography>
                        <Box display="flex" alignItems="center" gap={1} mt={0.5}>
                          <Checkbox
                            size="small"
                            checked={newProductRows.has(row.rowNumber)}
                            onChange={() => handleNewProductToggle(row.rowNumber)}
                          />
                          <Typography variant="caption" color="primary">
                            Yeni Ürün Olarak Ekle
                          </Typography>
                        </Box>
                      </Box>
                    )}
                  </TableCell>
                  <TableCell>
                    <Chip
                      icon={getMatchIcon(row)}
                      label={getMatchStatus(row)}
                      size="small"
                      color={
                        row.errors?.length
                          ? 'error'
                          : !row.match
                          ? 'warning'
                          : row.match.matchScore >= 90
                          ? 'success'
                          : 'warning'
                      }
                      variant="outlined"
                    />
                  </TableCell>
                  <TableCell>
                    {(!row.match || row.match.matchScore < 90) && (
                      <Tooltip title="Manuel Eşleştir">
                        <IconButton size="small" onClick={() => handleEditRow(row.rowNumber)}>
                          <EditIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    )}
                  </TableCell>
                </TableRow>
                
                {/* Editable fields for new product */}
                {!row.match && newProductRows.has(row.rowNumber) && (
                  <TableRow>
                    <TableCell colSpan={5} sx={{ bgcolor: 'primary.lighter', p: 2 }}>
                      <Box display="flex" flexDirection="column" gap={2}>
                        <Typography variant="subtitle2" color="primary" fontWeight="bold">
                          <AddIcon fontSize="small" sx={{ verticalAlign: 'middle', mr: 0.5 }} />
                          Yeni Ürün Bilgileri
                        </Typography>
                        <Box display="grid" gridTemplateColumns="1fr 1fr" gap={2}>
                          <TextField
                            label="Ürün Adı *"
                            size="small"
                            fullWidth
                            value={editableProductInfo[row.rowNumber]?.productName || ''}
                            onChange={(e) => handleProductInfoChange(row.rowNumber, 'productName', e.target.value)}
                            onFocus={(e) => e.target.select()}
                          />
                          <TextField
                            label="GTIN / Barkod"
                            size="small"
                            fullWidth
                            value={editableProductInfo[row.rowNumber]?.gtin || ''}
                            onChange={(e) => handleProductInfoChange(row.rowNumber, 'gtin', e.target.value)}
                            onFocus={(e) => e.target.select()}
                          />
                          <TextField
                            label="Üretici Firma"
                            size="small"
                            fullWidth
                            value={editableProductInfo[row.rowNumber]?.manufacturer || ''}
                            onChange={(e) => handleProductInfoChange(row.rowNumber, 'manufacturer', e.target.value)}
                            onFocus={(e) => e.target.select()}
                          />
                          <TextField
                            label="Fiyat (₺)"
                            size="small"
                            fullWidth
                            type="number"
                            value={editableProductInfo[row.rowNumber]?.price || ''}
                            onChange={(e) => handleProductInfoChange(row.rowNumber, 'price', e.target.value)}
                            onFocus={(e) => e.target.select()}
                          />
                        </Box>
                      </Box>
                    </TableCell>
                  </TableRow>
                )}
              </React.Fragment>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <Box mt={3} display="flex" justifyContent="space-between" alignItems="center">
        <Box>
          <Typography variant="body2" color="text.secondary">
            ✓ {matchedRowsCount} eşleştirildi
            {newProductCount > 0 && ` | ➕ ${newProductCount} yeni ürün`}
            {' '}/ {rows.length} toplam
          </Typography>
        </Box>
        <Button
          variant="contained"
          onClick={() => {
            // Add new product info to rows before executing
            const rowsWithNewProducts = rows.map(row => {
              if (newProductRows.has(row.rowNumber) && editableProductInfo[row.rowNumber]) {
                const info = editableProductInfo[row.rowNumber];
                return {
                  ...row,
                  isNewProduct: true,
                  newProductInfo: {
                    productName: info.productName,
                    gtin: info.gtin || undefined,
                    manufacturer: info.manufacturer || undefined,
                    price: info.price ? parseFloat(info.price) : undefined
                  }
                };
              }
              return row;
            });
            onExecute(rowsWithNewProducts);
          }}
          disabled={!canExecute || importing}
          startIcon={importing ? <CircularProgress size={20} /> : <SaveIcon />}
        >
          {importing 
            ? 'İmport Ediliyor...' 
            : `${matchedRowsCount + newProductCount} Ürünü İmport Et`}
        </Button>
      </Box>

      {/* Manual Match Dialog */}
      <Dialog open={editingRow !== null} onClose={() => setEditingRow(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Manuel Ürün Eşleştir</DialogTitle>
        <DialogContent>
          <Box mt={2}>
            <Autocomplete
              freeSolo
              options={searchResults}
              getOptionLabel={(option) =>
                typeof option === 'string' ? option : option.drug_name
              }
              loading={searching}
              onInputChange={(_, value) => {
                handleSearchProduct(value);
              }}
              onChange={(_, value) => {
                if (value && typeof value !== 'string' && editingRow !== null) {
                  handleSaveManualMatch(editingRow, value);
                }
              }}
              renderInput={(params) => (
                <TextField
                  {...params}
                  label="Ürün Ara"
                  placeholder="Ürün adı yazın..."
                  InputProps={{
                    ...params.InputProps,
                    endAdornment: (
                      <>
                        {searching ? <CircularProgress size={20} /> : null}
                        {params.InputProps.endAdornment}
                      </>
                    )
                  }}
                />
              )}
              renderOption={(props, option) => {
                const { key, ...optionProps } = props as React.HTMLAttributes<HTMLLIElement> & { key: string };
                return (
                  <li key={key} {...optionProps}>
                    <Box>
                      <Typography variant="body2">{option.drug_name}</Typography>
                      {option.gtin && (
                        <Typography variant="caption" color="text.secondary">
                          GTIN: {option.gtin}
                        </Typography>
                      )}
                    </Box>
                  </li>
                );
              }}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditingRow(null)}>İptal</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

