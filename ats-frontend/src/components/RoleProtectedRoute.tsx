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

  // Проверяем роли с учетом регистра
  const hasRequiredRole = requiredRoles.some(requiredRole => 
    roles.some(role => role.toLowerCase() === requiredRole.toLowerCase())
  );

  // Логирование для отладки (должно быть до всех условных возвратов)
  React.useEffect(() => {
    console.log('RoleProtectedRoute - Current roles:', roles);
    console.log('RoleProtectedRoute - Required roles:', requiredRoles);
    console.log('RoleProtectedRoute - Has required role:', hasRequiredRole);
  }, [roles, requiredRoles, hasRequiredRole]);

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

  if (!hasRequiredRole) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh', flexDirection: 'column' }}>
        <h2>Доступ запрещен</h2>
        <p>У вас нет необходимых прав для доступа к этой странице.</p>
        <p>Требуемые роли: {requiredRoles.join(', ')}</p>
        <p>Ваши роли: {roles.length > 0 ? roles.join(', ') : 'Нет ролей'}</p>
      </div>
    );
  }

  return <>{children}</>;
};
