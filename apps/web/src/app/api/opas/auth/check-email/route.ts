import { NextRequest, NextResponse } from 'next/server'

export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url)
    const email = searchParams.get('email')

    if (!email || email.length < 5) {
      return NextResponse.json(
        { found: false, error: 'Email adresi en az 5 karakter olmalıdır' },
        { status: 400 }
      )
    }

    // Email format kontrolü
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(email)) {
      return NextResponse.json(
        { found: false, error: 'Geçerli bir email adresi giriniz' },
        { status: 400 }
      )
    }

    // Backend API'ye email kontrolü için çağrı yap
    const backendResponse = await fetch(`http://127.0.0.1:5080/api/auth/check-email?email=${encodeURIComponent(email)}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    })

    if (!backendResponse.ok) {
      throw new Error(`Backend API error: ${backendResponse.status}`)
    }

    const result = await backendResponse.json()

    return NextResponse.json({
      found: result.found,
      email: result.email,
      username: result.username,
      firstName: result.firstName,
      lastName: result.lastName,
      gln: result.gln,
      message: result.message
    })

  } catch (error) {
    console.error('Email check error:', error)
    return NextResponse.json(
      { found: false, error: 'Email kontrolü başarısız' },
      { status: 500 }
    )
  }
}
