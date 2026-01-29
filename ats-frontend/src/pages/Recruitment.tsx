import React, { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { recruitmentService, Application } from '../services/recruitmentService';
import { candidateService, Candidate } from '../services/candidateService';
import { vacancyService, Vacancy } from '../services/vacancyService';
import './Recruitment.css';

interface CandidateApplication {
  candidate: Candidate;
  application: Application | null;
  vacancy: Vacancy | null;
}

// Этапы процесса подбора (согласно бизнес-процессу)
const RECRUITMENT_STAGES = [
  { id: 'New', name: 'Новая заявка', description: 'Кандидат отобран по вакансии' },
  { id: 'ResumeReview', name: 'Оценка резюме', description: 'Линейный руководитель оценивает резюме' },
  { id: 'InterviewInvited', name: 'Приглашение на собеседование', description: 'Кандидат приглашен на собеседование' },
  { id: 'InterviewCompleted', name: 'Собеседование пройдено', description: 'Кандидат прошел собеседование' },
  { id: 'Evaluation', name: 'Оценка кандидата', description: 'Линейный руководитель оценивает кандидата' },
  { id: 'Approved', name: 'Кандидат утвержден', description: 'Кандидат утвержден линейным руководителем' },
  { id: 'ProfileCreated', name: 'Анкета создана', description: 'Создана анкета кандидата' },
  { id: 'ATSInvited', name: 'Приглашение в ATS', description: 'Кандидат приглашен в систему' },
  { id: 'ProfileFilled', name: 'Анкета заполнена', description: 'Кандидат заполнил анкету и отправил на проверку' },
  { id: 'Verification', name: 'Проверка анкеты', description: 'Проверка анкеты кандидата' },
  { id: 'OfferPreparation', name: 'Подготовка оффера', description: 'Подготовка оффера для кандидата' },
  { id: 'OfferApproved', name: 'Оффер утвержден', description: 'Оффер утвержден линейным руководителем' },
  { id: 'OfferSent', name: 'Оффер отправлен', description: 'Оффер отправлен кандидату' },
  { id: 'OfferReview', name: 'Ознакомление с оффером', description: 'Кандидат ознакомился с условиями оффера' },
  { id: 'OfferAccepted', name: 'Оффер принят', description: 'Кандидат принял оффер' },
  { id: 'Rejected', name: 'Отклонен', description: 'Кандидат отклонен' },
  { id: 'Completed', name: 'Завершен', description: 'Подбор завершен успешно' },
];

export const Recruitment: React.FC = () => {
  const { token } = useAuth();
  const [candidateApplications, setCandidateApplications] = useState<CandidateApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedCandidate, setSelectedCandidate] = useState<CandidateApplication | null>(null);
  const [expandedCards, setExpandedCards] = useState<Set<number>>(new Set());

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

      // Создаем массив кандидатов с их заявками и вакансиями
      const candidateApps: CandidateApplication[] = candidatesData.map(candidate => {
        // Находим application для этого кандидата
        const application = applicationsData.find(app => 
          String(app.candidateId).trim() === String(candidate.id).trim()
        ) || null;

        // Определяем vacancyId - сначала из application, потом из candidate
        const vacancyId = application?.vacancyId || candidate.vacancyId;
        const vacancy = vacancyId ? vacanciesData.find(v => v.id === vacancyId) || null : null;

        return {
          candidate,
          application,
          vacancy,
        };
      });

      // Показываем всех кандидатов, у которых есть application (в воронке)
      // или есть vacancyId (откликнулись на вакансию)
      const filtered = candidateApps.filter(ca => 
        ca.application !== null || ca.vacancy !== null
      );

      setCandidateApplications(filtered);
    } catch (err: any) {
      console.error('Error fetching data:', err);
      setError('Не удалось загрузить данные: ' + (err.message || 'Неизвестная ошибка'));
    } finally {
      setLoading(false);
    }
  };

  const getCurrentStage = (application: Application | null) => {
    if (!application) return RECRUITMENT_STAGES[0];
    return RECRUITMENT_STAGES.find(stage => stage.id === application.status) || RECRUITMENT_STAGES[0];
  };

  const getStageIndex = (status: string) => {
    return RECRUITMENT_STAGES.findIndex(stage => stage.id === status);
  };

  const handleStatusChange = async (applicationId: number, newStatus: string) => {
    if (!token) {
      setError('Не авторизован');
      return;
    }

    try {
      await recruitmentService.updateApplicationStatus(applicationId, newStatus, token);
      await fetchData();
    } catch (err: any) {
      console.error('Error updating status:', err);
      setError('Не удалось обновить статус: ' + (err.message || 'Неизвестная ошибка'));
    }
  };

  const getNextStage = (currentStatus: string) => {
    const currentIndex = getStageIndex(currentStatus);
    if (currentIndex < RECRUITMENT_STAGES.length - 1) {
      return RECRUITMENT_STAGES[currentIndex + 1];
    }
    return null;
  };

  const toggleCardExpansion = (candidateId: number) => {
    setExpandedCards(prev => {
      const newSet = new Set(prev);
      if (newSet.has(candidateId)) {
        newSet.delete(candidateId);
      } else {
        newSet.add(candidateId);
      }
      return newSet;
    });
  };

  const isCardExpanded = (candidateId: number) => {
    return expandedCards.has(candidateId);
  };

  if (loading) {
    return (
      <div className="recruitment-loading">
        <div className="spinner"></div>
        <p>Загрузка процесса подбора...</p>
      </div>
    );
  }

  return (
    <div className="recruitment">
      <div className="recruitment-header">
        <h1>Подбор персонала</h1>
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
        <div className="no-applications">
          <p>Нет кандидатов в процессе подбора.</p>
        </div>
      )}

      {!error && candidateApplications.length > 0 && (
        <div className="recruitment-list">
            {candidateApplications.map((ca) => {
            // Если нет application, значит кандидат только откликнулся, но еще не в воронке
            const status = ca.application?.status || 'New';
            const currentStage = getCurrentStage(ca.application);
            const currentIndex = getStageIndex(status);
            const nextStage = getNextStage(status);

            return (
              <div key={ca.candidate.id} className="recruitment-card">
                <div className="recruitment-card-header">
                  <div className="candidate-info">
                    <h3>{ca.candidate.fullName}</h3>
                    {ca.vacancy && (
                      <p className="vacancy-name">Вакансия: {ca.vacancy.title}</p>
                    )}
                  </div>
                  <div className="current-status">
                    <span className={`status-badge status-${ca.application?.status || 'New'}`}>
                      {currentStage.name}
                    </span>
                  </div>
                </div>

                <div className="recruitment-stages">
                  <div className="stages-header">
                    <h4>Этапы подбора</h4>
                    <button
                      onClick={() => toggleCardExpansion(ca.candidate.id)}
                      className="btn-toggle-stages"
                    >
                      {isCardExpanded(ca.candidate.id) ? 'Свернуть' : 'Показать все этапы'}
                    </button>
                  </div>
                  <div className={`stages-timeline ${isCardExpanded(ca.candidate.id) ? 'expanded' : 'collapsed'}`}>
                    {RECRUITMENT_STAGES.map((stage, index) => {
                      const isCompleted = index < currentIndex;
                      const isCurrent = index === currentIndex;
                      const isPending = index > currentIndex;
                      const isExpanded = isCardExpanded(ca.candidate.id);
                      // Показываем только текущий этап, если карточка свернута
                      const shouldShow = isExpanded || isCurrent || isCompleted;

                      if (!shouldShow) return null;

                      return (
                        <div
                          key={stage.id}
                          className={`stage-item ${isCompleted ? 'completed' : ''} ${isCurrent ? 'current' : ''} ${isPending ? 'pending' : ''}`}
                        >
                          <div className="stage-marker">
                            {isCompleted && <span>✓</span>}
                            {isCurrent && <span>●</span>}
                            {isPending && <span>○</span>}
                          </div>
                          <div className="stage-content">
                            <div className="stage-name">{stage.name}</div>
                            <div className="stage-description">{stage.description}</div>
                          </div>
                        </div>
                      );
                    })}
                    {!isCardExpanded(ca.candidate.id) && currentIndex < RECRUITMENT_STAGES.length - 1 && (
                      <div className="stage-item-placeholder">
                        <div className="stage-marker">
                          <span>...</span>
                        </div>
                        <div className="stage-content">
                          <div className="stage-name">Еще {RECRUITMENT_STAGES.length - currentIndex - 1} этапов</div>
                        </div>
                      </div>
                    )}
                  </div>
                </div>

                <div className="recruitment-card-actions">
                  {!ca.application && ca.vacancy && (
                    <p className="info-text">Кандидат откликнулся, но еще не добавлен в воронку подбора</p>
                  )}
                  {nextStage && ca.application && (
                    <button
                      onClick={() => handleStatusChange(ca.application!.id, nextStage.id)}
                      className="btn btn-primary"
                    >
                      Перейти к этапу: {nextStage.name}
                    </button>
                  )}
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
                <p><strong>Телефон:</strong> {selectedCandidate.candidate.phone}</p>
                {selectedCandidate.vacancy && (
                  <p><strong>Вакансия:</strong> {selectedCandidate.vacancy.title}</p>
                )}
                {selectedCandidate.application && (
                  <>
                    <p><strong>Статус:</strong> {getCurrentStage(selectedCandidate.application).name}</p>
                    <p><strong>Дата создания:</strong> {new Date(selectedCandidate.application.createdAt).toLocaleDateString('ru-RU')}</p>
                    <p><strong>Последнее обновление:</strong> {new Date(selectedCandidate.application.updatedAt).toLocaleDateString('ru-RU')}</p>
                  </>
                )}
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
