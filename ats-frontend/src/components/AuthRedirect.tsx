import { useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export const AuthRedirect: React.FC = () => {
  const { isAuthenticated, isLoading, roles } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    // Only redirect if we're on the home page and user is authenticated
    if (!isLoading && isAuthenticated && location.pathname === '/') {
      // Проверяем роли с учетом регистра
      const hasRecruiterRole = roles.some(role => role.toLowerCase() === 'recruiter');
      const hasManagerRole = roles.some(role => role.toLowerCase() === 'manager');
      
      // Redirect managers to evaluation page
      if (hasManagerRole) {
        navigate('/manager/evaluation', { replace: true });
      } else if (hasRecruiterRole) {
        // Redirect recruiters to dashboard
        navigate('/dashboard', { replace: true });
      } else if (isAuthenticated) {
        // Other authenticated users go to dashboard
        navigate('/dashboard', { replace: true });
      }
    }
  }, [isAuthenticated, isLoading, roles, navigate, location.pathname]);

  return null;
};
