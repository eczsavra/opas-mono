import { NextRequest, NextResponse } from 'next/server'

export async function POST(request: NextRequest) {
  try {
    const body = await request.json()
    const { tcNumber, firstName, lastName, birthYear } = body

    // Input validation
    if (!tcNumber || !firstName || !lastName || !birthYear) {
      return NextResponse.json({
        ok: false,
        error: 'Tüm alanlar zorunludur'
      }, { status: 400 })
    }

    if (tcNumber.length !== 11) {
      return NextResponse.json({
        ok: false,
        error: 'TC kimlik numarası 11 haneli olmalıdır'
      }, { status: 400 })
    }

    // Backend API call
    const backendUrl = process.env.OPAS_BACKEND_URL || 'http://localhost:5080'
    const response = await fetch(`${backendUrl}/auth/nvi/validate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        tcNumber,
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        birthYear: parseInt(birthYear)
      }),
      signal: AbortSignal.timeout(15000) // 15 saniye timeout (NVI servisi yavaş olabilir)
    })

    if (!response.ok) {
      const errorResult = await response.json()
      return NextResponse.json({
        ok: false,
        error: errorResult.error || 'NVI doğrulama başarısız'
      }, { status: response.status })
    }

    const result = await response.json()
    
    return NextResponse.json({
      ok: true,
      message: 'Kimlik bilgileri NVI tarafından doğrulandı',
      data: result.data
    })

  } catch (error) {
    console.error('NVI validation API error:', error)
    
    if (error instanceof Error && error.name === 'AbortError') {
      return NextResponse.json({
        ok: false,
        error: 'NVI doğrulama zaman aşımına uğradı. Lütfen tekrar deneyin.'
      }, { status: 408 })
    }
    
    return NextResponse.json({
      ok: false,
      error: 'NVI doğrulama sırasında bir hata oluştu'
    }, { status: 500 })
  }
}
