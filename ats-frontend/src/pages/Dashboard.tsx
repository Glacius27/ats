import React, { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { vacancyService } from '../services/vacancyService';
import { candidateService } from '../services/candidateService';
import './Dashboard.css';

export const Dashboard: React.FC = () => {
  const { user, authorizedUser, roles } = useAuth();
  const [stats, setStats] = useState({
    openVacancies: 0,
    totalCandidates: 0,
    loading: true,
  });

  useEffect(() => {
    fetchStats();
  }, []);

  const fetchStats = async () => {
    try {
      const [vacancies, candidates] = await Promise.all([
        vacancyService.getAll(),
        candidateService.getAll(),
      ]);
      
      setStats({
        openVacancies: vacancies.filter(v => v.status === 'Open').length,
        totalCandidates: candidates.length,
        loading: false,
      });
    } catch (error) {
      console.error('Error fetching stats:', error);
      setStats(prev => ({ ...prev, loading: false }));
    }
  };

  const getDisplayName = () => {
    if (user?.given_name && user?.family_name) {
      return `${user.given_name} ${user.family_name}`;
    }
    if (user?.name) {
      return user.name;
    }
    return authorizedUser?.username || user?.preferred_username || 'Пользователь';
  };

  return (
    <div className="dashboard">
      <h1>Панель управления</h1>
      <div className="dashboard-content">
        <div className="dashboard-stats">
          <div className="stat-card">
            <div className="stat-icon">📋</div>
            <div className="stat-content">
              <div className="stat-value">{stats.loading ? '...' : stats.openVacancies}</div>
              <div className="stat-label">Открытых вакансий</div>
            </div>
          </div>
          
          <div className="stat-card">
            <div className="stat-icon">👥</div>
            <div className="stat-content">
              <div className="stat-value">{stats.loading ? '...' : stats.totalCandidates}</div>
              <div className="stat-label">Откликов</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
