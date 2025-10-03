'use client'

import { useState, useEffect, useCallback } from 'react'

interface LocalServiceStatus {
  isActive: boolean
  lastCheck: Date
  version?: string
  services?: {
    pos: boolean
    printer: boolean
    medula: boolean
  }
}

export const useLocalService = () => {
  const [status, setStatus] = useState<LocalServiceStatus>({
    isActive: false,
    lastCheck: new Date()
  })
  const [isLoading, setIsLoading] = useState(true)

  const checkLocalService = useCallback(async () => {
    try {
      const response = await fetch('http://localhost:8080/health', {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json'
        },
        // Silent mode - console'da network error gösterme
        signal: AbortSignal.timeout(2000) // 2 saniye timeout
      })

      if (response.ok) {
        await response.text() // Read response but don't store
        setStatus({
          isActive: true,
          lastCheck: new Date(),
          version: '1.0.0' // TODO: Get from service
        })
      } else {
        setStatus(prev => ({
          ...prev,
          isActive: false,
          lastCheck: new Date()
        }))
      }
    } catch {
      // LocalService kapalı - bu normal bir durum, hata değil
      setStatus(prev => ({
        ...prev,
        isActive: false,
        lastCheck: new Date()
      }))
    } finally {
      setIsLoading(false)
    }
  }, [])

  // İlk kontrol
  useEffect(() => {
    checkLocalService()
  }, [checkLocalService])

  // Periyodik kontrol (5 saniye) - daha responsive UX
  useEffect(() => {
    const interval = setInterval(checkLocalService, 5000)
    return () => clearInterval(interval)
  }, [checkLocalService])

  return {
    status,
    isLoading,
    checkLocalService,
    isLocalActive: status.isActive
  }
}
