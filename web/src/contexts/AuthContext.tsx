import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import type { User } from '../types/auth';
import { tokenStore } from '../services/api';
import { authService } from '../services/authService';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (accessToken: string, user: User) => void;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Na montagem, tenta renovar o token silenciosamente usando o cookie httpOnly
  // Se houver cookie de refresh válido, o backend retorna um novo access token
  useEffect(() => {
    const trysilentRefresh = async () => {
      try {
        const response = await authService.refresh();
        tokenStore.set(response.accessToken);
        setUser(response.user);
      } catch {
        // Sem sessão ativa — usuário precisará fazer login
      } finally {
        setIsLoading(false);
      }
    };
    trysilentRefresh();
  }, []);

  const login = (accessToken: string, userData: User) => {
    tokenStore.set(accessToken);
    setUser(userData);
  };

  const logout = async () => {
    try {
      await authService.logout();
    } finally {
      tokenStore.clear();
      setUser(null);
    }
  };

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth deve ser usado dentro de AuthProvider');
  return context;
}
