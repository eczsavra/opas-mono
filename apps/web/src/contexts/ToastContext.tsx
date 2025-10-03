'use client'

import React, { createContext, useContext, ReactNode } from 'react'
import { useToast, Toast } from '@/hooks/useToast'
import ToastContainer from '@/components/ToastContainer'

interface ToastContextType {
  showToast: (toast: Omit<Toast, 'id'>) => string
  success: (title: string, message?: string, options?: Partial<Toast>) => string
  error: (title: string, message?: string, options?: Partial<Toast>) => string
  warning: (title: string, message?: string, options?: Partial<Toast>) => string
  info: (title: string, message?: string, options?: Partial<Toast>) => string
  removeToast: (id: string) => void
  removeAllToasts: () => void
}

const ToastContext = createContext<ToastContextType | undefined>(undefined)

export const useToastContext = () => {
  const context = useContext(ToastContext)
  if (!context) {
    throw new Error('useToastContext must be used within a ToastProvider')
  }
  return context
}

interface ToastProviderProps {
  children: ReactNode
}

export const ToastProvider = ({ children }: ToastProviderProps) => {
  const toast = useToast()

  // Listen for custom toast events
  React.useEffect(() => {
    const handleShowToast = (event: CustomEvent) => {
      const toastData = event.detail
      toast.showToast(toastData)
    }

    document.addEventListener('showToast', handleShowToast as EventListener)
    
    return () => {
      document.removeEventListener('showToast', handleShowToast as EventListener)
    }
  }, [toast])

  return (
    <ToastContext.Provider value={toast}>
      {children}
      <ToastContainer 
        toasts={toast.toasts} 
        onRemove={toast.removeToast} 
      />
    </ToastContext.Provider>
  )
}
