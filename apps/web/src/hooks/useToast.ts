'use client'

import { useState, useCallback } from 'react'

export interface Toast {
  id: string
  type: 'success' | 'error' | 'warning' | 'info'
  title: string
  message?: string
  duration?: number
  action?: {
    label: string
    onClick: () => void
  }
}

export const useToast = () => {
  const [toasts, setToasts] = useState<Toast[]>([])

  const showToast = useCallback((toast: Omit<Toast, 'id'>) => {
    const id = Math.random().toString(36).substr(2, 9)
    const newToast: Toast = {
      id,
      duration: 5000, // Default 5 seconds
      ...toast
    }

    setToasts(prev => [...prev, newToast])

    // Auto remove after duration
    if (newToast.duration && newToast.duration > 0) {
      setTimeout(() => {
        setToasts(prev => prev.filter(t => t.id !== id))
      }, newToast.duration)
    }

    return id
  }, [])

  const removeToast = useCallback((id: string) => {
    setToasts(prev => prev.filter(toast => toast.id !== id))
  }, [])

  const removeAllToasts = useCallback(() => {
    setToasts([])
  }, [])

  // Convenience methods
  const success = useCallback((title: string, message?: string, options?: Partial<Toast>) => {
    return showToast({ type: 'success', title, message, ...options })
  }, [showToast])

  const error = useCallback((title: string, message?: string, options?: Partial<Toast>) => {
    return showToast({ type: 'error', title, message, duration: 8000, ...options }) // Errors stay longer
  }, [showToast])

  const warning = useCallback((title: string, message?: string, options?: Partial<Toast>) => {
    return showToast({ type: 'warning', title, message, ...options })
  }, [showToast])

  const info = useCallback((title: string, message?: string, options?: Partial<Toast>) => {
    return showToast({ type: 'info', title, message, ...options })
  }, [showToast])

  return {
    toasts,
    showToast,
    removeToast,
    removeAllToasts,
    success,
    error,
    warning,
    info
  }
}
