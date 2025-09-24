import { NextRequest, NextResponse } from 'next/server'

// Test için sabit doğrulama kodu
const TEST_VERIFICATION_CODE = '123456'

// Simulated verification code storage (production'da Redis/Database olacak)
interface CodeData {
  code: string
  email: string
  createdAt: Date
  expiresAt: Date
}

// In-memory storage for demo (production'da external store kullanın)
// eslint-disable-next-line @typescript-eslint/no-unused-vars
const verificationCodes = new Map<string, CodeData>()

export async function POST(request: NextRequest) {
  try {
    const body = await request.json()
    const { email, code } = body

    // Basit validasyon
    if (!email || !code) {
      return NextResponse.json(
        { ok: false, error: 'Email ve doğrulama kodu gerekli' },
        { status: 400 }
      )
    }

    // Kod format kontrolü
    if (!/^\d{6}$/.test(code)) {
      return NextResponse.json(
        { ok: false, error: 'Doğrulama kodu 6 haneli olmalıdır' },
        { status: 400 }
      )
    }

    // Test kodu kontrolü (basit)
    if (code === TEST_VERIFICATION_CODE) {
      console.log('✅ Test doğrulama kodu başarılı:', {
        email,
        code,
        timestamp: new Date().toISOString()
      })

      return NextResponse.json({
        ok: true,
        message: 'Email başarıyla doğrulandı',
        email,
        verifiedAt: new Date().toISOString()
      })
    }

    // Gerçek production'da burada:
    // 1. Email için stored code'u al
    // 2. Expiry kontrolü yap
    // 3. Kod eşleşmesini kontrol et
    // 4. Rate limiting
    // 5. Attempt counter

    // Test için başarısız durumlar
    if (code === '000000') {
      return NextResponse.json(
        { ok: false, error: 'Doğrulama kodunun süresi dolmuş' },
        { status: 400 }
      )
    }

    if (code === '111111') {
      return NextResponse.json(
        { ok: false, error: 'Çok fazla yanlış deneme' },
        { status: 429 }
      )
    }

    // Genel yanlış kod durumu
    console.log('❌ Yanlış doğrulama kodu:', {
      email,
      attemptedCode: code,
      correctCode: TEST_VERIFICATION_CODE,
      timestamp: new Date().toISOString()
    })

    return NextResponse.json(
      { ok: false, error: 'Doğrulama kodu yanlış' },
      { status: 400 }
    )

  } catch (error) {
    console.error('Email verify error:', error)
    return NextResponse.json(
      { ok: false, error: 'Doğrulama hatası' },
      { status: 500 }
    )
  }
}