import { NextRequest, NextResponse } from 'next/server'

// Test için sabit SMS doğrulama kodu
const TEST_SMS_CODE = '789456'

// Simulated verification code storage (production'da Redis/Database olacak)
interface SmsCodeData {
  code: string
  phone: string
  createdAt: Date
  expiresAt: Date
}

// In-memory storage for demo (production'da external store kullanın)
// eslint-disable-next-line @typescript-eslint/no-unused-vars
const smsVerificationCodes = new Map<string, SmsCodeData>()

export async function POST(request: NextRequest) {
  try {
    const body = await request.json()
    const { phone, code } = body

    // Basit validasyon
    if (!phone || !code) {
      return NextResponse.json(
        { ok: false, error: 'Telefon numarası ve doğrulama kodu gerekli' },
        { status: 400 }
      )
    }

    // Kod format kontrolü
    if (!/^\d{6}$/.test(code)) {
      return NextResponse.json(
        { ok: false, error: 'SMS doğrulama kodu 6 haneli olmalıdır' },
        { status: 400 }
      )
    }

    // Test kodu kontrolü (basit)
    if (code === TEST_SMS_CODE) {
      console.log('✅ Test SMS doğrulama kodu başarılı:', {
        phone,
        code,
        timestamp: new Date().toISOString()
      })

      return NextResponse.json({
        ok: true,
        message: 'Telefon numarası başarıyla doğrulandı',
        phone,
        verifiedAt: new Date().toISOString()
      })
    }

    // Test için başarısız durumlar
    if (code === '000000') {
      return NextResponse.json(
        { ok: false, error: 'SMS doğrulama kodunun süresi dolmuş' },
        { status: 400 }
      )
    }

    if (code === '111111') {
      return NextResponse.json(
        { ok: false, error: 'Çok fazla yanlış SMS deneme' },
        { status: 429 }
      )
    }

    if (code === '999999') {
      return NextResponse.json(
        { ok: false, error: 'SMS servisi geçici olarak kullanılamıyor' },
        { status: 503 }
      )
    }

    // Genel yanlış kod durumu
    console.log('❌ Yanlış SMS doğrulama kodu:', {
      phone,
      attemptedCode: code,
      correctCode: TEST_SMS_CODE,
      timestamp: new Date().toISOString()
    })

    return NextResponse.json(
      { ok: false, error: 'SMS doğrulama kodu yanlış' },
      { status: 400 }
    )

  } catch (error) {
    console.error('SMS verify error:', error)
    return NextResponse.json(
      { ok: false, error: 'SMS doğrulama hatası' },
      { status: 500 }
    )
  }
}
