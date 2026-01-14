import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Sidebar } from './Sidebar';
import './Layout.css';

interface LayoutProps {
  children: React.ReactNode;
}

export const Layout: React.FC<LayoutProps> = ({ children }) => {
  const { isAuthenticated, user, authorizedUser, roles, login, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  // Get full name from Keycloak token (firstName + lastName)
  const getDisplayName = () => {
    if (user?.given_name && user?.family_name) {
      return `${user.given_name} ${user.family_name}`;
    }
    if (user?.name) {
      return user.name;
    }
    return authorizedUser?.username || user?.preferred_username || 'Пользователь';
  };
  
  const displayName = getDisplayName();
  
  // Debug: log roles to console
  React.useEffect(() => {
    if (isAuthenticated) {
      console.log('User roles:', roles);
      console.log('Authorized user:', authorizedUser);
    }
  }, [isAuthenticated, roles, authorizedUser]);

  return (
    <div className={`layout ${isAuthenticated ? 'layout-authenticated' : ''}`}>
      <header className={`header ${isAuthenticated ? 'header-authenticated' : ''}`}>
        <div className="header-container">
          <Link to={isAuthenticated ? "/dashboard" : "/"} className="logo">
            <h1>ORBITA Systems</h1>
          </Link>
          <nav className="nav">
            {isAuthenticated ? (
              <div className="user-info">
                <span className="user-name">
                  {displayName}
                  {roles.length > 0 && (
                    <span className="user-roles"> ({roles.join(', ')})</span>
                  )}
                </span>
                <button onClick={handleLogout} className="logout-btn">
                  Выйти
                </button>
              </div>
            ) : (
              <>
                <Link to="/" className="nav-link">Главная</Link>
                <Link to="/vacancies" className="nav-link">Вакансии</Link>
                <button onClick={login} className="login-btn">
                  Войти
                </button>
              </>
            )}
          </nav>
        </div>
      </header>
      {isAuthenticated && <Sidebar />}
      <main className={`main-content ${isAuthenticated ? 'main-content-authenticated' : ''}`}>
        {children}
      </main>
      {!isAuthenticated && (
        <footer className="footer">
          <p>&copy; 2024 ORBITA Systems. Все права защищены.</p>
        </footer>
      )}
    </div>
  );
};
