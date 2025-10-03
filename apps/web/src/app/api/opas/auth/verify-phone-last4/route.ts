import { NextRequest, NextResponse } from 'next/server'

export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url)
    const username = searchParams.get('username')
    const lastFour = searchParams.get('lastFour')

    if (!username || !lastFour) {
      return NextResponse.json(
        { valid: false, error: 'Username and lastFour are required' },
        { status: 400 }
      )
    }

    if (lastFour.length !== 4 || !/^\d{4}$/.test(lastFour)) {
      return NextResponse.json(
        { valid: false, error: 'Last 4 digits must be exactly 4 numbers' },
        { status: 400 }
      )
    }

    // Backend API'ye telefon kontrolü için çağrı yap
    const backendResponse = await fetch(`http://127.0.0.1:5080/api/auth/verify-phone-last4?username=${encodeURIComponent(username)}&lastFour=${encodeURIComponent(lastFour)}`, {
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
      valid: result.valid,
      message: result.message
    })

  } catch (error) {
    console.error('Phone verification error:', error)
    return NextResponse.json(
      { valid: false, error: 'Telefon kontrolü başarısız' },
      { status: 500 }
    )
  }
}
