export default async function Page() {
  const res = await fetch('http://localhost:3000/api/opas/health', { cache: 'no-store' })
  const text = await res.text()
  return <pre>/api/opas/health → {text}</pre>
}
