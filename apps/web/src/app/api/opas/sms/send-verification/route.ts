import { NextRequest, NextResponse } from 'next/server'

export async function POST(request: NextRequest) {
  try {
    const body = await request.json()
    const { phone, recipientName, pharmacyGln } = body

    // Basit validasyon
    if (!phone || !recipientName) {
      return NextResponse.json(
        { ok: false, error: 'Telefon numarası ve alıcı adı gerekli' },
        { status: 400 }
      )
    }

    // Türkiye telefon numarası format kontrolü
    const phoneRegex = /^(\+90|0)?(5[0-9]{2})[- ]?([0-9]{3})[- ]?([0-9]{2})[- ]?([0-9]{2})$/
    const cleanPhone = phone.replace(/[\s\-\+]/g, '').replace(/^90/, '').replace(/^0/, '')
    
    if (!phoneRegex.test(phone) && !/^5[0-9]{9}$/.test(cleanPhone)) {
      return NextResponse.json(
        { ok: false, error: 'Geçersiz telefon numarası formatı (5XX XXX XX XX)' },
        { status: 400 }
      )
    }

    // Test için sabit SMS kodu
    const testSmsCode = '789456'

    // Simülasyon: SMS gönderme işlemi
    console.log('📱 SMS doğrulama kodu gönderiliyor:', {
      to: phone,
      recipientName,
      pharmacyGln,
      code: testSmsCode,
      timestamp: new Date().toISOString()
    })

    // Başarılı response
    return NextResponse.json({
      ok: true,
      message: 'SMS doğrulama kodu başarıyla gönderildi',
      phone,
      expiresIn: '3 dakika',
      expiresInSeconds: 180,
      testNote: 'Test için SMS doğrulama kodu: 789456'
    })

  } catch (error) {
    console.error('SMS send error:', error)
    return NextResponse.json(
      { ok: false, error: 'SMS gönderim hatası' },
      { status: 500 }
    )
  }
}
