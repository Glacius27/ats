import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { vacancyService, Vacancy } from '../services/vacancyService';
import './Vacancies.css';

export const Vacancies: React.FC = () => {
  const [vacancies, setVacancies] = useState<Vacancy[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchVacancies = async () => {
      try {
        setLoading(true);
        const data = await vacancyService.getAll();
        setVacancies(data);
        setError(null);
      } catch (err: any) {
        console.error('Error fetching vacancies:', err);
        console.error('Error details:', {
          message: err.message,
          code: err.code,
          response: err.response,
          request: err.request,
          config: err.config
        });
        
        let errorMessage = 'Не удалось загрузить вакансии. Пожалуйста, попробуйте позже.';
        
        if (err.code === 'ECONNREFUSED' || err.code === 'ERR_NETWORK') {
          errorMessage = 'Не удалось подключиться к серверу. Убедитесь, что сервис вакансий запущен и доступен по адресу: ' + (process.env.REACT_APP_VACANCY_SERVICE_URL || 'http://vacancy.local');
        } else if (err.response?.status === 404) {
          errorMessage = 'Эндпоинт не найден. Проверьте конфигурацию API.';
        } else if (err.response?.status === 0 || err.message?.includes('CORS') || err.message?.includes('Network Error')) {
          errorMessage = 'CORS ошибка или сервер недоступен. Проверьте настройки CORS на сервере и убедитесь, что сервис доступен.';
        } else if (err.response?.data?.message) {
          errorMessage = err.response.data.message;
        } else if (err.message) {
          errorMessage = err.message;
        }
        
        setError(errorMessage);
      } finally {
        setLoading(false);
      }
    };

    fetchVacancies();
  }, []);

  if (loading) {
    return (
      <div className="vacancies-loading">
        <div className="spinner"></div>
        <p>Загрузка вакансий...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="vacancies-error">
        <p>{error}</p>
        <button onClick={() => window.location.reload()} className="btn btn-primary">
          Повторить
        </button>
      </div>
    );
  }

  return (
    <div className="vacancies">
      <h1>Открытые вакансии</h1>
      {vacancies.length === 0 ? (
        <div className="no-vacancies">
          <p>На данный момент нет открытых вакансий. Зайдите позже!</p>
        </div>
      ) : (
        <div className="vacancies-grid">
          {vacancies.map((vacancy) => (
            <Link key={vacancy.id} to={`/vacancies/${vacancy.id}`} className="vacancy-card-link">
              <div className="vacancy-card">
                <h2>{vacancy.title}</h2>
                <div className="vacancy-meta">
                  <span className="vacancy-location">📍 {vacancy.location}</span>
                  {vacancy.department && (
                    <span className="vacancy-department">🏢 {vacancy.department}</span>
                  )}
                </div>
                <p className="vacancy-description">
                  {vacancy.description.length > 150 
                    ? `${vacancy.description.substring(0, 150)}...` 
                    : vacancy.description}
                </p>
                {vacancy.status && (
                  <span className={`vacancy-status ${vacancy.status.toLowerCase()}`}>
                    {vacancy.status === 'Open' ? 'Открыта' : vacancy.status === 'Closed' ? 'Закрыта' : vacancy.status}
                  </span>
                )}
                <div className="vacancy-read-more">Подробнее →</div>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
};
