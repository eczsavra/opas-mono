'use client';

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';

// Auth Types
export interface User {
  id: number;
  username: string;
  email: string;
  fullName: string;
  role: 'superadmin';
  permissions: string[];
  lastLoginAt?: string;
}


export interface AuthState {
  user: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
}

export interface AuthContextType extends AuthState {
  // Authentication Actions
  loginAsSuperAdmin: (username: string, password: string) => Promise<boolean>;
  logout: () => void;
  
  // Permission Helpers
  hasPermission: (permission: string) => boolean;
}

// Default Context Value
const defaultContextValue: AuthContextType = {
  user: null,
  isLoading: false,
  isAuthenticated: false,
  loginAsSuperAdmin: async () => false,
  logout: () => {},
  hasPermission: () => false,
};

// Create Context
const AuthContext = createContext<AuthContextType>(defaultContextValue);

// Auth Provider Props
interface AuthProviderProps {
  children: ReactNode;
}

// Auth Provider Component
export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Computed Properties
  const isAuthenticated = !!user;

  // Initialize Auth State
  useEffect(() => {
    initializeAuthState();
  }, []);

  // Initialize auth state from localStorage
  const initializeAuthState = async () => {
    try {
      setIsLoading(true);
      
      // Check localStorage for saved auth data
      const savedUser = localStorage.getItem('opas_user');
      
      if (savedUser) {
        const parsedUser: User = JSON.parse(savedUser);
        setUser(parsedUser);
        
        console.log('Auth state restored:', { user: parsedUser.username });
      }
    } catch (error) {
      console.error('Error initializing auth state:', error);
      // Clear corrupted data
      localStorage.removeItem('opas_user');
    } finally {
      setIsLoading(false);
    }
  };

  // SuperAdmin Login
  const loginAsSuperAdmin = async (username: string, password: string): Promise<boolean> => {
    try {
      setIsLoading(true);
      
      const response = await fetch('/api/opas/superadmin/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
      });

      if (!response.ok) {
        console.error('SuperAdmin login failed:', response.status);
        return false;
      }

      const data = await response.json();
      
      if (data.success && data.user) {
        const superAdminUser: User = {
          id: data.user.id,
          username: data.user.username,
          email: data.user.email,
          fullName: data.user.fullName,
          role: 'superadmin',
          permissions: data.user.permissions,
          lastLoginAt: data.user.lastLoginAt,
        };

        setUser(superAdminUser);

        // Save to localStorage
        localStorage.setItem('opas_user', JSON.stringify(superAdminUser));

        console.log('SuperAdmin login successful:', superAdminUser.username);
        return true;
      }

      return false;
    } catch (error) {
      console.error('SuperAdmin login error:', error);
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  // Logout
  const logout = () => {
    setUser(null);
    
    // Clear localStorage
    localStorage.removeItem('opas_user');
    
    console.log('User logged out');
  };

  // Permission Check
  const hasPermission = (permission: string): boolean => {
    if (!user) return false;
    return user.permissions.includes(permission) || user.permissions.includes('GLOBAL_ACCESS');
  };

  // Context Value
  const contextValue: AuthContextType = {
    user,
    isLoading,
    isAuthenticated,
    loginAsSuperAdmin,
    logout,
    hasPermission,
  };

  return (
    <AuthContext.Provider value={contextValue}>
      {children}
    </AuthContext.Provider>
  );
}

// Custom Hook
export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
