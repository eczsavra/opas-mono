import { NextRequest, NextResponse } from 'next/server';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5080';

export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ city: string }> }
) {
  try {
    const { city } = await params;
    
    const response = await fetch(`${API_URL}/api/gln-registry/towns/${encodeURIComponent(city)}`, {
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`API responded with status ${response.status}`);
    }

    const data = await response.json();
    return NextResponse.json(data);
  } catch (error) {
    console.error('GLN Registry Towns API error:', error);
    return NextResponse.json(
      { error: 'Failed to fetch towns data' },
      { status: 500 }
    );
  }
}
