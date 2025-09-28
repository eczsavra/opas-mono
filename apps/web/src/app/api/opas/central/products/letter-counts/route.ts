import { NextRequest, NextResponse } from 'next/server';

const backendUrl = process.env.BACKEND_URL || 'http://127.0.0.1:5080';

export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url);
    const backendUrlWithParams = `${backendUrl}/api/central/products/letter-counts?${searchParams.toString()}`;

    const response = await fetch(backendUrlWithParams, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Backend error: ${response.status}`);
    }

    const data = await response.json();
    return NextResponse.json(data);
  } catch (error) {
    console.error('Letter Counts API proxy error:', error);
    return NextResponse.json(
      { success: false, error: 'Failed to fetch letter counts' },
      { status: 500 }
    );
  }
}
