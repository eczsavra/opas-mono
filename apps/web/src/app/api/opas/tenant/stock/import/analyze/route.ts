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

    // Get form data
    const formData = await request.formData();

    // Forward to backend
    const backendUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5080';
    const response = await fetch(`${backendUrl}/api/opas/tenant/stock/import/analyze`, {
      method: 'POST',
      headers: {
        'X-TenantId': tenantId,
        'X-Username': username,
        'Cookie': `tenantId=${tenantId}; username=${username}`
      },
      body: formData
    });

    const data = await response.json();

    if (!response.ok) {
      return NextResponse.json(data, { status: response.status });
    }

    return NextResponse.json(data);
  } catch (error) {
    console.error('Import analyze error:', error);
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    );
  }
}

