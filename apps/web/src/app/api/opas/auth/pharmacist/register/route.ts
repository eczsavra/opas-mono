import { NextResponse } from 'next/server'

export async function POST(request: Request) {
  try {
    const body = await request.json()

    // Validate required fields
    if (!body.username || !body.password || !body.email || !body.personalGln) {
      return NextResponse.json(
        { success: false, error: 'Username, password, email, and personalGln are required' },
        { status: 400 }
      )
    }

    // Call backend API
    const backendUrl = `${process.env.NEXT_PUBLIC_BACKEND_URL}/api/auth/pharmacist/register`
    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        username: body.username,
        password: body.password,
        email: body.email,
        phone: body.phone,
        personalGln: body.personalGln,
        firstName: body.firstName,
        lastName: body.lastName,
        tcNumber: body.tcNumber,
        birthYear: body.birthYear,
        pharmacyRegistrationNo: body.pharmacyRegistrationNo,
        isEmailVerified: body.isEmailVerified || false,
        isPhoneVerified: body.isPhoneVerified || false,
        isNviVerified: body.isNviVerified || false
      })
    })

    if (!response.ok) {
      const errorData = await response.json()
      return NextResponse.json(
        { success: false, error: errorData.error || 'Registration failed' },
        { status: response.status }
      )
    }

    const result = await response.json()
    return NextResponse.json(result)

  } catch (error) {
    console.error('Error in pharmacist registration:', error)
    return NextResponse.json(
      { success: false, error: 'Internal server error during registration' },
      { status: 500 }
    )
  }
}
