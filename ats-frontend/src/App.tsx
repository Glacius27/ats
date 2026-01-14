import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { Layout } from './components/Layout';
import { ProtectedRoute } from './components/ProtectedRoute';
import { RoleProtectedRoute } from './components/RoleProtectedRoute';
import { AuthRedirect } from './components/AuthRedirect';
import { Home } from './pages/Home';
import { Vacancies } from './pages/Vacancies';
import { VacancyDetail } from './pages/VacancyDetail';
import { Dashboard } from './pages/Dashboard';
import { RecruiterVacancies } from './pages/RecruiterVacancies';
import { RecruiterCandidates } from './pages/RecruiterCandidates';
import { Recruitment } from './pages/Recruitment';
import './App.css';

function App() {
  return (
    <AuthProvider>
      <Router>
        <AuthRedirect />
        <Layout>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/vacancies" element={<Vacancies />} />
            <Route path="/vacancies/:id" element={<VacancyDetail />} />
            <Route
              path="/dashboard"
              element={
                <ProtectedRoute>
                  <Dashboard />
                </ProtectedRoute>
              }
            />
            <Route
              path="/recruiter/vacancies"
              element={
                <RoleProtectedRoute requiredRoles={['Recruiter', 'Manager']}>
                  <RecruiterVacancies />
                </RoleProtectedRoute>
              }
            />
            <Route
              path="/recruiter/candidates"
              element={
                <RoleProtectedRoute requiredRoles={['Recruiter', 'Manager']}>
                  <RecruiterCandidates />
                </RoleProtectedRoute>
              }
            />
            <Route
              path="/recruiter/recruitment"
              element={
                <RoleProtectedRoute requiredRoles={['Recruiter', 'Manager']}>
                  <Recruitment />
                </RoleProtectedRoute>
              }
            />
            <Route path="/login" element={<Navigate to="/" replace />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Layout>
      </Router>
    </AuthProvider>
  );
}

export default App;
