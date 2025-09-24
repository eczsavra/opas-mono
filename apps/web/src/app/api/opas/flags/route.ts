import { NextResponse } from 'next/server'
import { getApiBase } from '@/lib/api'

export async function GET() {
  const url = `${getApiBase()}/v1/flags`
  try {
    const res = await fetch(url, { cache: 'no-store' })
    const ct = res.headers.get('content-type') || ''
    if (!res.ok) {
      const text = await res.text().catch(() => '')
      return NextResponse.json({ error: 'upstream error', url, status: res.status, body: text }, { status: 502 })
    }
    if (!ct.includes('application/json')) {
      const text = await res.text()
      return NextResponse.json({ error: 'unexpected content-type', url, contentType: ct, body: text }, { status: 500 })
    }
    const data = await res.json()
    return NextResponse.json(data, { status: 200 })
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : 'fetch failed'
    console.error('[flags-proxy-error]', { url, message })
    return NextResponse.json({ error: 'fetch failed', url, message }, { status: 500 })
  }
}
