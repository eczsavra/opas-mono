import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:5080';

export async function POST(request: NextRequest) {
  try {
    const tenantId = request.headers.get('X-TenantId');
    const username = request.headers.get('X-Username');
    
    if (!tenantId || !username) {
      return NextResponse.json(
        { error: 'Missing credentials' },
        { status: 401 }
      );
    }

    const body = await request.json();

    const response = await fetch(`${BACKEND_URL}/api/opas/tenant/sales/complete`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-TenantId': tenantId,
        'X-Username': username
      },
      body: JSON.stringify(body)
    });

    const data = await response.json();

    if (!response.ok) {
      return NextResponse.json(
        { error: data.detail || data.error || 'Sale completion failed' },
        { status: response.status }
      );
    }

    return NextResponse.json(data);
  } catch (error) {
    console.error('Complete sale error:', error);
    return NextResponse.json(
      { error: error instanceof Error ? error.message : 'Internal server error' },
      { status: 500 }
    );
  }
}

