import { NextResponse } from 'next/server'

export async function POST(request: Request) {
  try {
    const body = await request.json()

    // Validate required fields
    if (!body.username || !body.password) {
      return NextResponse.json(
        { success: false, error: 'Kullanıcı adı ve şifre gereklidir' },
        { status: 400 }
      )
    }

    // Call backend API
    const backendUrl = `${process.env.NEXT_PUBLIC_BACKEND_URL || process.env.OPAS_BACKEND_URL || 'http://127.0.0.1:5080'}/api/auth/login`
    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        username: body.username,
        password: body.password
      })
    })

    if (!response.ok) {
      const errorData = await response.json()
      return NextResponse.json(
        { success: false, error: errorData.error || 'Giriş başarısız' },
        { status: response.status }
      )
    }

    const result = await response.json()
    return NextResponse.json(result)

  } catch (error) {
    console.error('Error in login:', error)
    return NextResponse.json(
      { success: false, error: 'Giriş sırasında bir hata oluştu' },
      { status: 500 }
    )
  }
}
