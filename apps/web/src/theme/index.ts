import { createTheme } from '@mui/material/styles'
import { palette } from './palette'
import { typography } from './typography'
import { components } from './components'
import { breakpoints, shadows } from './spacing'

export const lightTheme = createTheme({
  palette: {
    mode: 'light',
    ...palette.light,
  },
  typography,
  spacing: (factor: number) => `${factor * 8}px`,
  breakpoints: {
    values: breakpoints,
  },
  shadows: shadows.light as [
    "none",
    string, string, string, string, string,
    string, string, string, string, string,
    string, string, string, string, string,
    string, string, string, string, string,
    string, string, string, string
  ],
  components,
})

export const darkTheme = createTheme({
  palette: {
    mode: 'dark',
    ...palette.dark,
  },
  typography,
  spacing: (factor: number) => `${factor * 8}px`,
  breakpoints: {
    values: breakpoints,
  },
  shadows: shadows.dark as [
    "none",
    string, string, string, string, string,
    string, string, string, string, string,
    string, string, string, string, string,
    string, string, string, string, string,
    string, string, string, string
  ],
  components,
})