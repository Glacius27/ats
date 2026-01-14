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
      // Redirect recruiters and managers to dashboard
      if (roles.includes('Recruiter') || roles.includes('Manager')) {
        navigate('/dashboard', { replace: true });
      } else if (isAuthenticated) {
        // Other authenticated users also go to dashboard
        navigate('/dashboard', { replace: true });
      }
    }
  }, [isAuthenticated, isLoading, roles, navigate, location.pathname]);

  return null;
};
