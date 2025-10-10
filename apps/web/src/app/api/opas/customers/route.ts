import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:5080';

export async function GET(request: NextRequest) {
  try {
    const tenantId = request.headers.get('X-TenantId');
    const username = request.headers.get('X-Username');
    
    if (!tenantId || !username) {
      return NextResponse.json(
        { error: 'Missing tenant credentials' },
        { status: 401 }
      );
    }

    // Get query parameters
    const searchParams = request.nextUrl.searchParams;
    const queryString = searchParams.toString();
    
    const response = await fetch(
      `${BACKEND_URL}/api/tenant/customers${queryString ? `?${queryString}` : ''}`,
      {
        method: 'GET',
        headers: {
          'X-TenantId': tenantId,
          'X-Username': username,
          'Content-Type': 'application/json',
        },
      }
    );

    if (!response.ok) {
      const errorText = await response.text();
      console.error('Backend error:', errorText);
      return NextResponse.json(
        { error: 'Failed to fetch customers' },
        { status: response.status }
      );
    }

    const data = await response.json();
    return NextResponse.json(data);
  } catch (error) {
    console.error('API error:', error);
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    );
  }
}

export async function POST(request: NextRequest) {
  try {
    const tenantId = request.headers.get('X-TenantId');
    const username = request.headers.get('X-Username');
    
    if (!tenantId || !username) {
      return NextResponse.json(
        { error: 'Missing tenant credentials' },
        { status: 401 }
      );
    }

    const body = await request.json();
    
    const response = await fetch(
      `${BACKEND_URL}/api/tenant/customers`,
      {
        method: 'POST',
        headers: {
          'X-TenantId': tenantId,
          'X-Username': username,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(body),
      }
    );

    if (!response.ok) {
      const errorText = await response.text();
      console.error('Backend error:', errorText);
      return NextResponse.json(
        { error: 'Failed to create customer' },
        { status: response.status }
      );
    }

    const data = await response.json();
    return NextResponse.json(data);
  } catch (error) {
    console.error('API error:', error);
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    );
  }
}

