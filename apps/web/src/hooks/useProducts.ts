// import { useState, useCallback } from 'react' // TODO: Kullanılacak
import { useCallback } from 'react'

export const useProducts = () => {
  // TODO: Ürün hook state'leri buraya eklenecek

  const searchProducts = useCallback(async () => {
    // TODO: Ürün arama fonksiyonu
  }, [])

  const getProductById = useCallback(async () => {
    // TODO: ID ile ürün getirme fonksiyonu
  }, [])

  const getProductStock = useCallback(async () => {
    // TODO: Ürün stok kontrolü fonksiyonu
  }, [])

  return {
    // TODO: Hook return değerleri buraya eklenecek
    searchProducts,
    getProductById,
    getProductStock
  }
}
