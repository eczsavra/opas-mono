import { NextRequest, NextResponse } from 'next/server'

// Real GLN validation from our backend API
const validateGLNFromDB = async (gln: string) => {
  try {
    // OPAS backend API çağrısı - gerçek GLN veritabanından kontrol
    const backendUrl =
      process.env.NEXT_PUBLIC_BACKEND_URL ||
      process.env.OPAS_BACKEND_URL ||
      'http://127.0.0.1:5080'
    console.log(`🔍 GLN Validation attempt for: ${gln} -> ${backendUrl}/auth/register/validate-gln?value=${gln}`)
    const response = await fetch(`${backendUrl}/auth/register/validate-gln?value=${gln}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        // İleride JWT token eklenecek
      },
      // Timeout ekle
      signal: AbortSignal.timeout(5000) // 5 saniye timeout
    })

    if (!response.ok) {
      if (response.status === 404) {
        return { found: false, data: null, error: 'GLN bulunamadı' }
      }
      // Backend ayakta fakat farklı bir hata döndü
      return { found: false, data: null, error: `Backend hata kodu: ${response.status}` }
    }

    const result = await response.json()
    console.log(`✅ Backend response:`, result)
    
    // DEBUG: Return raw response temporarily
    if (result && result.gln) {
      return {
        found: true,
        data: {
          gln: result.gln,
          companyName: result.companyName,
          city: result.city, 
          town: result.town,
          address: null,
          active: true,
          authorized: null,
          email: null,
          phone: null
        }
      }
    }
    
    return { found: false, data: null, error: `Debug: ${JSON.stringify(result)}` }
    
  } catch (error) {
    console.error('GLN Backend API Error:', error)
    
    // Backend erişilemiyorsa fallback - development için demo data
    if (process.env.NODE_ENV === 'development') {
      console.log('🚧 Backend erişilemez, demo data kullanılıyor...')
      
      // Demo GLN'ler (development için)
      const demoGLNs = [
        {
          gln: '8680001530144',
          companyName: 'Demo Eczane A.Ş.',
          city: 'İstanbul',
          town: 'Kadıköy',
          address: 'Moda Cad. No: 123',
          active: true,
          authorized: 'Ahmet Yılmaz',
          email: 'demo@eczane.com',
          phone: '0212-555-0144'
        },
        {
          gln: '8680001530151', 
          companyName: 'Sağlık Eczanesi',
          city: 'Ankara',
          town: 'Çankaya', 
          address: 'Tunalı Hilmi Cad. No: 45',
          active: true,
          authorized: 'Fatma Demir',
          email: 'saglik@eczane.com',
          phone: '0312-555-0151'
        }
      ]
      
      const demoPharmacy = demoGLNs.find(p => p.gln === gln)
      if (demoPharmacy) {
        return { found: true, data: demoPharmacy }
      }
    }
    
    // Backend erişilemiyorsa fallback
    if (error instanceof Error && error.name === 'AbortError') {
      return { found: false, data: null, error: 'Doğrulama zaman aşımına uğradı' }
    }
    
    return { found: false, data: null, error: `Backend servisine ulaşılamadı (${(process.env.NEXT_PUBLIC_BACKEND_URL||process.env.OPAS_BACKEND_URL||'127.0.0.1:5080')})` }
  }
}

export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url)
    const gln = searchParams.get('gln')

    if (!gln) {
      return NextResponse.json({
        ok: false,
        error: 'GLN parametresi gereklidir'
      }, { status: 400 })
    }

    // GLN format validation
    if (gln.length !== 13) {
      return NextResponse.json({
        ok: false,
        error: 'GLN 13 haneli olmalıdır'
      }, { status: 400 })
    }

    if (!gln.startsWith('868')) {
      return NextResponse.json({
        ok: false,
        error: 'GLN 868 ile başlamalıdır'
      }, { status: 400 })
    }

    // Validate from database
    const result = await validateGLNFromDB(gln)

    if (result.found && result.data) {
      return NextResponse.json({
        ok: true,
        found: true,
        data: result.data,
        message: 'GLN doğrulandı - Gerçek verilerden (OPAS Backend)'
      })
    }

    // Not found is NOT an error for registration flow; respond 200 with found:false
    return NextResponse.json({
      ok: true,
      found: false,
      data: null,
      message: result.error || 'GLN bulunamadı'
    })

  } catch (error) {
    console.error('GLN validation error:', error)
    return NextResponse.json({
      ok: false,
      error: 'GLN doğrulama sırasında bir hata oluştu'
    }, { status: 500 })
  }
}

// POST method for future use (batch validation, etc.)
export async function POST() {
  return NextResponse.json({
    ok: false,
    error: 'POST method desteklenmiyor. GET method kullanın.'
  }, { status: 405 })
}
