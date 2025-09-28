'use client';

import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useAuth } from '../../contexts/AuthContext';
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
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControlLabel,
  Switch,
  Skeleton,
  Fab,
  CircularProgress,
  Tooltip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  ToggleButton,
  ToggleButtonGroup,
  Accordion,
  AccordionSummary,
  AccordionDetails
} from '@mui/material';
import {
  Search as SearchIcon,
  Refresh as RefreshIcon,
  Edit as EditIcon,
  Visibility as VisibilityIcon,
  Add as AddIcon,
  LocalPharmacy as PharmacyIcon,
  Business as BusinessIcon,
  AccountBalance as CentralIcon,
  AttachMoney as MoneyIcon,
  Schedule as ScheduleIcon,
  Home as HomeIcon,
  ArrowBack as ArrowBackIcon,
  ViewModule as CardViewIcon,
  ViewList as ListViewIcon,
  ExpandMore as ExpandMoreIcon,
} from '@mui/icons-material';

interface Product {
  id: string;
  gtin: string;
  drugName: string;
  manufacturerName?: string;
  manufacturerGln?: string;
  price: number;
  isActive: boolean;
  isImported: boolean;
  lastItsSyncAt?: string;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

interface ProductListResponse {
  success: boolean;
  data: Product[];
  meta: {
    offset: number;
    limit: number;
    totalItems: number;
    hasMore: boolean;
  };
}

export default function ProductsPage() {
  const { user } = useAuth();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const [detailDialogOpen, setDetailDialogOpen] = useState(false);
  
  // Filters
  const [searchTerm, setSearchTerm] = useState('');
  const [activeOnly, setActiveOnly] = useState<boolean | null>(true); // Default: sadece aktif ürünler
  
  // View mode
  const [viewMode, setViewMode] = useState<'card' | 'list'>('list'); // Default: liste görünümü
  
  // Accordion state
  const [expandedAccordion, setExpandedAccordion] = useState<string | false>(false);
  
          // Letter-based accordion state
          const [letterProducts, setLetterProducts] = useState<{ [letter: string]: Product[] }>({});
          const [letterLoading, setLetterLoading] = useState<{ [letter: string]: boolean }>({});
          const [letterCounts, setLetterCounts] = useState<{ [letter: string]: { active: number, passive: number } }>({});
          
          // Pagination per letter
          const [letterPagination, setLetterPagination] = useState<{ [letter: string]: { offset: number, limit: number, hasMore: boolean } }>({});
  
  // Infinite scroll state (legacy - will be removed)
  const [loadedProducts, setLoadedProducts] = useState<Product[]>([]);
  const [hasMore, setHasMore] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const loadedProductsRef = useRef<Product[]>([]);

  const fetchProductsRef = useRef<(() => Promise<void>) | null>(null);

          // Tüm harfleri oluştur (A-Z, 0-9, özel karakterler)
          const getAllLetters = () => {
            const letters = [];
            
            // A-Z harfleri
            for (let i = 65; i <= 90; i++) {
              letters.push(String.fromCharCode(i));
            }
            
            // 0-9 rakamları (tek grup)
            letters.push('0-9');
            
            // Özel karakterler (tek grup)
            letters.push('Özel Karakterler');
            
            return letters;
          };

          // Letter counts fonksiyonu geçici olarak kaldırıldı - backend sorunu
          // const fetchLetterCounts = ...

  // Belirli bir harfle başlayan ürünleri yükle (pagination ile)
  const fetchProductsByLetter = useCallback(async (letter: string, loadMore = false) => {
    if (!user) return;
    
    // Loading state'i güncelle
    setLetterLoading(prev => ({ ...prev, [letter]: true }));
    
    try {
      // Pagination bilgilerini al
      const currentPagination = letterPagination[letter] || { offset: 0, limit: 50, hasMore: true };
      const offset = loadMore ? currentPagination.offset + currentPagination.limit : 0;
      const limit = 50;
      
      const params = new URLSearchParams({
        ...(searchTerm && { search: searchTerm }),
        ...(activeOnly !== null && { isActive: activeOnly.toString() }),
        letterFilter: letter, // Backend'e harf filtresi gönder
        offset: offset.toString(),
        limit: limit.toString()
      });

      const apiEndpoint = user.role === 'superadmin' 
          ? `/api/opas/central/products?${params}`
          : `/api/opas/products?${params}`;
      
      const response = await fetch(apiEndpoint);
      
      if (!response.ok) {
        throw new Error(`Failed to fetch products: ${response.status}`);
      }

      const result: ProductListResponse = await response.json();
      
      if (!result || !result.data || !result.meta) {
        throw new Error('Invalid response format from server');
      }
      
      // Backend'de zaten filtrelenmiş ürünler geliyor
      const newProducts = result.data;
      
      if (loadMore) {
        // Mevcut ürünlere ekle
        setLetterProducts(prev => ({
          ...prev,
          [letter]: [...(prev[letter] || []), ...newProducts]
        }));
      } else {
        // Yeni ürünlerle değiştir
        setLetterProducts(prev => ({
          ...prev,
          [letter]: newProducts
        }));
      }
      
      // Pagination state'ini güncelle
      setLetterPagination(prev => ({
        ...prev,
        [letter]: {
          offset: offset,
          limit: limit,
          hasMore: result.meta.hasMore
        }
      }));
      
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error occurred');
    } finally {
      setLetterLoading(prev => ({ ...prev, [letter]: false }));
    }
  }, [user, searchTerm, activeOnly, letterPagination]);


  const fetchProducts = useCallback(async (loadMore = false) => {
    if (!user) {
      console.log('⚠️ User not loaded yet, skipping fetch');
      return;
    }
    
    if (loadMore) {
      setLoadingMore(true);
    } else {
      setLoading(true);
      setLoadedProducts([]);
      loadedProductsRef.current = [];
      setHasMore(true);
    }
    setError(null);
    
    try {
      // loadMore için current count'u ref ile al
      const currentCount = loadMore ? loadedProductsRef.current.length : 0;
      const params = new URLSearchParams({
        offset: currentCount.toString(),
        limit: '50', // 50'şer yükle
        ...(searchTerm && { search: searchTerm }),
        ...(activeOnly !== null && { isActive: activeOnly.toString() })
      });

      // SuperAdmin için central products, tenant için tenant products
      const apiEndpoint = user.role === 'superadmin' 
          ? `/api/opas/central/products?${params}`
          : `/api/opas/products?${params}`;
      
      console.log('🔍 Fetching products:', { 
        user: user.username,
        role: user.role, 
        apiEndpoint, 
        params: params.toString(),
        searchTerm: searchTerm,
        loadMore,
        currentCount
      });
      
      const response = await fetch(apiEndpoint);
      
      console.log('📡 Response status:', response.status);
      
      if (!response.ok) {
        const errorText = await response.text();
        console.error('❌ API Error:', { 
          status: response.status, 
          statusText: response.statusText, 
          body: errorText 
        });
        throw new Error(`Failed to fetch products: ${response.status} ${response.statusText}`);
      }

      const result: ProductListResponse = await response.json();
      
      // Response validation
      if (!result || !result.data || !result.meta) {
        throw new Error('Invalid response format from server');
      }
      
      if (loadMore) {
        const newProducts = [...loadedProductsRef.current, ...result.data];
        setLoadedProducts(newProducts);
        loadedProductsRef.current = newProducts;
      } else {
        setLoadedProducts(result.data);
        loadedProductsRef.current = result.data;
      }
      
      // Backend'den gelen hasMore bilgisini kullan
      setHasMore(result.meta.hasMore);
      
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error occurred');
    } finally {
      if (loadMore) {
        setLoadingMore(false);
      } else {
        setLoading(false);
      }
    }
  }, [user, searchTerm, activeOnly]); // loadedProducts.length'i kaldırdık

          useEffect(() => {
            console.log('🔍 useEffect triggered, calling fetchProducts');
            fetchProducts();
          }, [fetchProducts]);

  // fetchProducts'ı ref'e kaydet - her fetchProducts değiştiğinde
  useEffect(() => {
    console.log('🔍 Updating fetchProductsRef.current');
    fetchProductsRef.current = fetchProducts;
  }, [fetchProducts]);

  // Debounced search - searchTerm değiştiğinde 500ms sonra arama yap
  useEffect(() => {
    const timeoutId = setTimeout(() => {
      if (fetchProductsRef.current && user) {
        console.log('🔍 Debounced search triggered for:', searchTerm);
        fetchProductsRef.current();
      }
    }, 500);

    return () => clearTimeout(timeoutId);
  }, [searchTerm, user]);

  // Component mount olduğunda log
  useEffect(() => {
    console.log('🔍 Products page mounted');
    console.log('🔍 User:', user);
    console.log('🔍 handleSearch function:', typeof handleSearch);
  }, []);

  // User değiştiğinde log
  useEffect(() => {
    console.log('🔍 User changed:', user);
    if (user) {
      console.log('🔍 User loaded, handleSearch should work now');
    }
  }, [user]);

  const handleSearch = useCallback(() => {
    console.log('🔍 handleSearch called');
    console.log('🔍 User:', user);
    
    if (!user) {
      console.log('❌ User not loaded, cannot search');
      return;
    }
    
    // Yeni arama için ürünleri sıfırla
    fetchProducts(false);
  }, [user, fetchProducts]);

  const handleRefresh = useCallback(() => {
    fetchProducts(false);
  }, [fetchProducts]);

  const handleProductDetail = (product: Product) => {
    setSelectedProduct(product);
    setDetailDialogOpen(true);
  };

  const loadMoreProducts = useCallback(() => {
    if (hasMore && !loadingMore) {
      fetchProducts(true);
    }
  }, [hasMore, loadingMore, fetchProducts]);

  // Scroll detection için Intersection Observer
  const observerRef = useRef<IntersectionObserver | null>(null);
  const loadMoreRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (observerRef.current) {
      observerRef.current.disconnect();
    }

    observerRef.current = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loadingMore) {
          loadMoreProducts();
        }
      },
      { threshold: 0.1 }
    );

    if (loadMoreRef.current) {
      observerRef.current.observe(loadMoreRef.current);
    }

    return () => {
      if (observerRef.current) {
        observerRef.current.disconnect();
      }
    };
  }, [hasMore, loadingMore, loadMoreProducts]);

  const handleAccordionChange = (panel: string) => (event: React.SyntheticEvent, isExpanded: boolean) => {
    setExpandedAccordion(isExpanded ? panel : false);
    
    // Accordion açıldığında o harfle başlayan ürünleri yükle
    if (isExpanded && (!letterProducts[panel] || letterProducts[panel].length === 0)) {
      fetchProductsByLetter(panel);
    }
  };

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY'
    }).format(price);
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'N/A';
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
            {user?.role === 'superadmin' ? (
              <CentralIcon sx={{ fontSize: 32, color: 'error.main' }} />
            ) : (
              <PharmacyIcon sx={{ fontSize: 32, color: 'primary.main' }} />
            )}
            <Typography variant="h4" component="h1" fontWeight={600}>
              {user?.role === 'superadmin' ? 'Merkezi Ürün Listesi' : 'Ürün Listesi'}
            </Typography>
          </Box>
          <Chip 
            label={`${loadedProducts.length} Ürün`}
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
                onChange={(e) => {
                  console.log('🔍 Search term changed to:', e.target.value);
                  setSearchTerm(e.target.value);
                }}
                onFocus={() => console.log('🔍 Search input focused')}
                onBlur={() => console.log('🔍 Search input blurred')}
                onKeyDown={(e) => {
                  console.log('🔍 Key pressed:', e.key);
                  if (e.key === 'Enter') {
                    console.log('🔍 Enter pressed, calling handleSearch');
                    handleSearch();
                  }
                }}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchIcon color="action" />
                    </InputAdornment>
                  ),
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton onClick={() => {
                        console.log('🔍 Search button clicked');
                        console.log('🔍 handleSearch function:', typeof handleSearch);
                        handleSearch();
                      }} color="primary">
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
            
            {/* View Mode Toggle */}
            <Box sx={{ flex: '0 0 auto' }}>
              <ToggleButtonGroup
                value={viewMode}
                exclusive
                onChange={(event, newViewMode) => {
                  if (newViewMode) {
                    setViewMode(newViewMode);
                  }
                }}
                aria-label="view mode"
                size="small"
                sx={{ borderRadius: 2 }}
              >
                <ToggleButton value="card" aria-label="card view">
                  <Tooltip title="Kart Görünümü">
                    <CardViewIcon />
                  </Tooltip>
                </ToggleButton>
                <ToggleButton value="list" aria-label="list view">
                  <Tooltip title="Liste Görünümü">
                    <ListViewIcon />
                  </Tooltip>
                </ToggleButton>
              </ToggleButtonGroup>
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
      {!loading && loadedProducts.length > 0 && (
        <>
          {/* Card View */}
          {viewMode === 'card' && (
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)', lg: 'repeat(3, 1fr)' }, gap: 3 }}>
          {loadedProducts.map((product) => (
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

                  {/* Accordion View */}
                  {viewMode === 'list' && (
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                      {getAllLetters().map((letter) => {
                        const products = letterProducts[letter] || [];
                        const counts = letterCounts[letter] || { active: 0, passive: 0 };
                        
                        return (
                <Accordion 
                  key={letter}
                  expanded={expandedAccordion === letter}
                  onChange={handleAccordionChange(letter)}
                  sx={{ 
                    borderRadius: 2,
                    '&:before': { display: 'none' },
                    boxShadow: 1,
                    '&:hover': { boxShadow: 2 }
                  }}
                >
                  <AccordionSummary
                    expandIcon={<ExpandMoreIcon />}
                    sx={{
                      bgcolor: 'grey.50',
                      borderRadius: 2,
                      '&.Mui-expanded': {
                        borderRadius: '8px 8px 0 0'
                      }
                    }}
                  >
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                              <Typography variant="h6" fontWeight={600} color="primary">
                                {letter === '0-9' ? 'Rakamlar (0-9)' : 
                                 letter === 'Özel Karakterler' ? 'Özel Karakterler' : 
                                 letter}
                              </Typography>
                              {(counts.active > 0 || counts.passive > 0) && (
                                <Box sx={{ display: 'flex', gap: 1 }}>
                                  {counts.active > 0 && (
                                    <Chip 
                                      label={`${counts.active} aktif`}
                                      size="small"
                                      color="success"
                                      variant="outlined"
                                    />
                                  )}
                                  {counts.passive > 0 && (
                                    <Chip 
                                      label={`${counts.passive} pasif`}
                                      size="small"
                                      color="default"
                                      variant="outlined"
                                    />
                                  )}
                                </Box>
                              )}
                              {letterLoading[letter] && (
                                <CircularProgress size={16} />
                              )}
                            </Box>
                  </AccordionSummary>
                  <AccordionDetails sx={{ p: 0 }}>
                    <TableContainer>
                      <Table size="small">
                        <TableHead>
                          <TableRow sx={{ '& .MuiTableCell-head': { fontWeight: 600, bgcolor: 'grey.100' } }}>
                            <TableCell>Ürün Adı</TableCell>
                            <TableCell>GTIN</TableCell>
                            <TableCell>Üretici</TableCell>
                            <TableCell align="right">Fiyat</TableCell>
                            <TableCell align="center">Durum</TableCell>
                            <TableCell>Son Güncellenme</TableCell>
                            <TableCell align="center">İşlemler</TableCell>
                          </TableRow>
                        </TableHead>
                                <TableBody>
                                  {products.map((product) => (
                            <TableRow 
                              key={product.id}
                              sx={{ 
                                '&:hover': { 
                                  bgcolor: 'action.hover',
                                  cursor: 'pointer'
                                },
                                '&:last-child td, &:last-child th': { border: 0 }
                              }}
                              onClick={() => handleProductDetail(product)}
                            >
                              <TableCell>
                                <Typography variant="body2" fontWeight={500} color="primary.main">
                                  {product.drugName}
                                </Typography>
                              </TableCell>
                              <TableCell>
                                <Typography variant="body2" color="text.secondary" fontFamily="monospace">
                                  {product.gtin}
                                </Typography>
                              </TableCell>
                              <TableCell>
                                <Typography variant="body2" color="text.secondary">
                                  {product.manufacturerName || 'Bilinmiyor'}
                                </Typography>
                              </TableCell>
                              <TableCell align="right">
                                <Chip
                                  size="small"
                                  label={`₺${product.price.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}`}
                                  color="primary"
                                  variant="outlined"
                                  sx={{ fontWeight: 'bold', minWidth: 80 }}
                                />
                              </TableCell>
                              <TableCell align="center">
                                <Chip
                                  size="small"
                                  label={product.isActive ? 'Aktif' : 'Pasif'}
                                  color={product.isActive ? 'success' : 'default'}
                                  variant="outlined"
                                />
                              </TableCell>
                              <TableCell>
                                {product.lastItsSyncAt ? (
                                  <Typography variant="caption" color="text.secondary">
                                    {new Date(product.lastItsSyncAt).toLocaleDateString('tr-TR')}
                                  </Typography>
                                ) : (
                                  <Typography variant="caption" color="text.disabled">
                                    Bilinmiyor
                                  </Typography>
                                )}
                              </TableCell>
                              <TableCell align="center">
                                <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1 }}>
                                  <IconButton 
                                    size="small" 
                                    color="primary"
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      handleProductDetail(product);
                                    }}
                                  >
                                    <VisibilityIcon fontSize="small" />
                                  </IconButton>
                                  <IconButton 
                                    size="small" 
                                    color="secondary"
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      // Edit handler burada olacak
                                    }}
                                  >
                                    <EditIcon fontSize="small" />
                                  </IconButton>
                                </Box>
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                        </TableContainer>
                        
                        {/* Daha Fazla Butonu */}
                        {letterPagination[letter]?.hasMore && (
                          <Box sx={{ p: 2, textAlign: 'center', borderTop: '1px solid', borderColor: 'divider' }}>
                            <Button
                              variant="outlined"
                              onClick={() => fetchProductsByLetter(letter, true)}
                              disabled={letterLoading[letter]}
                              startIcon={letterLoading[letter] ? <CircularProgress size={16} /> : null}
                              sx={{ borderRadius: 2 }}
                            >
                              {letterLoading[letter] ? 'Yükleniyor...' : 'Daha Fazla Yükle'}
                            </Button>
                          </Box>
                        )}
                        
                      </AccordionDetails>
                    </Accordion>
                        );
                      })}
                    </Box>
                  )}
        </>
      )}

      {/* Empty State */}
      {!loading && loadedProducts.length === 0 && (
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
