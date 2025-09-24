export default async function Page() {
  const res = await fetch('http://localhost:3000/api/opas/flags', { cache: 'no-store' })
  const data = await res.json()
  return <pre>{JSON.stringify(data, null, 2)}</pre>
}
