export function getApiBase(): string {
    const base = process.env.NEXT_PUBLIC_API_BASE || 'http://127.0.0.1:5080';
    if (!base) throw new Error('API base not configured');
    return base;
  }
  