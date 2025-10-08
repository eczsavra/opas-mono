import { NextRequest, NextResponse } from 'next/server';

export async function POST(request: NextRequest) {
  try {
    const tenantId = request.headers.get('X-TenantId') || request.cookies.get('tenantId')?.value;
    const username = request.headers.get('X-Username') || request.cookies.get('username')?.value;

    if (!tenantId || !username) {
      return NextResponse.json(
        { error: 'Authentication required' },
        { status: 401 }
      );
    }

    // Get request body
    const body = await request.json();

    // Forward to backend
    const backendUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5080';
    const response = await fetch(`${backendUrl}/api/opas/tenant/stock/import/execute`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-TenantId': tenantId,
        'X-Username': username,
        'Cookie': `tenantId=${tenantId}; username=${username}`
      },
      body: JSON.stringify(body)
    });

    const data = await response.json();

    if (!response.ok) {
      return NextResponse.json(data, { status: response.status });
    }

    return NextResponse.json(data);
  } catch (error) {
    console.error('Import execute error:', error);
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    );
  }
}

