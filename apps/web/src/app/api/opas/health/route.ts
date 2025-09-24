import { NextResponse } from 'next/server'
import { getApiBase } from '@/lib/api'

export async function GET() {
  const url = `${getApiBase()}/health`
  try {
    const res = await fetch(url, { cache: 'no-store' })
    const text = await res.text()
    return new NextResponse(text, { status: res.status })
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : 'fetch failed'
    console.error('[health-proxy-error]', { url, message })
    return NextResponse.json({ error: 'fetch failed', url, message }, { status: 500 })
  }
}
