import React, { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { vacancyService, Vacancy } from '../services/vacancyService';
import keycloak from '../config/keycloak';
import './RecruiterVacancies.css';

export const RecruiterVacancies: React.FC = () => {
  const { token, authorizedUser, user } = useAuth();
  const [vacancies, setVacancies] = useState<Vacancy[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [editingVacancy, setEditingVacancy] = useState<Vacancy | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const [formData, setFormData] = useState({
    title: '',
    description: '',
    location: '',
    department: '',
    status: 'Open',
  });

  useEffect(() => {
    fetchVacancies();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const fetchVacancies = async () => {
    try {
      setLoading(true);
      const data = await vacancyService.getAll();
      // Показываем все вакансии
      setVacancies(data);
      setError(null);
    } catch (err: any) {
      console.error('Error fetching vacancies:', err);
      setError('Не удалось загрузить вакансии');
    } finally {
      setLoading(false);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    setSuccessMessage(null);

    try {
      if (!token) {
        setError('Токен авторизации отсутствует. Пожалуйста, войдите заново.');
        return;
      }

      if (editingVacancy) {
        await vacancyService.update(editingVacancy.id, formData, token);
        setSuccessMessage('Вакансия успешно обновлена!');
      } else {
        // При создании добавляем recruiterId
        // Приоритет: ID из authorization service > Keycloak user ID из контекста > Keycloak user ID напрямую
        const keycloakUserId = user?.sub || keycloak.tokenParsed?.sub || '';
        const recruiterId = authorizedUser?.id || keycloakUserId || '';
        
        console.log('Creating vacancy with:', {
          authorizedUser: authorizedUser,
          authorizedUserId: authorizedUser?.id,
          keycloakUserIdFromContext: user?.sub,
          keycloakUserIdDirect: keycloak.tokenParsed?.sub,
          finalRecruiterId: recruiterId,
        });
        
        const createData = {
          ...formData,
          recruiterId: recruiterId,
        };
        
        console.log('Sending create request:', createData);
        await vacancyService.create(createData, token);
        setSuccessMessage('Вакансия успешно создана!');
      }
      
      setShowCreateForm(false);
      setEditingVacancy(null);
      setFormData({
        title: '',
        description: '',
        location: '',
        department: '',
        status: 'Open',
      });
      await fetchVacancies();
      
      // Скрыть сообщение об успехе через 3 секунды
      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (err: any) {
      console.error('Error saving vacancy:', err);
      let errorMessage = 'Не удалось сохранить вакансию';
      
      if (err.response?.status === 401) {
        errorMessage = 'Ошибка авторизации. Пожалуйста, войдите заново.';
      } else if (err.response?.status === 403) {
        errorMessage = 'У вас нет прав для создания вакансий.';
      } else if (err.response?.data?.message) {
        errorMessage = err.response.data.message;
      } else if (err.message) {
        errorMessage = err.message;
      }
      
      setError(errorMessage);
    } finally {
      setSubmitting(false);
    }
  };

  const handleEdit = (vacancy: Vacancy) => {
    setEditingVacancy(vacancy);
    setFormData({
      title: vacancy.title,
      description: vacancy.description,
      location: vacancy.location,
      department: vacancy.department || '',
      status: vacancy.status || 'Open',
    });
    setShowCreateForm(true);
  };

  const handleArchive = async (id: string) => {
    if (!window.confirm('Перевести вакансию в архив? Она не будет отображаться в публичном разделе.')) {
      return;
    }

    try {
      setError(null);
      if (!token) {
        setError('Токен авторизации отсутствует. Пожалуйста, войдите заново.');
        return;
      }

      const vacancy = vacancies.find(v => v.id === id);
      if (!vacancy) {
        setError('Вакансия не найдена');
        return;
      }

      // Получаем полную вакансию с сервера
      const fullVacancy = await vacancyService.getById(id);
      
      // Обновляем только статус, отправляя полную модель
      await vacancyService.update(id, {
        title: fullVacancy.title,
        description: fullVacancy.description,
        location: fullVacancy.location,
        department: fullVacancy.department || '',
        status: 'Archived',
      }, token);
      
      setSuccessMessage('Вакансия переведена в архив');
      await fetchVacancies();
      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (err: any) {
      console.error('Error archiving vacancy:', err);
      let errorMessage = 'Не удалось перевести вакансию в архив';
      if (err.response?.data?.message) {
        errorMessage = err.response.data.message;
      }
      setError(errorMessage);
    }
  };

  const handleUnarchive = async (id: string) => {
    try {
      setError(null);
      if (!token) {
        setError('Токен авторизации отсутствует. Пожалуйста, войдите заново.');
        return;
      }

      const vacancy = vacancies.find(v => v.id === id);
      if (!vacancy) {
        setError('Вакансия не найдена');
        return;
      }

      // Получаем полную вакансию с сервера
      const fullVacancy = await vacancyService.getById(id);
      
      // Обновляем только статус, отправляя полную модель
      await vacancyService.update(id, {
        title: fullVacancy.title,
        description: fullVacancy.description,
        location: fullVacancy.location,
        department: fullVacancy.department || '',
        status: 'Open',
      }, token);
      
      setSuccessMessage('Вакансия восстановлена из архива');
      await fetchVacancies();
      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (err: any) {
      console.error('Error unarchiving vacancy:', err);
      let errorMessage = 'Не удалось восстановить вакансию из архива';
      if (err.response?.data?.message) {
        errorMessage = err.response.data.message;
      }
      setError(errorMessage);
    }
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Вы уверены, что хотите удалить эту вакансию?')) {
      return;
    }

    try {
      await vacancyService.delete(id, token);
      await fetchVacancies();
    } catch (err: any) {
      console.error('Error deleting vacancy:', err);
      setError('Не удалось удалить вакансию');
    }
  };

  const handleCancel = () => {
    setShowCreateForm(false);
    setEditingVacancy(null);
    setFormData({
      title: '',
      description: '',
      location: '',
      department: '',
      status: 'Open',
    });
  };

  if (loading) {
    return (
      <div className="recruiter-vacancies-loading">
        <div className="spinner"></div>
        <p>Загрузка вакансий...</p>
      </div>
    );
  }

  return (
    <div className="recruiter-vacancies">
      <div className="recruiter-vacancies-header">
        <h1>Управление вакансиями</h1>
        {!showCreateForm && (
          <button onClick={() => setShowCreateForm(true)} className="btn btn-primary">
            Создать вакансию
          </button>
        )}
      </div>

      {error && (
        <div className="error-message">
          <p>{error}</p>
        </div>
      )}

      {successMessage && (
        <div className="success-message">
          <p>{successMessage}</p>
        </div>
      )}

      {showCreateForm && (
        <div className="vacancy-form-container">
          <h2>{editingVacancy ? 'Редактировать вакансию' : 'Создать вакансию'}</h2>
          <form onSubmit={handleSubmit} className="vacancy-form">
            <div className="form-group">
              <label htmlFor="title">Название *</label>
              <input
                type="text"
                id="title"
                name="title"
                value={formData.title}
                onChange={handleInputChange}
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="description">Описание *</label>
              <textarea
                id="description"
                name="description"
                value={formData.description}
                onChange={handleInputChange}
                required
                rows={6}
              />
            </div>

            <div className="form-row">
              <div className="form-group">
                <label htmlFor="location">Местоположение *</label>
                <input
                  type="text"
                  id="location"
                  name="location"
                  value={formData.location}
                  onChange={handleInputChange}
                  required
                />
              </div>

              <div className="form-group">
                <label htmlFor="department">Отдел</label>
                <input
                  type="text"
                  id="department"
                  name="department"
                  value={formData.department}
                  onChange={handleInputChange}
                />
              </div>

              {editingVacancy && (
                <div className="form-group">
                  <label htmlFor="status">Статус</label>
                  <select
                    id="status"
                    name="status"
                    value={formData.status}
                    onChange={handleInputChange}
                  >
                    <option value="Open">Открыта</option>
                    <option value="Closed">Закрыта</option>
                    <option value="Archived">В архиве</option>
                  </select>
                </div>
              )}
            </div>

            <div className="form-actions">
              <button type="button" onClick={handleCancel} className="btn btn-secondary" disabled={submitting}>
                Отмена
              </button>
              <button type="submit" className="btn btn-primary" disabled={submitting}>
                {submitting ? 'Сохранение...' : editingVacancy ? 'Сохранить' : 'Создать'}
              </button>
            </div>
          </form>
        </div>
      )}

      <div className="vacancies-list">
        {vacancies.length === 0 ? (
          <div className="no-vacancies">
            <p>У вас пока нет вакансий. Создайте первую вакансию!</p>
          </div>
        ) : (
          <div className="vacancies-grid">
            {vacancies.map((vacancy) => (
              <div key={vacancy.id} className="vacancy-card">
                <div className="vacancy-card-header">
                  <h3>{vacancy.title}</h3>
                  <span className={`vacancy-status ${vacancy.status?.toLowerCase()}`}>
                    {vacancy.status === 'Open' ? 'Открыта' : 
                     vacancy.status === 'Closed' ? 'Закрыта' : 
                     vacancy.status === 'Archived' ? 'В архиве' : 
                     vacancy.status}
                  </span>
                </div>
                <div className="vacancy-card-body">
                  <p className="vacancy-location">📍 {vacancy.location}</p>
                  {vacancy.department && (
                    <p className="vacancy-department">🏢 {vacancy.department}</p>
                  )}
                  <p className="vacancy-description">{vacancy.description}</p>
                </div>
                <div className="vacancy-card-actions">
                  <button onClick={() => handleEdit(vacancy)} className="btn btn-secondary">
                    Редактировать
                  </button>
                  {vacancy.status !== 'Archived' && (
                    <button 
                      onClick={() => handleArchive(vacancy.id)} 
                      className="btn btn-archive"
                    >
                      В архив
                    </button>
                  )}
                  {vacancy.status === 'Archived' && (
                    <button 
                      onClick={() => handleUnarchive(vacancy.id)} 
                      className="btn btn-unarchive"
                    >
                      Из архива
                    </button>
                  )}
                  <button onClick={() => handleDelete(vacancy.id)} className="btn btn-danger">
                    Удалить
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
