import { ThemeProvider } from '../providers/ThemeProvider'
import { defaultLocale } from '../i18n/config'
import { Montserrat } from 'next/font/google'

// Montserrat font configuration
const montserrat = Montserrat({
  subsets: ['latin', 'latin-ext'],
  display: 'swap',
  fallback: ['Helvetica Neue', 'Arial', 'sans-serif'],
  weight: ['300', '400', '500', '600', '700', '800', '900'],
  style: ['normal', 'italic'],
  variable: '--font-montserrat'
})

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang={defaultLocale}>
      <body className={montserrat.variable}>
        <ThemeProvider>
          {children}
        </ThemeProvider>
      </body>
    </html>
  )
}