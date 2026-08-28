import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from '../auth/AuthProvider'
import { RequireAuth } from '../auth/RequireAuth'
import { RequireRole } from '../auth/RequireRole'
import { DashboardLayout } from '../layouts/DashboardLayout'
import { LandingPage } from '../pages/LandingPage'
import { LoginPage } from '../pages/LoginPage'
import { ForgotPasswordPage } from '../pages/ForgotPasswordPage'
import { ResetPasswordPage } from '../pages/ResetPasswordPage'
import { OverviewPage } from '../pages/OverviewPage'
import { PortCallsPage } from '../pages/PortCallsPage'
import { PortCallDetailPage } from '../pages/PortCallDetailPage'
import { PortCallFormPage } from '../pages/PortCallFormPage'
import { VesselFormPage } from '../pages/VesselFormPage'
import { BerthAgendaPage } from '../pages/BerthAgendaPage'
import { VesselsPage } from '../pages/VesselsPage'
import { AuditPage } from '../pages/AuditPage'
import { ObservabilityPage } from '../pages/ObservabilityPage'
import { UsersPage } from '../pages/UsersPage'
import { MasterDataPage } from '../pages/MasterDataPage'
import './app.css'
import '../styles/operations-theme.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
})

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <Routes>
            <Route path="/" element={<LandingPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/recuperar-senha" element={<ForgotPasswordPage />} />
            <Route path="/redefinir-senha" element={<ResetPasswordPage />} />

            <Route element={<RequireAuth />}>
              <Route element={<DashboardLayout />}>
                <Route path="/painel" element={<OverviewPage />} />
                <Route path="/navios" element={<VesselsPage />} />
                <Route path="/escalas" element={<PortCallsPage />} />
                <Route path="/escalas/:publicCode" element={<PortCallDetailPage />} />
                <Route path="/agenda" element={<BerthAgendaPage />} />

                <Route element={<RequireRole roles={['Administrator', 'Planner']} />}>
                  <Route path="/navios/novo" element={<VesselFormPage />} />
                  <Route path="/navios/:id/editar" element={<VesselFormPage />} />
                  <Route path="/escalas/nova" element={<PortCallFormPage />} />
                </Route>
                <Route element={<RequireRole roles={['Administrator']} />}>
                  <Route path="/usuarios" element={<UsersPage />} />
                  <Route path="/cadastros" element={<MasterDataPage />} />
                  <Route path="/auditoria" element={<AuditPage />} />
                  <Route path="/observabilidade" element={<ObservabilityPage />} />
                </Route>
              </Route>
            </Route>

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  )
}
