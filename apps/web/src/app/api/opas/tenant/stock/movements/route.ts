import { NextRequest, NextResponse } from 'next/server'

export async function GET(request: NextRequest) {
  try {
    const searchParams = request.nextUrl.searchParams
    const page = searchParams.get('page') || '1'
    const pageSize = searchParams.get('pageSize') || '20'
    
    const tenantId = request.cookies.get('x-tenant-id')?.value
    const username = request.cookies.get('username')?.value
    
    if (!tenantId || !username) {
      return NextResponse.json(
        { error: 'Authentication required' },
        { status: 401 }
      )
    }

    const backendUrl = process.env.NEXT_PUBLIC_BACKEND_URL || 'http://127.0.0.1:5080'
    const url = `${backendUrl}/api/tenant/stock/movements?tenantId=${tenantId}&page=${page}&pageSize=${pageSize}`

    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
      cache: 'no-store',
    })

    if (!response.ok) {
      return NextResponse.json(
        { error: 'Failed to fetch stock movements' },
        { status: response.status }
      )
    }

    const data = await response.json()
    return NextResponse.json(data)
  } catch (error) {
    console.error('Stock movements fetch error:', error)
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    )
  }
}

export async function POST(request: NextRequest) {
  try {
    // Try cookies first, then headers (for localStorage fallback)
    const tenantId = request.cookies.get('x-tenant-id')?.value || 
                     request.headers.get('x-tenant-id')
    const username = request.cookies.get('username')?.value || 
                     request.headers.get('x-username')
    
    if (!tenantId || !username) {
      return NextResponse.json(
        { error: 'Authentication required', details: 'tenantId or username missing' },
        { status: 401 }
      )
    }

    const body = await request.json()

    const backendUrl = process.env.NEXT_PUBLIC_BACKEND_URL || 'http://127.0.0.1:5080'
    const url = `${backendUrl}/api/tenant/stock/movements?tenantId=${tenantId}&username=${username}`

    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
      cache: 'no-store',
    })

    if (!response.ok) {
      const errorData = await response.json()
      return NextResponse.json(
        errorData,
        { status: response.status }
      )
    }

    const data = await response.json()
    return NextResponse.json(data)
  } catch (error) {
    console.error('Stock movement create error:', error)
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    )
  }
}

