'use client'

import React, { useState, useCallback, useEffect } from 'react'
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
  AccordionDetails
} from '@mui/material'
import { 
  Search as SearchIcon, 
  ContentPaste as PasteIcon,
  Clear as ClearIcon,
  ShoppingCart as CartIcon,
  AttachMoney as MoneyIcon,
  LocalOffer as OfferIcon,
  Receipt as ReceiptIcon,
  Delete as DeleteIcon
} from '@mui/icons-material'
import { styled } from '@mui/material/styles'
import { useSalesContext } from '@/contexts/SalesContext'

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
}

interface ModernSearchBoxProps {
  tabId: string // Hangi tab için çalışıyor
  onProductSelect?: (product: Product) => void
}

export default function ModernSearchBox({ tabId, onProductSelect }: ModernSearchBoxProps) {
  const { saleTabs, updateTab } = useSalesContext()
  const currentTab = saleTabs.find(tab => tab.id === tabId)
  const selectedProducts = currentTab?.products || []

  const [inputValue, setInputValue] = useState('')
  const [options, setOptions] = useState<Product[]>([])
  const [loading, setLoading] = useState(false)
  const [highlightedIndex, setHighlightedIndex] = useState<number>(0)
  const [open, setOpen] = useState(false)
  const listboxRef = React.useRef<HTMLUListElement>(null)

  const searchProducts = useCallback(async (query: string) => {
    if (query.length < 2) {
      setOptions([])
      setHighlightedIndex(0)
      setOpen(false)
      return
    }

    setLoading(true)
    
    try {
      const response = await fetch(`/api/opas/tenant/products/search?search=${encodeURIComponent(query)}&pageSize=10`)
      
      if (response.ok) {
        const data = await response.json()
        const products = data.data || []
        setOptions(products)
        setHighlightedIndex(0)
        setOpen(products.length > 0)
      } else {
        setOptions([])
        setHighlightedIndex(0)
        setOpen(false)
      }
    } catch {
      setOptions([])
      setHighlightedIndex(0)
      setOpen(false)
    } finally {
      setLoading(false)
    }
  }, [])

  const handleInputChange = (_event: React.SyntheticEvent, newInputValue: string) => {
    setInputValue(newInputValue)
    searchProducts(newInputValue)
  }

  const addProduct = (product: Product) => {
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
        height: '100%',
        overflow: 'hidden'
      }}
    >
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

                  {/* Sağ: Adet, Stok ve Sil Butonu */}
                         <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'center', flexShrink: 0 }}>
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
                    <Chip 
                      label="Stok: 150" 
                      size="small"
                      sx={{ 
                        fontWeight: 600,
                        backgroundColor: alpha('#ff9800', 0.12),
                        color: '#e65100',
                        border: `1px solid ${alpha('#ff9800', 0.3)}`
                      }}
                    />
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

      {/* Sağ Taraf - Satış Özeti */}
      <Card 
        elevation={0}
        sx={{ 
          width: '100%',
          minWidth: 0,
          borderRadius: 4,
          background: `linear-gradient(135deg, ${alpha('#1976d2', 0.05)} 0%, ${alpha('#42a5f5', 0.05)} 100%)`,
          border: `2px solid ${alpha('#1976d2', 0.12)}`,
          backdropFilter: 'blur(10px)',
          height: 'fit-content',
          maxHeight: '100%', // Parent yüksekliğini aşmasın
          overflowY: 'auto', // Gerekirse scroll
          transition: 'all 0.3s ease',
          '&:hover': {
            boxShadow: `0 12px 40px ${alpha('#1976d2', 0.15)}`,
            transform: 'translateY(-2px)',
          }
        }}
      >
        <Box sx={{ p: 3 }}>
          {/* Header */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 3 }}>
            <Box 
              sx={{ 
                width: 48,
                height: 48,
                borderRadius: 3,
                background: `linear-gradient(135deg, #1976d2 0%, #42a5f5 100%)`,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                boxShadow: `0 4px 16px ${alpha('#1976d2', 0.3)}`
              }}
            >
              <ReceiptIcon sx={{ color: 'white', fontSize: 28 }} />
            </Box>
            <Box>
              <Typography variant="h6" sx={{ fontWeight: 700, color: 'text.primary', letterSpacing: -0.5 }}>
                Satış Özeti
              </Typography>
              <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                Anlık durum
              </Typography>
            </Box>
          </Box>

          {/* Stats */}
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {/* Toplam Ürün */}
            <Box 
              sx={{ 
                display: 'flex', 
                alignItems: 'center', 
                justifyContent: 'space-between',
                p: 2,
                borderRadius: 2,
                backgroundColor: alpha('#fff', 0.6),
                transition: 'all 0.2s ease',
                '&:hover': {
                  backgroundColor: alpha('#fff', 0.9),
                  transform: 'translateX(4px)',
                }
              }}
            >
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                <Box sx={{ 
                  width: 36,
                  height: 36,
                  borderRadius: 2,
                  backgroundColor: alpha('#1976d2', 0.1),
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center'
                }}>
                  <CartIcon sx={{ fontSize: 20, color: 'primary.main' }} />
                </Box>
                <Typography variant="body2" sx={{ fontWeight: 600, color: 'text.secondary' }}>
                  Toplam Ürün
                </Typography>
              </Box>
              <Chip 
                label={salesSummary.totalItems} 
                size="small"
                sx={{ 
                  fontWeight: 700,
                  fontSize: '0.9rem',
                  backgroundColor: alpha('#1976d2', 0.15),
                  color: 'primary.main'
                }}
              />
            </Box>

            <Divider sx={{ my: 1 }} />

            {/* Ara Toplam */}
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', px: 1 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <MoneyIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
                <Typography variant="body2" sx={{ color: 'text.secondary', fontWeight: 500 }}>
                  Ara Toplam
                </Typography>
              </Box>
              <Typography variant="body1" sx={{ fontWeight: 700 }}>
                {salesSummary.subtotal.toFixed(2)} ₺
              </Typography>
            </Box>

            {/* İndirim */}
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', px: 1 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <OfferIcon sx={{ fontSize: 18, color: 'success.main' }} />
                <Typography variant="body2" sx={{ color: 'text.secondary', fontWeight: 500 }}>
                  İndirim
                </Typography>
              </Box>
              <Typography variant="body1" sx={{ fontWeight: 700, color: 'success.main' }}>
                -{salesSummary.discount.toFixed(2)} ₺
              </Typography>
            </Box>

            <Divider sx={{ my: 1 }} />

            {/* Toplam */}
            <Box 
              sx={{ 
                p: 2.5,
                borderRadius: 3,
                background: `linear-gradient(135deg, #1976d2 0%, #42a5f5 100%)`,
                boxShadow: `0 6px 24px ${alpha('#1976d2', 0.3)}`,
              }}
            >
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body1" sx={{ color: 'white', fontWeight: 700, fontSize: '1.1rem', letterSpacing: 0.5 }}>
                  TOPLAM
                </Typography>
                <Typography variant="h5" sx={{ color: 'white', fontWeight: 800, letterSpacing: -0.5 }}>
                  {salesSummary.total.toFixed(2)} ₺
                </Typography>
              </Box>
            </Box>
          </Box>
        </Box>
      </Card>
    </Box>
  )
}

