import { NextRequest, NextResponse } from 'next/server'

export async function GET(request: NextRequest) {
  try {
    const searchParams = request.nextUrl.searchParams
    const page = searchParams.get('page') || '1'
    const pageSize = searchParams.get('pageSize') || '50'
    
    // Try cookies first, then headers
    const tenantId = request.cookies.get('x-tenant-id')?.value || request.headers.get('x-tenant-id')
    const username = request.cookies.get('x-username')?.value || request.headers.get('x-username')
    
    if (!tenantId || !username) {
      return NextResponse.json(
        { error: 'Authentication required' },
        { status: 401 }
      )
    }

    const backendUrl = process.env.NEXT_PUBLIC_BACKEND_URL || 'http://127.0.0.1:5080'
    const url = `${backendUrl}/api/tenant/stock/summary?tenantId=${tenantId}&page=${page}&pageSize=${pageSize}`

    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
      cache: 'no-store',
    })

    if (!response.ok) {
      return NextResponse.json(
        { error: 'Failed to fetch stock summary' },
        { status: response.status }
      )
    }

    const data = await response.json()
    return NextResponse.json(data)
  } catch (error) {
    console.error('Stock summary fetch error:', error)
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    )
  }
}

