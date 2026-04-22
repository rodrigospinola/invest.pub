export interface User {
  id: string;
  nome: string;
  email: string;
  telefone?: string;
  status: string;
  onboardingStep: string;
  createdAt: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  nome: string;
  email: string;
  password: string;
  telefone?: string;
}

export interface ApiError {
  error: {
    code: string;
    message: string;
    field?: string;
  };
}
