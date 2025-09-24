import { NextRequest, NextResponse } from 'next/server'

export async function POST(request: NextRequest) {
  try {
    const body = await request.json()
    const { email, recipientName, pharmacyGln } = body

    // Basit validasyon
    if (!email || !recipientName) {
      return NextResponse.json(
        { ok: false, error: 'Email ve alıcı adı gerekli' },
        { status: 400 }
      )
    }

    // Email format kontrolü
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(email)) {
      return NextResponse.json(
        { ok: false, error: 'Geçersiz email formatı' },
        { status: 400 }
      )
    }

    // Simülasyon: Email gönderme işlemi
    console.log('📧 Doğrulama kodu gönderiliyor:', {
      to: email,
      recipientName,
      pharmacyGln,
      code: '123456', // Test için sabit kod
      timestamp: new Date().toISOString()
    })

    // Başarılı response
    return NextResponse.json({
      ok: true,
      message: 'Doğrulama kodu başarıyla gönderildi',
      email,
      expiresIn: '3 dakika',
      expiresInSeconds: 180,
      testNote: 'Test için doğrulama kodu: 123456'
    })

  } catch (error) {
    console.error('Email send error:', error)
    return NextResponse.json(
      { ok: false, error: 'Email gönderim hatası' },
      { status: 500 }
    )
  }
}