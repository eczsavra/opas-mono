import { NextRequest, NextResponse } from 'next/server'

export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url)
    const username = searchParams.get('username')

    if (!username || username.length < 3) {
      return NextResponse.json(
        { ok: false, error: 'Kullanıcı adı en az 3 karakter olmalıdır' },
        { status: 400 }
      )
    }

    // Username format kontrolü
    const usernameRegex = /^[a-zA-Z0-9_.-]{3,50}$/
    if (!usernameRegex.test(username)) {
      return NextResponse.json(
        { ok: false, error: 'Kullanıcı adı sadece harf, rakam, nokta, tire ve alt çizgi içerebilir' },
        { status: 400 }
      )
    }

    // Backend API'ye username kontrolü için çağrı yap
    const backendResponse = await fetch(`http://127.0.0.1:5080/api/auth/check-username?username=${encodeURIComponent(username)}`, {
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
      ok: true,
      available: result.available,
      username: username.toLowerCase(), // Username'i lowercase'e çevir
      message: result.available ? 
        `✅ "${username}" kullanıcı adı kullanılabilir` : 
        `❌ "${username}" kullanıcı adı zaten alınmış`
    })

  } catch (error) {
    console.error('Username check error:', error)
    return NextResponse.json(
      { ok: false, error: 'Kullanıcı adı kontrolü başarısız' },
      { status: 500 }
    )
  }
}
