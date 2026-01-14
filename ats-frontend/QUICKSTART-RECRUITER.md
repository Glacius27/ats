# Быстрый старт для рекрутера

## Предварительные требования

1. Все сервисы развернуты в Kubernetes
2. Keycloak настроен и доступен по адресу `http://keycloak.ats.local`
3. В Keycloak есть пользователь с ролью `Recruiter` или `Manager`

## Настройка фронтенда

1. Перейдите в директорию фронтенда:
```bash
cd ats-frontend
```

2. Создайте файл `.env` (если его еще нет):
```env
REACT_APP_KEYCLOAK_URL=http://keycloak.ats.local
REACT_APP_KEYCLOAK_REALM=ats
REACT_APP_KEYCLOAK_CLIENT_ID=ats-frontend
REACT_APP_VACANCY_SERVICE_URL=http://vacancy.local
REACT_APP_AUTHORIZATION_SERVICE_URL=http://authorization.local
REACT_APP_CANDIDATE_SERVICE_URL=http://candidate.local
REACT_APP_RECRUITMENT_SERVICE_URL=http://recruitment.local
```

3. Установите зависимости (если еще не установлены):
```bash
npm install
```

4. Запустите фронтенд:
```bash
npm start
```

Приложение откроется на `http://localhost:3000`

## Использование функциональности рекрутера

### 1. Вход в систему

1. Нажмите кнопку "Войти" в правом верхнем углу
2. Войдите с учетными данными пользователя с ролью `Recruiter` или `Manager`
3. После успешного входа в меню появятся дополнительные ссылки:
   - **Мои вакансии** - управление вакансиями
   - **Отклики** - просмотр откликов кандидатов

### 2. Управление вакансиями (`/recruiter/vacancies`)

- **Создание вакансии**: Нажмите "Создать вакансию", заполните форму и нажмите "Создать"
- **Редактирование**: Нажмите "Редактировать" на карточке вакансии
- **Удаление**: Нажмите "Удалить" на карточке вакансии (требует подтверждения)
- **Изменение статуса**: При редактировании можно изменить статус на "Открыта" или "Закрыта"

### 3. Просмотр откликов кандидатов (`/recruiter/candidates`)

- **Просмотр всех откликов**: На странице отображаются все кандидаты, откликнувшиеся на вакансии
- **Скачивание резюме**: Нажмите "Скачать резюме" для просмотра файла резюме кандидата
- **Запуск в воронку подбора**: 
  1. Нажмите "Запустить в воронку" на карточке кандидата
  2. Выберите вакансию из списка
  3. Нажмите "Добавить в воронку"
  4. Кандидат будет добавлен в систему подбора (recruitment-service)

## Проверка работы сервисов

Все сервисы должны быть доступны через Ingress:

- **Vacancy Service**: http://vacancy.local
- **Candidate Service**: http://candidate.local
- **Authorization Service**: http://authorization.local
- **Recruitment Service**: http://recruitment.local
- **Keycloak**: http://keycloak.ats.local

## Тестовые пользователи

В Keycloak настроены следующие пользователи:
- `recruiter` / `123456` - роль Recruiter
- `manager` / `123456` - роль Manager
- `candidate` / `123456` - роль Candidate

## Устранение неполадок

1. **Ошибка авторизации**: Убедитесь, что пользователь существует в authorization service (может потребоваться создание через API)
2. **Ошибка CORS**: Проверьте, что все сервисы имеют правильную конфигурацию CORS
3. **Сервисы недоступны**: Проверьте статус подов в Kubernetes: `kubectl get pods -n ats`
