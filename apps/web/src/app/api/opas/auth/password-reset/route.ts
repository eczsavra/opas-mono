import { NextResponse } from 'next/server'

export async function POST(request: Request) {
  try {
    const body = await request.json()
    const { username, newPassword } = body

    if (!username || !newPassword) {
      return NextResponse.json(
        { success: false, error: 'Kullanıcı adı ve yeni şifre zorunludur' },
        { status: 400 }
      )
    }

    const backendUrl = `${process.env.NEXT_PUBLIC_BACKEND_URL || process.env.OPAS_BACKEND_URL || 'http://127.0.0.1:5080'}/api/auth/password/reset`
    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ Username: username, NewPassword: newPassword })
    })

    if (!response.ok) {
      const errorData = await response.json()
      return NextResponse.json(
        { success: false, error: errorData.error || 'Şifre sıfırlama başarısız' },
        { status: response.status }
      )
    }

    const result = await response.json()
    return NextResponse.json(result)

  } catch (error) {
    console.error('Error in password reset proxy:', error)
    return NextResponse.json(
      { success: false, error: 'Şifre sıfırlama sırasında bir hata oluştu' },
      { status: 500 }
    )
  }
}
