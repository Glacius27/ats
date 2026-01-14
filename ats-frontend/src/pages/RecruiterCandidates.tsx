import React, { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { candidateService, Candidate } from '../services/candidateService';
import { vacancyService, Vacancy } from '../services/vacancyService';
import { recruitmentService, CreateApplicationRequest, Application } from '../services/recruitmentService';
import './RecruiterCandidates.css';

interface CandidateWithVacancy extends Candidate {
  vacancyTitle?: string;
  vacancyId?: string;
  applicationId?: number;
}

export const RecruiterCandidates: React.FC = () => {
  const { token, authorizedUser } = useAuth();
  const [candidates, setCandidates] = useState<CandidateWithVacancy[]>([]);
  const [filteredCandidates, setFilteredCandidates] = useState<CandidateWithVacancy[]>([]);
  const [vacancies, setVacancies] = useState<Vacancy[]>([]);
  const [applications, setApplications] = useState<Application[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCandidate, setSelectedCandidate] = useState<CandidateWithVacancy | null>(null);
  const [showStartPipelineForm, setShowStartPipelineForm] = useState(false);
  const [selectedVacancyId, setSelectedVacancyId] = useState<string>('');
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    fetchData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const [candidatesData, vacanciesData, applicationsData] = await Promise.all([
        candidateService.getAll(token),
        vacancyService.getAll(),
        recruitmentService.getAllApplications(token).catch(() => []), // Если applications не загрузились, используем пустой массив
      ]);
      
      console.log('Loaded candidates:', candidatesData);
      console.log('Loaded applications:', applicationsData);
      console.log('Loaded vacancies:', vacanciesData);
      
      // Связываем кандидатов с вакансиями
      const candidatesWithVacancies: CandidateWithVacancy[] = candidatesData.map(candidate => {
        // Сначала проверяем, есть ли vacancyId в самом кандидате (из отклика)
        let vacancyId = candidate.vacancyId;
        let vacancy = vacancyId ? vacanciesData.find(v => v.id === vacancyId) : undefined;
        let applicationId: number | undefined;
        
        // Если vacancyId нет в кандидате, ищем через applications
        if (!vacancyId) {
          const application = applicationsData.find(app => {
            const appCandidateId = String(app.candidateId).trim();
            const candidateId = String(candidate.id).trim();
            return appCandidateId === candidateId;
          });
          
          if (application) {
            vacancyId = application.vacancyId;
            vacancy = vacanciesData.find(v => v.id === application.vacancyId);
            applicationId = application.id;
          }
        }
        
        return {
          ...candidate,
          vacancyTitle: vacancy?.title,
          vacancyId: vacancyId,
          applicationId: applicationId,
        };
      });
      
      console.log('Candidates with vacancies:', candidatesWithVacancies);
      
      setCandidates(candidatesWithVacancies);
      setFilteredCandidates(candidatesWithVacancies);
      setApplications(applicationsData);
      // Показываем все вакансии
      setVacancies(vacanciesData);
    } catch (err: any) {
      console.error('Error fetching data:', err);
      setError('Не удалось загрузить данные: ' + (err.message || 'Неизвестная ошибка'));
    } finally {
      setLoading(false);
    }
  };

  // Фильтрация по поисковому запросу
  useEffect(() => {
    if (!searchQuery.trim()) {
      setFilteredCandidates(candidates);
      return;
    }

    const query = searchQuery.toLowerCase().trim();
    const filtered = candidates.filter(candidate => {
      const matchesName = candidate.fullName?.toLowerCase().includes(query) || false;
      const matchesVacancy = candidate.vacancyTitle?.toLowerCase().includes(query) || false;
      return matchesName || matchesVacancy;
    });

    setFilteredCandidates(filtered);
  }, [searchQuery, candidates]);

  const handleRejectCandidate = async (candidate: CandidateWithVacancy) => {
    if (!window.confirm(`Вы уверены, что хотите отказать кандидату ${candidate.fullName}?`)) {
      return;
    }
    
    try {
      // Обновляем статус кандидата на "rejected"
      // TODO: Добавить endpoint для обновления статуса кандидата
      setError(null);
      // Пока просто обновляем локально
      setCandidates(prev => prev.map(c => 
        c.id === candidate.id ? { ...c, status: 'rejected' } : c
      ));
    } catch (err: any) {
      setError('Не удалось обновить статус кандидата');
      console.error('Error rejecting candidate:', err);
    }
  };

  const handleStartPipeline = async () => {
    if (!selectedCandidate || !selectedVacancyId) {
      setError('Выберите кандидата и вакансию');
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      const request: CreateApplicationRequest = {
        candidateId: selectedCandidate.id.toString(),
        vacancyId: selectedVacancyId,
        status: 'New',
      };

      await recruitmentService.createApplication(request, token);
      
      setShowStartPipelineForm(false);
      setSelectedCandidate(null);
      setSelectedVacancyId('');
      alert('Кандидат успешно добавлен в воронку подбора!');
      await fetchData();
    } catch (err: any) {
      console.error('Error starting pipeline:', err);
      setError(err.response?.data?.message || 'Не удалось добавить кандидата в воронку');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDownloadResume = async (candidate: CandidateWithVacancy) => {
    if (!candidate.resumeFileName) {
      alert('Резюме не найдено');
      return;
    }

    try {
      const url = `${process.env.REACT_APP_CANDIDATE_SERVICE_URL || 'http://candidate.local'}/api/candidates/${candidate.id}/resume`;
      window.open(url, '_blank');
    } catch (err) {
      console.error('Error downloading resume:', err);
      alert('Не удалось скачать резюме');
    }
  };

  if (loading) {
    return (
      <div className="recruiter-candidates-loading">
        <div className="spinner"></div>
        <p>Загрузка откликов...</p>
      </div>
    );
  }

  return (
    <div className="recruiter-candidates">
      <div className="recruiter-candidates-header">
        <h1>Отклики кандидатов</h1>
      </div>


      {/* Search bar - показываем только если нет ошибки и есть кандидаты */}
      {!error && candidates.length > 0 && (
        <div className="search-container">
          <input
            type="text"
            placeholder="Поиск по имени кандидата или вакансии..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="search-input"
          />
        </div>
      )}

      {showStartPipelineForm && selectedCandidate && (
        <div className="pipeline-form-container">
          <h2>Запустить кандидата в воронку подбора</h2>
          <div className="pipeline-form">
            <div className="candidate-info">
              <p><strong>Кандидат:</strong> {selectedCandidate.fullName}</p>
              <p><strong>Email:</strong> {selectedCandidate.email}</p>
              <p><strong>Телефон:</strong> {selectedCandidate.phone}</p>
            </div>
            <div className="form-group">
              <label htmlFor="vacancy">Выберите вакансию *</label>
              <select
                id="vacancy"
                value={selectedVacancyId}
                onChange={(e) => setSelectedVacancyId(e.target.value)}
                required
              >
                <option value="">-- Выберите вакансию --</option>
                {vacancies.map((vacancy) => (
                  <option key={vacancy.id} value={vacancy.id}>
                    {vacancy.title} - {vacancy.location}
                  </option>
                ))}
              </select>
            </div>
            <div className="form-actions">
              <button
                type="button"
                onClick={() => {
                  setShowStartPipelineForm(false);
                  setSelectedCandidate(null);
                  setSelectedVacancyId('');
                }}
                className="btn btn-secondary"
                disabled={submitting}
              >
                Отмена
              </button>
              <button
                type="button"
                onClick={handleStartPipeline}
                className="btn btn-primary"
                disabled={submitting || !selectedVacancyId}
              >
                {submitting ? 'Добавление...' : 'Добавить в воронку'}
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="candidates-list">
        {error ? (
          <div className="error-message">
            <p>{error}</p>
            <button onClick={fetchData} className="btn btn-secondary" style={{ marginTop: '1rem' }}>
              Повторить
            </button>
          </div>
        ) : filteredCandidates.length === 0 ? (
          <div className="no-candidates">
            <p>{searchQuery ? 'Ничего не найдено' : 'Пока нет откликов от кандидатов.'}</p>
          </div>
        ) : (
          <div className="candidates-grid">
            {filteredCandidates.map((candidate) => (
              <div key={candidate.id} className="candidate-card">
                <div className="candidate-card-header">
                  <h3>{candidate.fullName}</h3>
                  <span className={`candidate-status ${candidate.status?.toLowerCase()}`}>
                    {candidate.status === 'active' ? 'Активен' : candidate.status}
                  </span>
                </div>
                <div className="candidate-card-body">
                  {candidate.vacancyTitle && (
                    <div className="candidate-info-item candidate-vacancy">
                      <strong>Вакансия:</strong> {candidate.vacancyTitle}
                    </div>
                  )}
                  <div className="candidate-info-item">
                    <strong>Email:</strong> {candidate.email}
                  </div>
                  <div className="candidate-info-item">
                    <strong>Телефон:</strong> {candidate.phone}
                  </div>
                  {candidate.resumeFileName && (
                    <div className="candidate-info-item">
                      <strong>Резюме:</strong> {candidate.resumeFileName}
                    </div>
                  )}
                </div>
                <div className="candidate-card-actions">
                  {candidate.resumeFileName && (
                    <button
                      onClick={() => handleDownloadResume(candidate)}
                      className="btn btn-secondary"
                    >
                      Скачать резюме
                    </button>
                  )}
                  <button
                    onClick={() => {
                      setSelectedCandidate(candidate);
                      setShowStartPipelineForm(true);
                    }}
                    className="btn btn-primary"
                  >
                    Запустить в воронку
                  </button>
                  <button
                    onClick={() => handleRejectCandidate(candidate)}
                    className="btn btn-reject"
                  >
                    Отказать
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
