import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url);
    
    // Backend URL'ini al
    const backendUrl = process.env.OPAS_BACKEND_URL || 'http://127.0.0.1:5080';
    
    // Query parametrelerini backend'e ilet
    const backendUrlWithParams = `${backendUrl}/api/logs/export?${searchParams.toString()}`;
    
    const response = await fetch(backendUrlWithParams, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Backend error: ${response.status}`);
    }

    // CSV ise dosya olarak döndür
    if (searchParams.get('format') === 'csv') {
      const csvData = await response.text();
      return new NextResponse(csvData, {
        headers: {
          'Content-Type': 'text/csv',
          'Content-Disposition': `attachment; filename="logs_export_${new Date().toISOString().split('T')[0]}.csv"`,
        },
      });
    }

    // JSON ise normal response
    const data = await response.json();
    return NextResponse.json(data);
  } catch (error) {
    console.error('Export API proxy error:', error);
    return NextResponse.json(
      { 
        success: false, 
        error: error instanceof Error ? error.message : 'Unknown error' 
      },
      { status: 500 }
    );
  }
}
