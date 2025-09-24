export const defaultLocale = process.env.NEXT_PUBLIC_DEFAULT_LOCALE || 'tr';
export const locales = ['tr'] as const;
export type Locale = typeof locales[number];
