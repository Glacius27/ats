import React, { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { vacancyService, Vacancy } from '../services/vacancyService';
import './RecruiterVacancies.css';

export const RecruiterVacancies: React.FC = () => {
  const { token, authorizedUser } = useAuth();
  const [vacancies, setVacancies] = useState<Vacancy[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
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

    try {
      if (editingVacancy) {
        await vacancyService.update(editingVacancy.id, formData, token);
      } else {
        // При создании добавляем recruiterId
        const createData = {
          ...formData,
          recruiterId: authorizedUser?.id || '',
        };
        await vacancyService.create(createData, token);
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
    } catch (err: any) {
      console.error('Error saving vacancy:', err);
      setError(err.response?.data?.message || 'Не удалось сохранить вакансию');
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
                    {vacancy.status === 'Open' ? 'Открыта' : 'Закрыта'}
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
