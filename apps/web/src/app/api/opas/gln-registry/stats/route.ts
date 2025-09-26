import { NextResponse } from 'next/server';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5080';

export async function GET() {
  try {
    const response = await fetch(`${API_URL}/api/gln-registry/stats`, {
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
    console.error('GLN Registry Stats API error:', error);
    return NextResponse.json(
      { error: 'Failed to fetch GLN registry stats' },
      { status: 500 }
    );
  }
}
