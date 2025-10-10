'use client'

import React, { useState, useCallback, useEffect, useMemo } from 'react'
import { 
  Box, 
  TextField,
  Typography,
  alpha,
  Autocomplete,
  InputAdornment,
  CircularProgress,
  IconButton,
  Paper,
  Card,
  Divider,
  Chip,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Button,
  Snackbar,
  Alert
} from '@mui/material'
import { 
  Search as SearchIcon, 
  ContentPaste as PasteIcon,
  Clear as ClearIcon,
  Delete as DeleteIcon,
  CheckCircle as SuccessIcon,
  Receipt as ReceiptIcon
} from '@mui/icons-material'
import { styled } from '@mui/material/styles'
import { useSalesContext } from '@/contexts/SalesContext'
import { useProductSearch } from '@/hooks/useProductSearch'
import { parseDataMatrix, isDataMatrix, extractGTIN, type DataMatrixResult } from '@/utils/dataMatrixParser'

const StyledAutocomplete = styled(Autocomplete<Product, false, false, true>)(({ theme }) => ({
  '& .MuiOutlinedInput-root': {
    borderRadius: 16,
    backgroundColor: alpha(theme.palette.background.paper, 0.9),
    backdropFilter: 'blur(10px)',
    border: `2px solid ${alpha(theme.palette.primary.main, 0.1)}`,
    padding: '8px 14px',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    '&:hover': {
      borderColor: alpha(theme.palette.primary.main, 0.3),
      backgroundColor: theme.palette.background.paper,
    },
    '&.Mui-focused': {
      borderColor: theme.palette.primary.main,
      backgroundColor: theme.palette.background.paper,
      boxShadow: `0 8px 24px ${alpha(theme.palette.primary.main, 0.15)}`,
    },
    '& fieldset': {
      border: 'none',
    },
    '& .MuiAutocomplete-input': {
      padding: '8px 4px !important',
      fontSize: '1rem',
      fontWeight: 500,
    }
  },
  '& .MuiAutocomplete-clearIndicator': {
    visibility: 'visible',
    color: theme.palette.text.secondary,
    '&:hover': {
      color: theme.palette.error.main,
      backgroundColor: alpha(theme.palette.error.main, 0.08),
    }
  },
}))

const StyledPaper = styled(Paper)(({ theme }) => ({
  marginTop: 8,
  borderRadius: 12,
  boxShadow: `0 8px 32px ${alpha(theme.palette.common.black, 0.12)}`,
  border: `1px solid ${alpha(theme.palette.divider, 0.1)}`,
  overflow: 'hidden',
  '& .MuiAutocomplete-listbox': {
    padding: 4,
    maxHeight: '400px',
    '&::-webkit-scrollbar': {
      width: 6,
    },
    '&::-webkit-scrollbar-thumb': {
      backgroundColor: alpha(theme.palette.text.secondary, 0.2),
      borderRadius: 3,
    },
  },
  '& .MuiAutocomplete-option': {
    padding: '12px 14px',
    borderRadius: 8,
    margin: '2px 0',
    transition: 'all 0.15s ease',
    '&[aria-selected="true"]': {
      backgroundColor: `${alpha(theme.palette.primary.main, 0.12)} !important`,
    },
    '&:hover': {
      backgroundColor: `${alpha(theme.palette.action.hover, 0.08)} !important`,
    },
    '&.Mui-focused': {
      backgroundColor: `${alpha(theme.palette.primary.main, 0.08)} !important`,
    },
  },
}))

interface Product {
  id: string
  gtin: string
  drugName: string
  manufacturerName: string
  price: number
  quantity?: number // Adet bilgisi
  category?: string // PHARMACEUTICAL | OTC
  unitCost?: number // Maliyet (stock_summary'den gelecek)
  stockQuantity?: number // Gerçek stok miktarı
  // Karekod bilgileri (opsiyonel)
  serialNumber?: string
  expiryDate?: string
  lotNumber?: string
}

interface ModernSearchBoxProps {
  tabId: string // Hangi tab için çalışıyor
  onProductSelect?: (product: Product) => void
}

export default function ModernSearchBox({ tabId, onProductSelect }: ModernSearchBoxProps) {
  const { saleTabs, updateTab } = useSalesContext()
  const currentTab = saleTabs.find(tab => tab.id === tabId)
  
  // ✅ Wrap selectedProducts in useMemo to prevent unnecessary re-renders
  const selectedProducts = useMemo(() => currentTab?.products || [], [currentTab?.products])

  const [inputValue, setInputValue] = useState('')
  const [options, setOptions] = useState<Product[]>([])
  const [loading, setLoading] = useState(false)
  const [highlightedIndex, setHighlightedIndex] = useState<number>(0)
  const [open, setOpen] = useState(false)
  const [searchSource, setSearchSource] = useState<'backend' | 'cache'>('backend')
  const listboxRef = React.useRef<HTMLUListElement>(null)
  const inputRef = React.useRef<HTMLInputElement>(null) // ⚠️ Ref for auto-focus
  
  // Success Notification State
  const [successNotification, setSuccessNotification] = useState<{
    open: boolean
    saleNumber: string
    totalAmount: number
  }>({
    open: false,
    saleNumber: '',
    totalAmount: 0
  })
  
  // ✅ Use IndexedDB-powered search hook
  const { searchProducts: searchWithCache } = useProductSearch()

  // ⚠️ AUTO-FOCUS: When tab changes or component mounts, focus search box
  useEffect(() => {
    const timer = setTimeout(() => {
      if (inputRef.current) {
        inputRef.current.focus()
      }
    }, 100) // Small delay to ensure component is fully rendered

    return () => clearTimeout(timer)
  }, [tabId]) // Re-run when tabId changes (tab switch)

  const searchProducts = useCallback(async (query: string) => {
    if (query.length < 2) {
      setOptions([])
      setHighlightedIndex(0)
      setOpen(false)
      return
    }

    setLoading(true)
    
    try {
      // ✅ Search with IndexedDB cache fallback
      const result = await searchWithCache(query, 10)
      
      setSearchSource(result.source)
      setOptions(result.products)
      setHighlightedIndex(0)
      setOpen(result.products.length > 0)
      
      if (result.source === 'cache') {
        console.log('🔵 Offline mode: Using cached products')
      }
    } catch (error) {
      console.error('Search error:', error)
      setOptions([])
      setHighlightedIndex(0)
      setOpen(false)
    } finally {
      setLoading(false)
    }
  }, [searchWithCache])

  // addProduct - searchByGTIN'den önce tanımlanmalı
  const addProduct = useCallback((product: Product) => {
    // Ürün zaten listede var mı kontrol et
    const existingIndex = selectedProducts.findIndex(p => p.gtin === product.gtin)
    
    let updatedProducts: Product[]
    if (existingIndex !== -1) {
      // Varsa adetini arttır
      updatedProducts = [...selectedProducts]
      updatedProducts[existingIndex] = {
        ...updatedProducts[existingIndex],
        quantity: (updatedProducts[existingIndex].quantity || 1) + 1
      }
    } else {
      // Yoksa yeni ekle (adet: 1)
      updatedProducts = [...selectedProducts, { ...product, quantity: 1 }]
    }

    // Context'e kaydet (localStorage'a otomatik kaydedilecek)
    updateTab(tabId, { products: updatedProducts })
    
    if (onProductSelect) {
      onProductSelect(product)
    }
    setInputValue('')
    setOptions([])
    setHighlightedIndex(0)
    setOpen(false)
  }, [selectedProducts, tabId, updateTab, onProductSelect])

  const searchByGTIN = useCallback(async (gtin: string, dataMatrixResult?: DataMatrixResult) => {
    setLoading(true)
    
    try {
      const tenantId = localStorage.getItem('tenantId')
      const username = localStorage.getItem('username')
      
      if (!tenantId || !username) {
        console.error('Missing credentials')
        return
      }

      // GTIN ile backend'de ara
      const response = await fetch(`/api/opas/tenant/products/search?query=${encodeURIComponent(gtin)}&username=${encodeURIComponent(username)}`, {
        headers: {
          'X-TenantId': tenantId,
          'X-Username': username
        }
      })

      if (!response.ok) {
        throw new Error('GTIN search failed')
      }

       const data = await response.json()
       
       if (data && data.length > 0) {
         const product = {
           id: data[0].product_id,
           gtin: data[0].gtin || gtin,
           drugName: data[0].drug_name,
           manufacturerName: data[0].manufacturer_name || '',
           price: data[0].price || 0,
           quantity: 1,
           category: data[0].category || 'DRUG', // İTS'den gelen ürünler ilaç
           unitCost: data[0].unit_cost,
           stockQuantity: data[0].stock_quantity ?? 0, // Gerçek stok bilgisi
           // Karekod bilgilerini ekle
           ...(dataMatrixResult && {
             serialNumber: dataMatrixResult.serialNumber || undefined,
             expiryDate: dataMatrixResult.expiryDate || undefined,
             lotNumber: dataMatrixResult.lotNumber || undefined
           })
         }
        
        // Direkt ekle
        addProduct(product)
        
        console.log('✅ Ürün GTIN ile bulundu ve eklendi:', dataMatrixResult ? 'Karekoddan' : 'Barkoddan')
      } else {
        console.log('❌ GTIN bulunamadı:', gtin)
        setInputValue('')
      }
    } catch (error) {
      console.error('GTIN search error:', error)
    } finally {
      setLoading(false)
    }
  }, [addProduct])

  const handleInputChange = async (_event: React.SyntheticEvent, newInputValue: string) => {
    setInputValue(newInputValue)
    
    // Karekod veya GTIN kontrolü
    if (isDataMatrix(newInputValue)) {
      // Karekod parse et
      const result = parseDataMatrix(newInputValue)
      if (result.isValid && result.gtin) {
        // GTIN ile ürün ara
        await searchByGTIN(result.gtin, result)
        return
      }
    }
    
    // GTIN ile arama
    const gtin = extractGTIN(newInputValue)
    if (gtin) {
      await searchByGTIN(gtin)
      return
    }
    
    // Normal isim araması
    searchProducts(newInputValue)
  }

  const handleChange = () => {
    // Devre dışı - manuel kontrol kullanılıyor
  }

  const salesSummary = {
    totalItems: selectedProducts.reduce((sum, p) => sum + (p.quantity || 1), 0),
    subtotal: selectedProducts.reduce((sum, p) => sum + ((p.price || 0) * (p.quantity || 1)), 0),
    discount: 0,
    total: selectedProducts.reduce((sum, p) => sum + ((p.price || 0) * (p.quantity || 1)), 0)
  }

  const handlePaste = async () => {
    try {
      const text = await navigator.clipboard.readText()
      setInputValue(text)
      searchProducts(text)
    } catch (err) {
      console.error('Clipboard access failed:', err)
    }
  }

  useEffect(() => {
    if (open && listboxRef.current) {
      const listbox = listboxRef.current
      const highlightedItem = listbox.querySelector(`li:nth-child(${highlightedIndex + 1})`) as HTMLElement
      
      if (highlightedItem) {
        const listboxRect = listbox.getBoundingClientRect()
        const itemRect = highlightedItem.getBoundingClientRect()
        
        if (itemRect.bottom > listboxRect.bottom) {
          highlightedItem.scrollIntoView({ block: 'end', behavior: 'smooth' })
        } else if (itemRect.top < listboxRect.top) {
          highlightedItem.scrollIntoView({ block: 'start', behavior: 'smooth' })
        }
      }
    }
  }, [highlightedIndex, open])

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (!open && options.length > 0 && (event.key === 'ArrowDown' || event.key === 'ArrowUp')) {
      setOpen(true)
      return
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault()
      event.stopPropagation()
      if (open) {
        setHighlightedIndex(prev => prev === options.length - 1 ? 0 : prev + 1)
      }
    } else if (event.key === 'ArrowUp') {
      event.preventDefault()
      event.stopPropagation()
      if (open) {
        setHighlightedIndex(prev => prev === 0 ? options.length - 1 : prev - 1)
      }
    } else if (event.key === 'Enter' && options.length > 0 && open) {
      event.preventDefault()
      event.stopPropagation()
      const selectedProduct = options[highlightedIndex]
      if (selectedProduct) {
        addProduct(selectedProduct)
      }
    } else if (event.key === 'Escape') {
      event.preventDefault()
      setOpen(false)
    }
  }

  return (
    <Box 
      sx={{ 
        display: 'grid',
        gridTemplateColumns: '1fr 320px',
        gap: 3,
        width: '100%',
        maxWidth: '100%', // ⚠️ CRITICAL: Never exceed parent width
        height: '100%',
        overflow: 'hidden',
        position: 'relative',
        boxSizing: 'border-box' // ⚠️ Include padding/border in width calc
      }}
    >
      {/* Offline Indicator */}
      {searchSource === 'cache' && (
        <Chip
          label="📡 Offline Mode - Cached Data"
          color="warning"
          size="small"
          sx={{
            position: 'absolute',
            top: 8,
            right: 8,
            zIndex: 10,
            fontWeight: 600
          }}
        />
      )}
      
      {/* Sol Taraf - Arama + Accordion */}
      <Box 
        sx={{ 
          display: 'flex', 
          flexDirection: 'column', 
          minWidth: 0,
          overflow: 'hidden'
        }}
      >
        <Box sx={{ flexShrink: 0, mb: 2 }}>
          <StyledAutocomplete
        freeSolo
        open={open}
        disableListWrap
        options={options}
        loading={loading}
        inputValue={inputValue}
        onInputChange={handleInputChange}
        onChange={handleChange}
        onClose={() => setOpen(false)}
        clearIcon={<ClearIcon fontSize="small" />}
        getOptionLabel={(option) => {
          if (typeof option === 'string') return option
          return (option as Product).drugName
        }}
        filterOptions={(x) => x}
        noOptionsText={
          inputValue.length < 2 
            ? "En az 2 karakter girin"
            : "Ürün bulunamadı"
        }
        PaperComponent={StyledPaper}
        ListboxProps={{
          ref: listboxRef,
        }}

        renderInput={(params) => (
          <TextField
            {...params}
            inputRef={inputRef} // ⚠️ Connect ref for auto-focus
            placeholder="Ara, Sor, Keşfet!"
            onKeyDown={handleKeyDown}
            InputProps={{
              ...params.InputProps,
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon sx={{ color: 'primary.main', opacity: 0.7 }} />
                </InputAdornment>
              ),
              endAdornment: (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                  {loading && <CircularProgress size={20} />}
                  <IconButton 
                    onClick={handlePaste} 
                    size="small"
                    sx={{ 
                      color: 'primary.main',
                      backgroundColor: alpha('#1976d2', 0.08),
                      '&:hover': {
                        backgroundColor: alpha('#1976d2', 0.15),
                      }
                    }}
                  >
                    <PasteIcon fontSize="small" />
                  </IconButton>
                  {params.InputProps.endAdornment}
                </Box>
              ),
            }}
          />
        )}
        renderOption={(props, option, state) => {
          const isHighlighted = state.index === highlightedIndex
          const { ...otherProps } = props
          return (
            <Box 
              component="li" 
              {...otherProps}
              key={option.id}
              onClick={(e) => {
                e.preventDefault()
                e.stopPropagation()
                addProduct(option)
              }}
              sx={{
                backgroundColor: isHighlighted ? `${alpha('#1976d2', 0.12)} !important` : undefined,
                cursor: 'pointer',
              }}
            >
              <Box sx={{ width: '100%', pointerEvents: 'none' }}>
                <Typography variant="body2" sx={{ fontWeight: 600, mb: 0.5, color: 'text.primary' }}>
                  {option.drugName}
                </Typography>
                <Typography variant="caption" sx={{ color: 'text.secondary', display: 'block', mb: 0.5 }}>
                  GTIN: {option.gtin} | {option.manufacturerName}
                </Typography>
                <Typography variant="caption" sx={{ fontWeight: 700, color: 'primary.main' }}>
                  {option.price?.toFixed(2) || '0.00'} ₺
                </Typography>
              </Box>
            </Box>
          )
        }}
      />
        </Box>
        
        {/* Ürün Listesi - Scrollable Alan */}
        <Box sx={{ 
          flex: 1, 
          overflow: 'auto', 
          minHeight: 0,
          minWidth: 0,
          '&::-webkit-scrollbar': {
            width: '6px',
          },
          '&::-webkit-scrollbar-track': {
            background: 'transparent',
          },
          '&::-webkit-scrollbar-thumb': {
            background: alpha('#1976d2', 0.2),
            borderRadius: '3px',
            '&:hover': {
              background: alpha('#1976d2', 0.4),
            }
          },
          scrollbarWidth: 'thin',
          scrollbarColor: `${alpha('#1976d2', 0.2)} transparent`,
        }}>
          {selectedProducts.map((product, index) => (
            <Accordion 
              key={`${product.id}-${index}`}
              sx={{ 
                mb: 1.5,
                borderRadius: '12px !important',
                border: `1px solid ${alpha('#1976d2', 0.12)}`,
                boxShadow: `0 2px 8px ${alpha('#000', 0.04)}`,
                overflow: 'hidden',
                '&:before': { display: 'none' },
                '&.Mui-expanded': {
                  margin: '0 0 12px 0 !important',
                  boxShadow: `0 4px 16px ${alpha('#1976d2', 0.12)}`,
                }
              }}
            >
              <AccordionSummary
                sx={{
                  borderRadius: '12px',
                  '&:hover': {
                    backgroundColor: alpha('#1976d2', 0.04),
                  },
                  '& .MuiAccordionSummary-content': {
                    margin: '12px 0',
                    alignItems: 'center',
                    width: '100%',
                  }
                }}
              >
                <Box sx={{ 
                  display: 'flex', 
                  alignItems: 'center', 
                  justifyContent: 'space-between',
                  width: '100%',
                  gap: 2,
                  overflow: 'hidden'
                }}>
                  {/* Sol: Numara */}
                  <Chip 
                    label={`#${index + 1}`} 
                    size="small"
                    sx={{ 
                      fontWeight: 700,
                      minWidth: 45,
                      background: `linear-gradient(135deg, #1976d2 0%, #42a5f5 100%)`,
                      color: 'white'
                    }}
                  />
                  
                  {/* Orta: Ürün Adı */}
                  <Typography 
                    variant="body2" 
                    sx={{ 
                      flex: 1,
                      minWidth: 0,
                      fontWeight: 600,
                      color: 'text.primary',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap'
                    }}
                  >
                    {product.drugName}
                  </Typography>

                  {/* Sağ: Kategori, Adet, Maliyet/Fiyat ve Sil Butonu */}
                         <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'center', flexShrink: 0 }}>
                           {/* Kategori Badge */}
                           <Chip 
                             label={product.category === 'DRUG' || product.category === 'PHARMACEUTICAL' ? 'İlaç' : 'OTC'}
                             size="small"
                             sx={{ 
                               fontWeight: 600,
                               backgroundColor: (product.category === 'DRUG' || product.category === 'PHARMACEUTICAL')
                                 ? alpha('#2196f3', 0.12) 
                                 : alpha('#4caf50', 0.12),
                               color: (product.category === 'DRUG' || product.category === 'PHARMACEUTICAL') ? '#1565c0' : '#2e7d32',
                               border: `1px solid ${(product.category === 'DRUG' || product.category === 'PHARMACEUTICAL') ? alpha('#2196f3', 0.3) : alpha('#4caf50', 0.3)}`
                             }}
                           />
                           
                           {/* Adet */}
                           <Chip 
                             label={`Adet: ${product.quantity || 1}`}
                             size="small"
                             sx={{ 
                               fontWeight: 600,
                               backgroundColor: alpha('#4caf50', 0.12),
                               color: '#2e7d32',
                               border: `1px solid ${alpha('#4caf50', 0.3)}`
                             }}
                           />
                           
                            {/* Stok Bilgisi - GERÇEK VERİ */}
                            <Chip 
                              label={`Stok: ${product.stockQuantity ?? 0}`}
                              size="small"
                              sx={{ 
                                fontWeight: 600,
                                backgroundColor: (product.stockQuantity ?? 0) > 10 
                                  ? alpha('#4caf50', 0.12) 
                                  : (product.stockQuantity ?? 0) > 0 
                                    ? alpha('#ff9800', 0.12) 
                                    : alpha('#f44336', 0.12),
                                color: (product.stockQuantity ?? 0) > 10 
                                  ? '#2e7d32' 
                                  : (product.stockQuantity ?? 0) > 0 
                                    ? '#f57c00' 
                                    : '#d32f2f',
                                border: `1px solid ${(product.stockQuantity ?? 0) > 10 
                                  ? alpha('#4caf50', 0.3) 
                                  : (product.stockQuantity ?? 0) > 0 
                                    ? alpha('#ff9800', 0.3) 
                                    : alpha('#f44336', 0.3)}`
                              }}
                            />
                           
                           {/* Maliyet/Fiyat */}
                           <Typography variant="caption" sx={{ fontSize: '0.75rem', fontWeight: 700, color: 'primary.main' }}>
                             {product.unitCost ? `${product.unitCost.toFixed(1)}` : '-'}/{product.price.toFixed(2)}₺
                           </Typography>
                    <Box
                      component="span"
                      onClick={(e) => {
                        e.stopPropagation()
                        const updatedProducts = selectedProducts.filter((_, i) => i !== index)
                        updateTab(tabId, { products: updatedProducts })
                      }}
                      sx={{
                        display: 'inline-flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        width: 32,
                        height: 32,
                        borderRadius: '8px',
                        color: 'error.main',
                        backgroundColor: alpha('#d32f2f', 0.08),
                        cursor: 'pointer',
                        transition: 'all 0.2s ease',
                        '&:hover': {
                          backgroundColor: alpha('#d32f2f', 0.15),
                          transform: 'scale(1.1)',
                        }
                      }}
                    >
                      <DeleteIcon fontSize="small" />
                    </Box>
                  </Box>
                </Box>
              </AccordionSummary>

              <AccordionDetails sx={{ pt: 0, pb: 2 }}>
                <Box sx={{ 
                  display: 'flex', 
                  flexDirection: 'column', 
                  gap: 2,
                  backgroundColor: alpha('#f5f5f5', 0.5),
                  borderRadius: 2,
                  p: 2
                }}>
                  {/* Detay Bilgileri */}
                  <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1.5 }}>
                    <Box>
                      <Typography variant="caption" sx={{ color: 'text.secondary', display: 'block', mb: 0.5 }}>
                        GTIN
                      </Typography>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>
                        {product.gtin}
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" sx={{ color: 'text.secondary', display: 'block', mb: 0.5 }}>
                        Üretici
                      </Typography>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>
                        {product.manufacturerName}
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" sx={{ color: 'text.secondary', display: 'block', mb: 0.5 }}>
                        Birim Fiyat
                      </Typography>
                      <Typography variant="body2" sx={{ fontWeight: 700, color: 'primary.main' }}>
                        {product.price?.toFixed(2) || '0.00'} ₺
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" sx={{ color: 'text.secondary', display: 'block', mb: 0.5 }}>
                        Toplam Fiyat
                      </Typography>
                      <Typography variant="body2" sx={{ fontWeight: 700, color: 'success.main' }}>
                        {product.price?.toFixed(2) || '0.00'} ₺
                      </Typography>
                    </Box>
                  </Box>

                </Box>
              </AccordionDetails>
            </Accordion>
          ))}
        </Box>
      </Box>

       {/* Sağ Taraf - Satış Özeti - YENİ TASARIM */}
       <Card 
         elevation={3}
         sx={{ 
           width: '100%',
           minWidth: 0,
           borderRadius: 3,
           background: '#fff',
           border: `1px solid ${alpha('#000', 0.08)}`,
           height: '100%',
           maxHeight: 'calc(100vh - 200px)',
           display: 'flex',
           flexDirection: 'column',
           overflow: 'hidden'
         }}
       >
        {/* Header - Kompakt */}
        <Box sx={{ 
          p: 2, 
          borderBottom: `2px solid ${alpha('#1976d2', 0.1)}`,
          background: `linear-gradient(135deg, ${alpha('#1976d2', 0.03)} 0%, ${alpha('#42a5f5', 0.03)} 100%)`
        }}>
          <Typography variant="h6" sx={{ fontWeight: 800, color: '#1976d2', fontSize: '1.1rem' }}>
            SATIŞ ÖZETİ
          </Typography>
        </Box>

        {/* Stats - Kompakt */}
        <Box sx={{ 
          p: 2, 
          flex: 1, 
          display: 'flex', 
          flexDirection: 'column', 
          gap: 1.5,
          overflow: 'auto',
          minHeight: 0
        }}>
          {/* Ürün Sayısı + Toplam */}
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Typography variant="caption" sx={{ color: 'text.secondary', fontWeight: 600, fontSize: '0.75rem' }}>
              ÜRÜN
            </Typography>
            <Typography variant="body2" sx={{ fontWeight: 700 }}>
              {salesSummary.totalItems} Adet
            </Typography>
          </Box>

          <Divider />

          {/* Ödeme Yöntemi */}
          <Box>
            <Typography variant="caption" sx={{ color: 'text.secondary', fontWeight: 600, fontSize: '0.75rem', mb: 0.5, display: 'block' }}>
              ÖDEME YÖNTEMİ
            </Typography>
            <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 0.5 }}>
              {['CASH', 'CARD', 'CREDIT', 'CONSIGNMENT', 'IBAN', 'QR'].map((method) => (
                <Button
                  key={method}
                  variant={currentTab?.paymentMethod === method ? 'contained' : 'outlined'}
                  size="small"
                  onClick={() => updateTab(tabId, { paymentMethod: method })}
                  sx={{
                    py: 0.4,
                    px: 0.3,
                    fontWeight: 600,
                    fontSize: '0.65rem',
                    borderRadius: 1,
                    textTransform: 'none',
                    minHeight: 26,
                    ...(currentTab?.paymentMethod === method && {
                      bgcolor: '#1976d2',
                      color: 'white'
                    })
                  }}
                >
                  {method === 'CASH' ? 'Nakit' :
                   method === 'CARD' ? 'Kart' :
                   method === 'CREDIT' ? 'Veresiye' :
                   method === 'CONSIGNMENT' ? 'Emanet' :
                   method === 'IBAN' ? 'IBAN' : 'QR'}
                </Button>
              ))}
            </Box>
          </Box>

          <Divider />

          {/* Satış Tipi */}
          <Box>
            <Typography variant="caption" sx={{ color: 'text.secondary', fontWeight: 600, fontSize: '0.75rem', mb: 0.5, display: 'block' }}>
              SATIŞ TİPİ
            </Typography>
            <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 0.5 }}>
              {['NORMAL', 'CONSIGNMENT'].map((type) => (
                <Button
                  key={type}
                  variant={currentTab?.saleType === type ? 'contained' : 'outlined'}
                  size="small"
                  onClick={() => updateTab(tabId, { saleType: type })}
                  sx={{
                    py: 0.4,
                    fontWeight: 600,
                    fontSize: '0.7rem',
                    borderRadius: 1,
                    textTransform: 'none',
                    minHeight: 26,
                    ...(currentTab?.saleType === type && {
                      bgcolor: '#2e7d32',
                      color: 'white'
                    })
                  }}
                >
                  {type === 'NORMAL' ? 'Normal Satış' : 'Emanet'}
                </Button>
              ))}
            </Box>
          </Box>

          <Box sx={{ flex: 1 }} /> {/* Spacer */}

          <Divider />

          {/* Toplam Tutar */}
          <Box sx={{ 
            p: 1.5, 
            bgcolor: alpha('#1976d2', 0.05), 
            borderRadius: 2,
            border: `2px solid ${alpha('#1976d2', 0.15)}`
          }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="body2" sx={{ fontWeight: 700, color: '#1976d2' }}>
                TOPLAM
              </Typography>
              <Typography variant="h6" sx={{ fontWeight: 800, color: '#1976d2' }}>
                {salesSummary.total.toFixed(2)} ₺
              </Typography>
            </Box>
          </Box>
        </Box>

        {/* Tamamla Butonu - Footer - SABIT - ALWAYS VISIBLE */}
        <Box 
          id="sales-complete-button-container"
          sx={{ 
            p: 1.5, 
            borderTop: `3px solid ${alpha('#2e7d32', 0.3)}`,
            flexShrink: 0,
            backgroundColor: alpha('#f5f5f5', 0.5),
            position: 'sticky',
            bottom: 0,
            zIndex: 10
          }}>
          <Button
            variant="contained"
            fullWidth
            disabled={selectedProducts.length === 0 || !currentTab?.paymentMethod}
            onClick={async () => {
                if (!currentTab || selectedProducts.length === 0 || !currentTab.paymentMethod) return;

                try {
                  const tenantId = localStorage.getItem('tenantId');
                  const username = localStorage.getItem('username');

                  if (!tenantId || !username) {
                    alert('Lütfen giriş yapın');
                    return;
                  }

                  // Payload hazırla
                  const payload = {
                    tabId: currentTab.id,
                    items: selectedProducts.map(p => ({
                      productId: p.id,
                      productName: p.drugName,
                      productCategory: p.category,
                      quantity: p.quantity || 1,
                      unitPrice: p.price,
                      unitCost: p.unitCost || 0,
                      discountRate: 0,
                      totalPrice: (p.price || 0) * (p.quantity || 1),
                      serialNumber: p.serialNumber || null,
                      expiryDate: p.expiryDate || null,
                      lotNumber: p.lotNumber || null,
                      gtin: p.gtin || null
                    })),
                    payment: {
                      method: currentTab.paymentMethod || 'CASH',
                      amount: salesSummary.total,
                      notes: null
                    },
                    saleType: currentTab.saleType || 'NORMAL',
                    customer: currentTab.customerId ? {
                      customerId: currentTab.customerId,
                      name: currentTab.customerName,
                      tcNo: null,
                      phone: null
                    } : null,
                    notes: null
                  };

                  // API'ye gönder
                  const response = await fetch('/api/opas/tenant/sales/complete', {
                    method: 'POST',
                    headers: {
                      'Content-Type': 'application/json',
                      'X-TenantId': tenantId,
                      'X-Username': username
                    },
                    body: JSON.stringify(payload)
                  });

                  if (!response.ok) {
                    const error = await response.json();
                    throw new Error(error.error || 'Satış tamamlanamadı');
                  }

                  const result = await response.json();
                  
                  // Başarılı! Modern notification göster
                  setSuccessNotification({
                    open: true,
                    saleNumber: result.saleNumber,
                    totalAmount: result.totalAmount
                  });
                  
                  // Tab'ı temizle (products boş array olacak, backend zaten draft_sales'i siliyor)
                  updateTab(tabId, { 
                    products: [],
                    paymentMethod: undefined,
                    saleType: undefined
                  });
                  
                } catch (error) {
                  console.error('Satış hatası:', error);
                  alert(`❌ Hata: ${error instanceof Error ? error.message : 'Bilinmeyen hata'}`);
                }
              }}
            sx={{
              py: 1.8,
              fontWeight: 800,
              fontSize: '1.1rem',
              textTransform: 'none',
              borderRadius: 2,
              boxShadow: 3,
              // Aktif state
              ...(selectedProducts.length > 0 && currentTab?.paymentMethod && {
                bgcolor: '#2e7d32',
                color: 'white',
                '&:hover': {
                  bgcolor: '#1b5e20',
                  boxShadow: 6,
                  transform: 'scale(1.02)'
                }
              }),
              // Disabled state - daha belirgin
              '&.Mui-disabled': {
                bgcolor: alpha('#9e9e9e', 0.3),
                color: alpha('#000', 0.5),
                fontWeight: 700
              }
            }}
          >
            {!currentTab?.paymentMethod ? '⚠️ ÖNCE ÖDEME YÖNTEMİ SEÇİN' : 
             selectedProducts.length === 0 ? '🛒 SEPETE ÜRÜN EKLEYİN' : 
             `✅ SATIŞI TAMAMLA (${salesSummary.total.toFixed(2)} ₺)`}
          </Button>
        </Box>
      </Card>

      {/* Success Notification - Kırmızı İşaretli Alan (Satış Özeti Üstü) */}
      <Snackbar
        open={successNotification.open}
        autoHideDuration={6000}
        onClose={() => setSuccessNotification({ open: false, saleNumber: '', totalAmount: 0 })}
        anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
        sx={{ 
          position: 'absolute',
          top: 120, // Navbar altı + tab altı
          right: 20, // Sağdan 20px
          zIndex: 1400 // Snackbar'ın üstünde görünsün
        }}
      >
        <Alert
          onClose={() => setSuccessNotification({ open: false, saleNumber: '', totalAmount: 0 })}
          severity="success"
          icon={<SuccessIcon sx={{ fontSize: 28 }} />}
          sx={{
            minWidth: 320, // Kırmızı alana uygun boyut
            maxWidth: 380, // Maksimum genişlik
            borderRadius: 3,
            boxShadow: '0 12px 48px rgba(46, 125, 50, 0.3)',
            background: 'linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%)',
            border: '2px solid #10b981',
            '& .MuiAlert-icon': {
              fontSize: 28, // Biraz küçült
              color: '#059669',
            },
            '& .MuiAlert-message': {
              width: '100%',
            }
          }}
        >
          <Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1.5 }}>
              <ReceiptIcon sx={{ fontSize: 24, color: '#059669' }} />
              <Typography variant="h6" sx={{ fontWeight: 700, color: '#065f46' }}>
                Satış Başarıyla Tamamlandı!
              </Typography>
            </Box>
            
            <Box
              sx={{
                p: 2,
                borderRadius: 2,
                background: 'white',
                border: '1px solid #6ee7b7',
                mb: 1,
              }}
            >
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                <Typography variant="body2" sx={{ color: '#059669', fontWeight: 600 }}>
                  Fiş Numarası:
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 700, color: '#065f46', fontFamily: 'monospace' }}>
                  {successNotification.saleNumber}
                </Typography>
              </Box>
              
              <Divider sx={{ my: 1, borderColor: '#6ee7b7' }} />
              
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body2" sx={{ color: '#059669', fontWeight: 600 }}>
                  Toplam Tutar:
                </Typography>
                <Typography variant="h5" sx={{ fontWeight: 700, color: '#065f46' }}>
                  {successNotification.totalAmount.toFixed(2)} ₺
                </Typography>
              </Box>
            </Box>

            <Typography variant="caption" sx={{ color: '#047857', display: 'block', textAlign: 'center' }}>
              🎉 Fiş kaydedildi ve stok güncellendi
            </Typography>
          </Box>
        </Alert>
      </Snackbar>
    </Box>
  )
}

