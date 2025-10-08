import { NextRequest, NextResponse } from 'next/server'

export async function GET(request: NextRequest) {
  try {
    const searchParams = request.nextUrl.searchParams
    const search = searchParams.get('query') || searchParams.get('search') || ''
    const page = searchParams.get('page') || '1'
    const pageSize = searchParams.get('pageSize') || '20'
    
    // Try cookies first, then headers
    const tenantId = request.cookies.get('x-tenant-id')?.value || request.headers.get('x-tenant-id')
    const username = request.cookies.get('x-username')?.value || request.headers.get('x-username')
    
    if (!tenantId || !username) {
      return NextResponse.json(
        { error: 'Authentication required' },
        { status: 401 }
      )
    }

    const backendUrl = process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:5080'
    const url = `${backendUrl}/api/tenant/products?search=${encodeURIComponent(search)}&page=${page}&pageSize=${pageSize}&isActive=true`

    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'x-tenant-id': tenantId,
      },
      cache: 'no-store',
    })

    if (!response.ok) {
      return NextResponse.json(
        { error: 'Failed to fetch products' },
        { status: response.status }
      )
    }

    const data = await response.json()
    return NextResponse.json(data)
  } catch (error) {
    console.error('Product search error:', error)
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    )
  }
}

