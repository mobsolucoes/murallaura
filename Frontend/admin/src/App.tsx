import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider, useAuth } from './auth/AuthContext'
import Shell from './layout/Shell'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import HashtagFormPage from './pages/HashtagFormPage'
import ModerationPage from './pages/ModerationPage'
import WallVisualPage from './pages/WallVisualPage'
import LogsPage from './pages/LogsPage'
import OnboardingPage from './pages/OnboardingPage'

function Private({ children }: { children: React.ReactElement }) {
  const { auth } = useAuth()
  if (!auth.token) return <Navigate to="/login" replace />
  return children
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="/"
            element={
              <Private>
                <Shell />
              </Private>
            }
          >
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="dashboard" element={<DashboardPage />} />
            <Route path="onboarding" element={<OnboardingPage />} />
            <Route path="hashtags/new" element={<HashtagFormPage />} />
            <Route path="hashtags/:id/edit" element={<HashtagFormPage />} />
            <Route path="hashtags/:id/moderate" element={<ModerationPage />} />
            <Route path="hashtags/:id/wall" element={<WallVisualPage />} />
            <Route path="hashtags/:id/logs" element={<LogsPage />} />
          </Route>
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}
