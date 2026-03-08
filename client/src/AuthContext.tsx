import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import api from './api';
import type { LoginRequest, LoginResponse } from './types';

interface AuthContextType {
  isAuthenticated: boolean;
  username: string | null;
  login: (creds: LoginRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(!!localStorage.getItem('token'));
  const [username, setUsername] = useState(localStorage.getItem('username'));

  useEffect(() => {
    const token = localStorage.getItem('token');
    setIsAuthenticated(!!token);
    setUsername(localStorage.getItem('username'));
  }, []);

  const login = async (creds: LoginRequest) => {
    const { data } = await api.post<LoginResponse>('/auth/login', creds);
    localStorage.setItem('token', data.token);
    localStorage.setItem('username', data.username);
    setIsAuthenticated(true);
    setUsername(data.username);
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('username');
    setIsAuthenticated(false);
    setUsername(null);
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, username, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be within AuthProvider');
  return ctx;
}
