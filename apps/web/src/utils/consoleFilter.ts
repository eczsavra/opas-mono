/**
 * Console error filtreleme - LocalService hatalarını sustur
 */

// Orijinal console.error'u sakla
const originalConsoleError = console.error

// LocalService ile ilgili hataları filtrele
console.error = (...args: unknown[]) => {
  const message = args.join(' ')
  
  // SADECE beklenen LocalService bağlantı hatalarını sustur
  const isExpectedLocalServiceError = (
    // Health check hataları (normal - servis kapalı olabilir)
    (message.includes('GET http://localhost:8080/health') && message.includes('ERR_CONNECTION_REFUSED')) ||
    
    // Servis durumu kontrol hataları (normal - servis kapalı olabilir)  
    (message.includes('checkServiceStatus') && message.includes('ERR_CONNECTION_REFUSED')) ||
    
    // Start service sırasında health check (normal - servis henüz başlamamış)
    (message.includes('startLocalService') && message.includes('ERR_CONNECTION_REFUSED'))
  )
  
  if (isExpectedLocalServiceError) {
    // Bu hatalar normal - LocalService kapalı/başlamamış olabilir
    return
  }
  
  // DİĞER TÜM HATALARI GÖSTER (LocalService ile ilgili gerçek hatalar dahil!)
  originalConsoleError.apply(console, args)
}

// Fetch wrapper - sadece beklenen hataları sustur
const originalFetch = window.fetch
window.fetch = async (...args) => {
  try {
    return await originalFetch(...args)
  } catch (error) {
    const url = args[0]?.toString() || ''
    
    // SADECE health check hatalarını sustur (diğer LocalService hatalarını göster!)
    const isExpectedHealthCheckError = (
      url.includes('localhost:8080/health') && 
      error instanceof TypeError && 
      error.message.includes('Failed to fetch')
    )
    
    if (isExpectedHealthCheckError) {
      // Bu normal - health check başarısız (servis kapalı)
      throw error // Hata fırlat ama console'a yazma
    }
    
    // Diğer tüm hatalar normal şekilde console'a yazılsın
    throw error
  }
}

// Cleanup fonksiyonu
export const restoreConsoleError = () => {
  console.error = originalConsoleError
}

// Named export olarak değiştir
const consoleFilter = {
  restoreConsoleError
}

export default consoleFilter
