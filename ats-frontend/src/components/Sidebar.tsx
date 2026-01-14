import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import './Sidebar.css';

export const Sidebar: React.FC = () => {
  const { roles } = useAuth();
  const location = useLocation();
  const hasRecruiterRole = roles.includes('Recruiter') || roles.includes('Manager');

  const isActive = (path: string) => {
    return location.pathname === path || location.pathname.startsWith(path + '/');
  };

  return (
    <aside className="sidebar">
      <nav className="sidebar-nav">
        <Link 
          to="/dashboard" 
          className={`sidebar-link ${isActive('/dashboard') ? 'active' : ''}`}
        >
          <span className="sidebar-icon">🚀</span>
          <span className="sidebar-text">Панель управления</span>
        </Link>
        
        {hasRecruiterRole && (
          <>
            <Link 
              to="/recruiter/vacancies" 
              className={`sidebar-link ${isActive('/recruiter/vacancies') ? 'active' : ''}`}
            >
              <span className="sidebar-icon">📋</span>
              <span className="sidebar-text">Вакансии</span>
            </Link>
            
            <Link 
              to="/recruiter/candidates" 
              className={`sidebar-link ${isActive('/recruiter/candidates') ? 'active' : ''}`}
            >
              <span className="sidebar-icon">👥</span>
              <span className="sidebar-text">Отклики</span>
            </Link>
            
            <Link 
              to="/recruiter/recruitment" 
              className={`sidebar-link ${isActive('/recruiter/recruitment') ? 'active' : ''}`}
            >
              <span className="sidebar-icon">🎯</span>
              <span className="sidebar-text">Подбор</span>
            </Link>
          </>
        )}
      </nav>
    </aside>
  );
};
