import { NextRequest, NextResponse } from 'next/server'

export async function GET(request: NextRequest) {
  try {
    // Get tenant ID from headers
    const tenantId = request.headers.get('x-tenant-id')
    
    if (!tenantId) {
      return NextResponse.json(
        { error: 'Tenant ID not found' },
        { status: 400 }
      )
    }

    // Get query parameters
    const { searchParams } = new URL(request.url)
    const page = searchParams.get('page') || '0'
    const limit = searchParams.get('limit') || '50'
    const search = searchParams.get('search') || ''
    const manufacturer = searchParams.get('manufacturer') || ''
    const active = searchParams.get('active')
    const sortBy = searchParams.get('sortBy') || 'drug_name'
    const sortOrder = searchParams.get('sortOrder') || 'asc'

    // Build query string
    const queryParams = new URLSearchParams({
      page,
      limit,
      sortBy,
      sortOrder
    })

    if (search) queryParams.append('search', search)
    if (manufacturer) queryParams.append('manufacturer', manufacturer)
    if (active !== null) queryParams.append('active', active || '')

    // Call backend API
    const backendUrl = `http://127.0.0.1:5080/api/tenant/products-view?${queryParams.toString()}`
    
    const response = await fetch(backendUrl, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'x-tenant-id': tenantId
      }
    })

    if (!response.ok) {
      const errorText = await response.text()
      return NextResponse.json(
        { error: `Backend error: ${response.status} - ${errorText}` },
        { status: response.status }
      )
    }

    const data = await response.json()
    return NextResponse.json(data)

  } catch (error) {
    console.error('Product List API Error:', error)
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    )
  }
}
