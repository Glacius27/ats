import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

interface RoleProtectedRouteProps {
  children: React.ReactNode;
  requiredRoles: string[];
}

export const RoleProtectedRoute: React.FC<RoleProtectedRouteProps> = ({ 
  children, 
  requiredRoles 
}) => {
  const { isAuthenticated, isLoading, roles } = useAuth();

  if (isLoading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        <div>Загрузка...</div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const hasRequiredRole = requiredRoles.some(role => roles.includes(role));

  if (!hasRequiredRole) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh', flexDirection: 'column' }}>
        <h2>Доступ запрещен</h2>
        <p>У вас нет необходимых прав для доступа к этой странице.</p>
        <p>Требуемые роли: {requiredRoles.join(', ')}</p>
      </div>
    );
  }

  return <>{children}</>;
};
