/**
 * OPAS Local Service Protocol Handler Utilities
 * Web'den opas:// protokolü ile local service'i kontrol etme
 */

export interface ProtocolResponse {
  success: boolean
  message: string
  error?: string
}

/**
 * Local service'i protocol handler ile başlatmaya çalış
 */
export const startLocalService = async (): Promise<ProtocolResponse> => {
  try {
    // Protocol handler ile başlatmaya çalış (önceden kontrol etme)
    const protocolUrl = 'opas://start-service'
    
    // Modern tarayıcılarda protocol handler çağırma
    if (typeof window !== 'undefined') {
      try {
        // Protocol handler çağır (artık güzel dialog ile onaylanmış)
        window.location.href = protocolUrl
        
        // Kısa bir süre bekle ve kontrol et
        await new Promise(resolve => setTimeout(resolve, 3000))
        
        const statusAfter = await checkServiceStatus()
        if (statusAfter.success) {
          return {
            success: true,
            message: 'Local service başarıyla başlatıldı'
          }
        } else {
          return {
            success: false,
            message: 'Local service başlatılamadı. Protocol handler çalışmadı.',
            error: 'Protocol handler failed'
          }
        }
      } catch (error) {
        return {
          success: false,
          message: 'Protocol handler hatası. Registry kaydı eksik olabilir.',
          error: error instanceof Error ? error.message : 'Protocol error'
        }
      }
    }

    return {
      success: false,
      message: 'Bu özellik sadece tarayıcıda çalışır'
    }

  } catch (error) {
    console.error('Start local service error:', error)
    return {
      success: false,
      message: 'Local service başlatılırken hata oluştu',
      error: error instanceof Error ? error.message : 'Unknown error'
    }
  }
}

/**
 * Local service durumunu kontrol et
 */
export const checkServiceStatus = async (): Promise<ProtocolResponse> => {
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
      return {
        success: true,
        message: 'Local service çalışıyor'
      }
    } else {
      return {
        success: false,
        message: 'Local service yanıt vermiyor'
      }
    }
  } catch {
    // Sessizce handle et - console'u kirletme
    return {
      success: false,
      message: 'Local service bağlantı hatası'
    }
  }
}

/**
 * Local service'i yeniden başlat
 */
export const restartLocalService = async (): Promise<ProtocolResponse> => {
  try {
    const response = await fetch('http://localhost:8080/system/restart', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      }
    })

    if (response.ok) {
      return {
        success: true,
        message: 'Local service yeniden başlatılıyor'
      }
    } else {
      return {
        success: false,
        message: 'Local service yeniden başlatılamadı'
      }
    }
  } catch (error) {
    return {
      success: false,
      message: 'Yeniden başlatma hatası',
      error: error instanceof Error ? error.message : 'Restart failed'
    }
  }
}

/**
 * Local service'i durdur
 */
export const stopLocalService = async (): Promise<ProtocolResponse> => {
  try {
    // Önce servisin çalışıp çalışmadığını kontrol et
    const isRunning = await checkServiceStatus()
    if (!isRunning.success) {
      return {
        success: true,
        message: 'Local service zaten kapalı'
      }
    }

    const response = await fetch('http://localhost:8080/system/stop', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      // Kısa timeout - servis kapanacak
      signal: AbortSignal.timeout(3000)
    })

    if (response.ok) {
      return {
        success: true,
        message: 'Local service durduruluyor'
      }
    } else {
      return {
        success: false,
        message: 'Local service durdurulamadı'
      }
    }
  } catch (error) {
    // Servis kapandığı için connection error normal
    if (error instanceof Error && error.name === 'AbortError') {
      return {
        success: true,
        message: 'Local service başarıyla durduruldu'
      }
    }
    
    return {
      success: false,
      message: 'Local service durdurma hatası'
    }
  }
}

/**
 * Detaylı servis durumu al
 */
export const getDetailedServiceStatus = async () => {
  try {
    const response = await fetch('http://localhost:8080/system/status', {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    })

    if (response.ok) {
      const data = await response.json()
      return {
        success: true,
        data: data.status
      }
    } else {
      return {
        success: false,
        message: 'Detaylı durum alınamadı'
      }
    }
  } catch (error) {
    return {
      success: false,
      message: 'Durum sorgu hatası',
      error: error instanceof Error ? error.message : 'Status query failed'
    }
  }
}

/**
 * Kurulum durumunu kontrol et
 */
export const checkInstallationStatus = async () => {
  try {
    const response = await fetch('http://localhost:8080/system/installation-status', {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    })

    if (response.ok) {
      const data = await response.json()
      return {
        success: true,
        data: {
          isInstalled: data.isInstalled,
          isAutoStart: data.isAutoStart,
          serviceName: data.serviceName
        }
      }
    } else {
      return {
        success: false,
        message: 'Kurulum durumu kontrol edilemedi'
      }
    }
  } catch (error) {
    return {
      success: false,
      message: 'Kurulum durumu sorgu hatası',
      error: error instanceof Error ? error.message : 'Installation check failed'
    }
  }
}
