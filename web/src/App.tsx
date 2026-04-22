import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { PhaseProvider } from './contexts/PhaseContext';
import LoginPage from './pages/auth/LoginPage';
import RegisterPage from './pages/auth/RegisterPage';
import ForgotPasswordPage from './pages/auth/ForgotPasswordPage';
import DashboardPage from './pages/DashboardPage';
import OnboardingPage from './pages/onboarding/OnboardingPage';
import OnboardingResultPage from './pages/onboarding/OnboardingResultPage';
import SubStrategyPage from './pages/portfolio/SubStrategyPage';
import RankingPage from './pages/portfolio/RankingPage';
import SuggestionPage from './pages/portfolio/SuggestionPage';
import PortfolioImportPage from './pages/portfolio/PortfolioImportPage';
import PortfolioConfirmPage from './pages/portfolio/PortfolioConfirmPage';
import StrategyPage from './pages/portfolio/StrategyPage';
import MyPortfolioPage from './pages/portfolio/MyPortfolioPage';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();
  if (isLoading) return <div>Carregando...</div>;
  return isAuthenticated ? <>{children}</> : <Navigate to="/auth/login" replace />;
}

function PublicRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();
  if (isLoading) return <div>Carregando...</div>;
  return isAuthenticated ? <Navigate to="/dashboard" replace /> : <>{children}</>;
}

function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/auth/login" replace />} />

      {/* Auth */}
      <Route path="/auth/login" element={<PublicRoute><LoginPage /></PublicRoute>} />
      <Route path="/auth/register" element={<PublicRoute><RegisterPage /></PublicRoute>} />
      <Route path="/auth/forgot-password" element={<PublicRoute><ForgotPasswordPage /></PublicRoute>} />

      {/* Dashboard */}
      <Route path="/dashboard" element={<ProtectedRoute><DashboardPage /></ProtectedRoute>} />

      {/* Fase 1 — Onboarding e perfil */}
      <Route path="/onboarding" element={<ProtectedRoute><OnboardingPage /></ProtectedRoute>} />
      <Route path="/onboarding/result" element={<ProtectedRoute><OnboardingResultPage /></ProtectedRoute>} />

      {/* Fase 2 — Estratégia e seleção de ativos */}
      <Route path="/strategy" element={<ProtectedRoute><StrategyPage /></ProtectedRoute>} />
      <Route path="/sub-strategy" element={<ProtectedRoute><SubStrategyPage /></ProtectedRoute>} />
      <Route path="/ranking" element={<ProtectedRoute><RankingPage /></ProtectedRoute>} />
      <Route path="/suggestion" element={<ProtectedRoute><SuggestionPage /></ProtectedRoute>} />
      <Route path="/portfolio/my" element={<ProtectedRoute><MyPortfolioPage /></ProtectedRoute>} />
      <Route path="/portfolio/import" element={<ProtectedRoute><PortfolioImportPage /></ProtectedRoute>} />
      <Route path="/portfolio/confirm" element={<ProtectedRoute><PortfolioConfirmPage /></ProtectedRoute>} />
    </Routes>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <PhaseProvider>
        <BrowserRouter>
          <AppRoutes />
        </BrowserRouter>
      </PhaseProvider>
    </AuthProvider>
  );
}
