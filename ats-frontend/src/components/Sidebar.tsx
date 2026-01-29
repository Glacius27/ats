import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import './Sidebar.css';

export const Sidebar: React.FC = () => {
  const { roles } = useAuth();
  const location = useLocation();
  
  // Проверяем роли с учетом регистра
  const hasRecruiterRole = roles.some(role => 
    role.toLowerCase() === 'recruiter'
  );
  
  const hasManagerRole = roles.some(role => 
    role.toLowerCase() === 'manager'
  );
  
  // Отладочная информация
  React.useEffect(() => {
    console.log('Sidebar - Current roles:', roles);
    console.log('Sidebar - Has recruiter role:', hasRecruiterRole);
    console.log('Sidebar - Has manager role:', hasManagerRole);
  }, [roles, hasRecruiterRole, hasManagerRole]);

  const isActive = (path: string) => {
    return location.pathname === path || location.pathname.startsWith(path + '/');
  };

  return (
    <aside className="sidebar">
      <nav className="sidebar-nav">
        {/* Показываем "Панель управления" только для recruiter */}
        {hasRecruiterRole && (
          <Link 
            to="/dashboard" 
            className={`sidebar-link ${isActive('/dashboard') ? 'active' : ''}`}
          >
            <span className="sidebar-icon">🚀</span>
            <span className="sidebar-text">Панель управления</span>
          </Link>
        )}
        
        {/* Меню для recruiter */}
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
        
        {/* Меню для manager */}
        {hasManagerRole && (
          <Link 
            to="/manager/evaluation" 
            className={`sidebar-link ${isActive('/manager/evaluation') ? 'active' : ''}`}
          >
            <span className="sidebar-icon">⭐</span>
            <span className="sidebar-text">Оценка кандидатов</span>
          </Link>
        )}
      </nav>
    </aside>
  );
};
