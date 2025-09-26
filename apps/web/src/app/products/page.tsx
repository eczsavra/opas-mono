'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Typography,
  TextField,
  Button,
  Card,
  CardContent,
  Chip,
  IconButton,
  InputAdornment,
  Pagination,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControlLabel,
  Switch,
  Skeleton,
  Fab,
  Tooltip
} from '@mui/material';
import {
  Search as SearchIcon,
  Refresh as RefreshIcon,
  Edit as EditIcon,
  Visibility as VisibilityIcon,
  Add as AddIcon,
  LocalPharmacy as PharmacyIcon,
  Business as BusinessIcon,
  AttachMoney as MoneyIcon,
  Schedule as ScheduleIcon,
  Home as HomeIcon,
  ArrowBack as ArrowBackIcon
} from '@mui/icons-material';

interface Product {
  id: string;
  gtin: string;
  drugName: string;
  manufacturerName: string;
  manufacturerGln: string;
  price: number;
  isActive: boolean;
  lastItsSyncAt: string;
  createdAt: string;
  updatedAt: string;
}

interface ProductListResponse {
  data: Product[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNext: boolean;
    hasPrevious: boolean;
  };
  filters: {
    search?: string;
    isActive?: boolean;
  };
}

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const [detailDialogOpen, setDetailDialogOpen] = useState(false);
  
  // Filters
  const [searchTerm, setSearchTerm] = useState('');
  const [activeOnly, setActiveOnly] = useState<boolean | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [pagination, setPagination] = useState({
    totalCount: 0,
    totalPages: 0,
    hasNext: false,
    hasPrevious: false
  });

  const fetchProducts = useCallback(async () => {
    setLoading(true);
    setError(null);
    
    try {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString(),
        ...(searchTerm && { search: searchTerm }),
        ...(activeOnly !== null && { isActive: activeOnly.toString() })
      });

      const response = await fetch(`/api/opas/products?${params}`);
      
      if (!response.ok) {
        throw new Error('Failed to fetch products');
      }

      const result: ProductListResponse = await response.json();
      setProducts(result.data);
      setPagination(result.pagination);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error occurred');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, searchTerm, activeOnly]);

  useEffect(() => {
    fetchProducts();
  }, [fetchProducts]);

  const handleSearch = () => {
    setPage(1);
    fetchProducts();
  };

  const handleRefresh = () => {
    fetchProducts();
  };

  const handleProductDetail = (product: Product) => {
    setSelectedProduct(product);
    setDetailDialogOpen(true);
  };

  const handlePageChange = (event: React.ChangeEvent<unknown>, newPage: number) => {
    setPage(newPage);
  };

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY'
    }).format(price);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('tr-TR');
  };

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ mb: 4 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
          <Button
            variant="outlined"
            startIcon={<ArrowBackIcon />}
            onClick={() => window.history.back()}
            sx={{ borderRadius: 2 }}
          >
            Geri
          </Button>
          <Button
            variant="outlined"
            startIcon={<HomeIcon />}
            href="/"
            sx={{ borderRadius: 2 }}
          >
            Ana Sayfa
          </Button>
        </Box>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <PharmacyIcon sx={{ fontSize: 32, color: 'primary.main' }} />
            <Typography variant="h4" component="h1" fontWeight={600}>
              Ürün Listesi
            </Typography>
          </Box>
          <Chip 
            label={`${pagination.totalCount} Ürün`}
            color="primary"
            variant="outlined"
            size="medium"
          />
        </Box>
      </Box>

      {/* Filters */}
      <Card sx={{ mb: 3, borderRadius: 3, boxShadow: 2 }}>
        <CardContent>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, alignItems: 'center' }}>
            <Box sx={{ flex: '1 1 auto', minWidth: { xs: '100%', md: '50%' } }}>
              <TextField
                fullWidth
                variant="outlined"
                placeholder="Ürün adı, GTIN veya üretici ara..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchIcon color="action" />
                    </InputAdornment>
                  ),
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton onClick={handleSearch} color="primary">
                        <SearchIcon />
                      </IconButton>
                    </InputAdornment>
                  ),
                }}
                sx={{
                  '& .MuiOutlinedInput-root': {
                    borderRadius: 2,
                  }
                }}
              />
            </Box>
            <Box sx={{ flex: '0 0 auto' }}>
              <FormControlLabel
                control={
                  <Switch
                    checked={activeOnly === true}
                    onChange={(e) => setActiveOnly(e.target.checked ? true : null)}
                    color="primary"
                  />
                }
                label="Sadece Aktif Ürünler"
              />
            </Box>
            <Box sx={{ flex: '0 0 auto' }}>
              <Button
                variant="outlined"
                startIcon={<RefreshIcon />}
                onClick={handleRefresh}
                disabled={loading}
                fullWidth
                sx={{ borderRadius: 2, py: 1.5 }}
              >
                Yenile
              </Button>
            </Box>
          </Box>
        </CardContent>
      </Card>

      {/* Error State */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {/* Loading State */}
      {loading && (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)', lg: 'repeat(3, 1fr)' }, gap: 2 }}>
          {[...Array(6)].map((_, index) => (
            <Box key={index}>
              <Card sx={{ borderRadius: 3 }}>
                <CardContent>
                  <Skeleton variant="text" width="60%" height={24} />
                  <Skeleton variant="text" width="80%" height={20} />
                  <Skeleton variant="text" width="40%" height={20} />
                  <Box sx={{ mt: 2, display: 'flex', gap: 1 }}>
                    <Skeleton variant="rounded" width={60} height={24} />
                    <Skeleton variant="rounded" width={80} height={24} />
                  </Box>
                </CardContent>
              </Card>
            </Box>
          ))}
        </Box>
      )}

      {/* Product List */}
      {!loading && products.length > 0 && (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)', lg: 'repeat(3, 1fr)' }, gap: 3 }}>
          {products.map((product) => (
            <Box key={product.id}>
              <Card 
                sx={{ 
                  borderRadius: 3, 
                  boxShadow: 2,
                  transition: 'all 0.2s ease-in-out',
                  '&:hover': {
                    boxShadow: 6,
                    transform: 'translateY(-2px)'
                  }
                }}
              >
                <CardContent sx={{ pb: 2 }}>
                  {/* Header */}
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', mb: 2 }}>
                    <Typography variant="h6" component="h3" sx={{ 
                      fontWeight: 600,
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      display: '-webkit-box',
                      WebkitLineClamp: 2,
                      WebkitBoxOrient: 'vertical',
                      lineHeight: 1.2,
                      minHeight: '2.4em'
                    }}>
                      {product.drugName}
                    </Typography>
                    <Chip
                      size="small"
                      label={product.isActive ? 'Aktif' : 'Pasif'}
                      color={product.isActive ? 'success' : 'default'}
                      variant="outlined"
                    />
                  </Box>

                  {/* GTIN */}
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 1, fontFamily: 'monospace' }}>
                    GTIN: {product.gtin}
                  </Typography>

                  {/* Manufacturer */}
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                    <BusinessIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary" noWrap>
                      {product.manufacturerName}
                    </Typography>
                  </Box>

                  {/* Price */}
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                    <MoneyIcon sx={{ fontSize: 16, color: 'primary.main' }} />
                    <Typography variant="h6" color="primary.main" fontWeight={600}>
                      {formatPrice(product.price)}
                    </Typography>
                  </Box>

                  {/* Last Sync */}
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                    <ScheduleIcon sx={{ fontSize: 14, color: 'text.secondary' }} />
                    <Typography variant="caption" color="text.secondary">
                      Son Sync: {formatDate(product.lastItsSyncAt)}
                    </Typography>
                  </Box>

                  {/* Actions */}
                  <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
                    <Button
                      variant="outlined"
                      size="small"
                      startIcon={<VisibilityIcon />}
                      onClick={() => handleProductDetail(product)}
                      sx={{ borderRadius: 2, flex: 1 }}
                    >
                      Detay
                    </Button>
                    <Tooltip title="Düzenle">
                      <IconButton color="primary" size="small">
                        <EditIcon />
                      </IconButton>
                    </Tooltip>
                  </Box>
                </CardContent>
              </Card>
            </Box>
          ))}
        </Box>
      )}

      {/* Empty State */}
      {!loading && products.length === 0 && (
        <Box sx={{ textAlign: 'center', py: 8 }}>
          <PharmacyIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
          <Typography variant="h5" color="text.secondary" gutterBottom>
            Ürün bulunamadı
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
            {searchTerm ? 'Arama kriterlerinize uygun ürün bulunamadı.' : 'Henüz hiç ürün eklenmemiş.'}
          </Typography>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            sx={{ borderRadius: 3, px: 4, py: 1.5 }}
          >
            ITS&apos;den Ürün Senkronizasyonu Yap
          </Button>
        </Box>
      )}

      {/* Pagination */}
      {!loading && products.length > 0 && (
        <Box sx={{ mt: 4, display: 'flex', justifyContent: 'center' }}>
          <Pagination
            count={pagination.totalPages}
            page={page}
            onChange={handlePageChange}
            color="primary"
            size="large"
            showFirstButton
            showLastButton
            sx={{
              '& .MuiPaginationItem-root': {
                borderRadius: 2,
              }
            }}
          />
        </Box>
      )}

      {/* Floating Action Button */}
      <Fab
        color="primary"
        aria-label="add"
        sx={{
          position: 'fixed',
          bottom: 24,
          right: 24,
        }}
      >
        <AddIcon />
      </Fab>

      {/* Product Detail Dialog */}
      <Dialog
        open={detailDialogOpen}
        onClose={() => setDetailDialogOpen(false)}
        maxWidth="md"
        fullWidth
        PaperProps={{
          sx: { borderRadius: 3 }
        }}
      >
        <DialogTitle sx={{ pb: 1 }}>
          <Typography variant="h5" fontWeight={600}>
            Ürün Detayları
          </Typography>
        </DialogTitle>
        <DialogContent>
          {selectedProduct && (
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)' }, gap: 3, mt: 1 }}>
              <Box>
                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  İlaç Adı
                </Typography>
                <Typography variant="body1" fontWeight={500} sx={{ mb: 2 }}>
                  {selectedProduct.drugName}
                </Typography>

                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  GTIN
                </Typography>
                <Typography variant="body1" fontFamily="monospace" sx={{ mb: 2 }}>
                  {selectedProduct.gtin}
                </Typography>

                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  Üretici
                </Typography>
                <Typography variant="body1" sx={{ mb: 2 }}>
                  {selectedProduct.manufacturerName}
                </Typography>
              </Box>
              <Box>
                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  Fiyat
                </Typography>
                <Typography variant="h5" color="primary.main" fontWeight={600} sx={{ mb: 2 }}>
                  {formatPrice(selectedProduct.price)}
                </Typography>

                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  Durum
                </Typography>
                <Chip
                  label={selectedProduct.isActive ? 'Aktif' : 'Pasif'}
                  color={selectedProduct.isActive ? 'success' : 'default'}
                  sx={{ mb: 2 }}
                />

                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  Son ITS Sync
                </Typography>
                <Typography variant="body2" sx={{ mb: 2 }}>
                  {formatDate(selectedProduct.lastItsSyncAt)}
                </Typography>
              </Box>
            </Box>
          )}
        </DialogContent>
        <DialogActions sx={{ p: 3, pt: 1 }}>
          <Button onClick={() => setDetailDialogOpen(false)} sx={{ borderRadius: 2 }}>
            Kapat
          </Button>
          <Button 
            variant="contained" 
            startIcon={<EditIcon />}
            sx={{ borderRadius: 2 }}
          >
            Düzenle
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
