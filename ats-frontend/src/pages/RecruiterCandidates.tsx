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
  displayStatus?: string; // Статус для отображения (может отличаться от статуса в БД)
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
      
      // Отладочная информация для связывания
      console.log('Vacancy IDs from vacancies:', vacanciesData.map(v => ({ id: v.id, title: v.title, idType: typeof v.id })));
      console.log('Vacancy IDs from candidates:', candidatesData.map(c => ({ 
        candidateId: c.id, 
        candidateIdType: typeof c.id,
        vacancyId: c.vacancyId, 
        vacancyIdType: typeof c.vacancyId,
        fullName: c.fullName 
      })));
      console.log('Applications mapping:', applicationsData.map(app => ({ 
        applicationId: app.id, 
        candidateId: app.candidateId, 
        vacancyId: app.vacancyId 
      })));
      
      // Связываем кандидатов с вакансиями
      const candidatesWithVacancies: CandidateWithVacancy[] = candidatesData.map(candidate => {
        let vacancyId: string | undefined = candidate.vacancyId;
        let vacancy = undefined;
        let applicationId: number | undefined;
        
        console.log(`Processing candidate ${candidate.id} (${candidate.fullName}):`, {
          candidateVacancyId: candidate.vacancyId,
          candidateVacancyIdType: typeof candidate.vacancyId,
        });
        
        // Сначала проверяем, есть ли vacancyId в самом кандидате (из отклика)
        if (vacancyId) {
          // Нормализуем ID для сравнения (убираем пробелы, приводим к строке)
          vacancyId = String(vacancyId).trim();
          console.log(`  Looking for vacancy with ID: "${vacancyId}"`);
          
          // Пробуем разные варианты сравнения
          vacancy = vacanciesData.find(v => {
            const vId = String(v.id).trim();
            const match = vId === vacancyId;
            if (match) {
              console.log(`  ✓ Found vacancy: "${v.title}" (ID: ${vId})`);
            }
            return match;
          });
          
          if (!vacancy) {
            console.warn(`  ✗ Vacancy not found with exact match. Available IDs:`, 
              vacanciesData.map(v => ({ id: String(v.id).trim(), title: v.title }))
            );
          }
        } else {
          console.log(`  No vacancyId in candidate`);
        }
        
        // Если вакансия не найдена через vacancyId кандидата, ищем через applications
        if (!vacancy) {
          const candidateIdStr = String(candidate.id).trim();
          console.log(`  Trying to find via applications for candidate ID: "${candidateIdStr}"`);
          
          const application = applicationsData.find(app => {
            const appCandidateId = String(app.candidateId).trim();
            const match = appCandidateId === candidateIdStr;
            if (match) {
              console.log(`  ✓ Found application:`, app);
            }
            return match;
          });
          
          if (application) {
            vacancyId = String(application.vacancyId).trim();
            console.log(`  Looking for vacancy from application with ID: "${vacancyId}"`);
            vacancy = vacanciesData.find(v => String(v.id).trim() === vacancyId);
            if (vacancy) {
              console.log(`  ✓ Found vacancy via application: "${vacancy.title}"`);
            }
            applicationId = application.id;
          } else {
            console.log(`  ✗ No application found for candidate`);
          }
        }
        
        // Определяем статус для отображения: если есть application, значит кандидат в воронке
        let displayStatus = candidate.status;
        if (applicationId && candidate.status === 'active') {
          displayStatus = 'in_progress'; // В работе
        }
        
        const result = {
          ...candidate,
          vacancyTitle: vacancy?.title || (vacancyId ? `Вакансия ID: ${vacancyId}` : undefined),
          vacancyId: vacancyId,
          applicationId: applicationId,
          displayStatus: displayStatus, // Статус для отображения
        };
        
        console.log(`  Final result for candidate ${candidate.id}:`, {
          vacancyTitle: result.vacancyTitle,
          vacancyId: result.vacancyId,
          applicationId: result.applicationId,
          status: candidate.status,
          displayStatus: result.displayStatus,
        });
        
        return result;
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
      setError(null);
      
      if (!token) {
        setError('Токен авторизации отсутствует. Пожалуйста, войдите заново.');
        return;
      }
      
      // Обновляем статус кандидата на "rejected" через API
      await candidateService.updateStatus(candidate.id, 'rejected', token);
      
      // Обновляем локальное состояние
      setCandidates(prev => prev.map(c => 
        c.id === candidate.id ? { ...c, status: 'rejected', displayStatus: 'rejected' } : c
      ));
      setFilteredCandidates(prev => prev.map(c => 
        c.id === candidate.id ? { ...c, status: 'rejected', displayStatus: 'rejected' } : c
      ));
    } catch (err: any) {
      console.error('Error rejecting candidate:', err);
      let errorMessage = 'Не удалось обновить статус кандидата';
      
      if (err.response?.status === 401) {
        errorMessage = 'Ошибка авторизации. Пожалуйста, войдите заново.';
      } else if (err.response?.data?.message) {
        errorMessage = err.response.data.message;
      } else if (err.message) {
        errorMessage = err.message;
      }
      
      setError(errorMessage);
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

      console.log('Creating application with request:', request);
      const createdApplication = await recruitmentService.createApplication(request, token);
      console.log('Application created successfully:', createdApplication);
      
      // Обновляем статус кандидата локально на "В работе" (будет обновлено после fetchData)
      setShowStartPipelineForm(false);
      setSelectedCandidate(null);
      setSelectedVacancyId('');
      alert('Кандидат успешно добавлен в воронку подбора!');
      
      // Перезагружаем данные, чтобы получить актуальный applicationId и обновить статус
      await fetchData();
    } catch (err: any) {
      console.error('Error starting pipeline:', err);
      console.error('Error details:', {
        message: err.message,
        response: err.response?.data,
        status: err.response?.status,
        statusText: err.response?.statusText,
      });
      
      let errorMessage = 'Не удалось добавить кандидата в воронку';
      if (err.response?.data?.message) {
        errorMessage = err.response.data.message;
      } else if (err.response?.status === 409) {
        errorMessage = 'Кандидат уже добавлен в воронку для этой вакансии';
      } else if (err.response?.status === 400) {
        errorMessage = err.response.data || 'Некорректные данные запроса';
      } else if (err.message) {
        errorMessage = err.message;
      }
      
      setError(errorMessage);
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
                  <div className="candidate-header-info">
                    <h3>{candidate.fullName}</h3>
                    {candidate.vacancyTitle && (
                      <div className="candidate-vacancy-badge">
                        📋 {candidate.vacancyTitle}
                      </div>
                    )}
                  </div>
                  <span className={`candidate-status ${(candidate.displayStatus || candidate.status)?.toLowerCase()}`}>
                    {(candidate.displayStatus || candidate.status) === 'active' ? 'Активен' : 
                     (candidate.displayStatus || candidate.status) === 'in_progress' ? 'В работе' :
                     candidate.status === 'rejected' ? 'Отказано' : 
                     candidate.status || 'Активен'}
                  </span>
                </div>
                <div className="candidate-card-body">
                  <div className="candidate-info-item candidate-vacancy">
                    <strong>Вакансия:</strong>{' '}
                    {candidate.vacancyTitle ? (
                      <span className="vacancy-title-text">{candidate.vacancyTitle}</span>
                    ) : (
                      <span className="vacancy-title-empty">Не указана</span>
                    )}
                  </div>
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
                  {candidate.status !== 'rejected' && (
                    <>
                      {/* Показываем кнопку "Запустить в воронку" только если кандидат еще не в воронке */}
                      {!candidate.applicationId && (
                        <button
                          onClick={() => {
                            setSelectedCandidate(candidate);
                            setShowStartPipelineForm(true);
                          }}
                          className="btn btn-primary"
                        >
                          Запустить в воронку
                        </button>
                      )}
                      {/* Показываем кнопку "Отказать" только если кандидат не в воронке */}
                      {!candidate.applicationId && (
                        <button
                          onClick={() => handleRejectCandidate(candidate)}
                          className="btn btn-reject"
                        >
                          Отказать
                        </button>
                      )}
                      {/* Если кандидат в воронке, показываем индикатор */}
                      {candidate.applicationId && (
                        <span className="in-progress-indicator">В воронке подбора</span>
                      )}
                    </>
                  )}
                  {candidate.status === 'rejected' && (
                    <span className="rejected-indicator">Кандидат отклонен</span>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
