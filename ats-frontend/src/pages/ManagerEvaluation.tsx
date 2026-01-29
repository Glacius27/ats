import React, { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { recruitmentService, Application } from '../services/recruitmentService';
import { candidateService, Candidate } from '../services/candidateService';
import { vacancyService, Vacancy } from '../services/vacancyService';
import './ManagerEvaluation.css';

interface CandidateApplication {
  candidate: Candidate;
  application: Application;
  vacancy: Vacancy | null;
}

export const ManagerEvaluation: React.FC = () => {
  const { token } = useAuth();
  const [candidateApplications, setCandidateApplications] = useState<CandidateApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedCandidate, setSelectedCandidate] = useState<CandidateApplication | null>(null);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      setError(null);

      const [candidatesData, applicationsData, vacanciesData] = await Promise.all([
        candidateService.getAll(token),
        recruitmentService.getAllApplications(token).catch(() => []),
        vacancyService.getAll(),
      ]);

      // Фильтруем заявки, которые требуют оценки manager:
      // 1. ResumeReview - оценка резюме
      // 2. InterviewCompleted - оценка после интервью
      const applicationsNeedingEvaluation = applicationsData.filter(app => 
        app.status === 'ResumeReview' || app.status === 'InterviewCompleted'
      );

      // Создаем массив кандидатов с их заявками и вакансиями
      const candidateApps: CandidateApplication[] = applicationsNeedingEvaluation
        .map(application => {
          const candidate = candidatesData.find(c => 
            String(c.id).trim() === String(application.candidateId).trim()
          );
          
          if (!candidate) return null;

          const vacancy = application.vacancyId 
            ? vacanciesData.find(v => v.id === application.vacancyId) || null
            : null;

          return {
            candidate,
            application,
            vacancy,
          };
        })
        .filter((ca): ca is CandidateApplication => ca !== null);

      setCandidateApplications(candidateApps);
    } catch (err: any) {
      console.error('Error fetching data:', err);
      setError('Не удалось загрузить данные: ' + (err.message || 'Неизвестная ошибка'));
    } finally {
      setLoading(false);
    }
  };

  const handleResumeReview = async (applicationId: number, decision: 'approve' | 'reject') => {
    if (!token) {
      setError('Не авторизован');
      return;
    }

    try {
      if (decision === 'approve') {
        // Пригласить на собеседование
        await recruitmentService.updateApplicationStatus(applicationId, 'InterviewInvited', token);
      } else {
        // Отказать
        await recruitmentService.updateApplicationStatus(applicationId, 'Rejected', token);
      }
      await fetchData();
    } catch (err: any) {
      console.error('Error updating status:', err);
      setError('Не удалось обновить статус: ' + (err.message || 'Неизвестная ошибка'));
    }
  };

  const handleInterviewEvaluation = async (applicationId: number, decision: 'approve' | 'reject') => {
    if (!token) {
      setError('Не авторизован');
      return;
    }

    try {
      if (decision === 'approve') {
        // Утвердить кандидата
        await recruitmentService.updateApplicationStatus(applicationId, 'Approved', token);
      } else {
        // Отказать
        await recruitmentService.updateApplicationStatus(applicationId, 'Rejected', token);
      }
      await fetchData();
    } catch (err: any) {
      console.error('Error updating status:', err);
      setError('Не удалось обновить статус: ' + (err.message || 'Неизвестная ошибка'));
    }
  };

  const getStatusLabel = (status: string) => {
    switch (status) {
      case 'ResumeReview':
        return 'Оценка резюме';
      case 'InterviewCompleted':
        return 'Оценка после интервью';
      default:
        return status;
    }
  };

  if (loading) {
    return (
      <div className="manager-evaluation-loading">
        <div className="spinner"></div>
        <p>Загрузка кандидатов для оценки...</p>
      </div>
    );
  }

  return (
    <div className="manager-evaluation">
      <div className="manager-evaluation-header">
        <h1>Оценка кандидатов</h1>
        <p className="subtitle">Оцените резюме кандидатов и результаты интервью</p>
      </div>

      {error && (
        <div className="error-message">
          <p>{error}</p>
          <button onClick={fetchData} className="btn btn-secondary" style={{ marginTop: '1rem' }}>
            Повторить
          </button>
        </div>
      )}

      {!error && candidateApplications.length === 0 && (
        <div className="no-candidates">
          <p>Нет кандидатов, требующих оценки.</p>
        </div>
      )}

      {!error && candidateApplications.length > 0 && (
        <div className="candidates-list">
          {candidateApplications.map((ca) => {
            const isResumeReview = ca.application.status === 'ResumeReview';
            const isInterviewEvaluation = ca.application.status === 'InterviewCompleted';

            return (
              <div key={ca.candidate.id} className="evaluation-card">
                <div className="evaluation-card-header">
                  <div className="candidate-info">
                    <h3>{ca.candidate.fullName}</h3>
                    {ca.vacancy && (
                      <p className="vacancy-name">Вакансия: {ca.vacancy.title}</p>
                    )}
                    <p className="candidate-email">Email: {ca.candidate.email}</p>
                    {ca.candidate.phone && (
                      <p className="candidate-phone">Телефон: {ca.candidate.phone}</p>
                    )}
                  </div>
                  <div className="status-badge-container">
                    <span className={`status-badge status-${ca.application.status}`}>
                      {getStatusLabel(ca.application.status)}
                    </span>
                  </div>
                </div>

                <div className="evaluation-content">
                  {isResumeReview && (
                    <div className="evaluation-section">
                      <h4>Оценка резюме</h4>
                      <p className="evaluation-description">
                        Пожалуйста, оцените резюме кандидата и примите решение:
                      </p>
                      {ca.candidate.resumeFileName && (
                        <div className="resume-section">
                          <button
                            onClick={() => {
                              const url = `${process.env.REACT_APP_CANDIDATE_SERVICE_URL || 'http://candidate.local'}/api/candidates/${ca.candidate.id}/resume`;
                              window.open(url, '_blank');
                            }}
                            className="btn btn-secondary"
                          >
                            📄 Скачать резюме
                          </button>
                        </div>
                      )}
                      <div className="evaluation-actions">
                        <button
                          onClick={() => handleResumeReview(ca.application.id, 'approve')}
                          className="btn btn-primary"
                        >
                          ✓ Пригласить на собеседование
                        </button>
                        <button
                          onClick={() => handleResumeReview(ca.application.id, 'reject')}
                          className="btn btn-danger"
                        >
                          ✗ Отказать
                        </button>
                      </div>
                    </div>
                  )}

                  {isInterviewEvaluation && (
                    <div className="evaluation-section">
                      <h4>Оценка после интервью</h4>
                      <p className="evaluation-description">
                        Кандидат прошел собеседование. Пожалуйста, дайте оценку и примите решение:
                      </p>
                      <div className="evaluation-actions">
                        <button
                          onClick={() => handleInterviewEvaluation(ca.application.id, 'approve')}
                          className="btn btn-primary"
                        >
                          ✓ Утвердить кандидата
                        </button>
                        <button
                          onClick={() => handleInterviewEvaluation(ca.application.id, 'reject')}
                          className="btn btn-danger"
                        >
                          ✗ Отказать
                        </button>
                      </div>
                    </div>
                  )}
                </div>

                <div className="evaluation-card-footer">
                  <button
                    onClick={() => setSelectedCandidate(ca)}
                    className="btn btn-secondary"
                  >
                    Подробнее
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {selectedCandidate && (
        <div className="candidate-detail-modal">
          <div className="modal-content">
            <div className="modal-header">
              <h2>{selectedCandidate.candidate.fullName}</h2>
              <button
                onClick={() => setSelectedCandidate(null)}
                className="modal-close"
              >
                ×
              </button>
            </div>
            <div className="modal-body">
              <div className="detail-section">
                <h3>Информация о кандидате</h3>
                <p><strong>Email:</strong> {selectedCandidate.candidate.email}</p>
                <p><strong>Телефон:</strong> {selectedCandidate.candidate.phone || 'Не указан'}</p>
                {selectedCandidate.vacancy && (
                  <p><strong>Вакансия:</strong> {selectedCandidate.vacancy.title}</p>
                )}
                <p><strong>Статус:</strong> {getStatusLabel(selectedCandidate.application.status)}</p>
                <p><strong>Дата создания заявки:</strong> {new Date(selectedCandidate.application.createdAt).toLocaleDateString('ru-RU')}</p>
                <p><strong>Последнее обновление:</strong> {new Date(selectedCandidate.application.updatedAt).toLocaleDateString('ru-RU')}</p>
              </div>
              {selectedCandidate.candidate.resumeFileName && (
                <div className="detail-section">
                  <h3>Резюме</h3>
                  <button
                    onClick={() => {
                      const url = `${process.env.REACT_APP_CANDIDATE_SERVICE_URL || 'http://candidate.local'}/api/candidates/${selectedCandidate.candidate.id}/resume`;
                      window.open(url, '_blank');
                    }}
                    className="btn btn-secondary"
                  >
                    Скачать резюме
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
