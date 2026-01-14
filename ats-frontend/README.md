# ATS Frontend Application

A modern React frontend application for the ATS (Applicant Tracking System) with public and private zones, integrated with Keycloak for authentication and authorization.

## Features

- **Public Zone**:
  - Company information page
  - Vacancies listing page (fetches from `ats-vacancy-service`)

- **Private Zone**:
  - Protected dashboard
  - Keycloak authentication and authorization
  - User profile information

## Prerequisites

- Node.js 16+ and npm
- Keycloak server running (default: http://localhost:8080)
- ATS Vacancy Service running (default: http://vacancy.local for Kubernetes, http://localhost:5039 for local development)

## Setup

1. Install dependencies:
```bash
npm install
```

2. Configure environment variables:
Create a `.env` file in the root directory:
```env
# For Kubernetes:
REACT_APP_KEYCLOAK_URL=http://keycloak.ats.local
REACT_APP_KEYCLOAK_REALM=ats
REACT_APP_KEYCLOAK_CLIENT_ID=ats-frontend
REACT_APP_VACANCY_SERVICE_URL=http://vacancy.local
REACT_APP_AUTHORIZATION_SERVICE_URL=http://authorization.local
REACT_APP_CANDIDATE_SERVICE_URL=http://candidate.local
REACT_APP_RECRUITMENT_SERVICE_URL=http://recruitment.local

# For local development:
# REACT_APP_KEYCLOAK_URL=http://localhost:8080
# REACT_APP_VACANCY_SERVICE_URL=http://localhost:5039
# REACT_APP_AUTHORIZATION_SERVICE_URL=http://localhost:5001
# REACT_APP_CANDIDATE_SERVICE_URL=http://localhost:5002
# REACT_APP_RECRUITMENT_SERVICE_URL=http://localhost:5003
```

3. Ensure Keycloak is configured:
   - Realm: `ats`
   - Client ID: `ats-frontend`
   - Client Type: Public
   - Valid Redirect URIs: `http://localhost:3000/*`
   - Web Origins: `*`

## Running the Application

Start the development server:
```bash
npm start
```

The application will open at http://localhost:3000

## Project Structure

```
src/
├── components/          # Reusable components
│   ├── Layout.tsx      # Main layout with navigation
│   └── ProtectedRoute.tsx  # Route protection component
├── config/             # Configuration files
│   └── keycloak.ts     # Keycloak initialization
├── context/            # React contexts
│   └── AuthContext.tsx # Authentication context
├── pages/              # Page components
│   ├── Home.tsx        # Company information page
│   ├── Vacancies.tsx   # Vacancies listing page
│   └── Dashboard.tsx   # Protected dashboard
├── services/           # API services
│   └── vacancyService.ts  # Vacancy API client
└── App.tsx             # Main app component with routing
```

## Keycloak Setup

The application uses Keycloak for authentication. Make sure:

1. Keycloak is running and accessible
2. The `ats` realm exists
3. A public client `ats-frontend` is configured with:
   - Standard Flow Enabled: Yes
   - Direct Access Grants Enabled: Yes
   - Valid Redirect URIs: `http://localhost:3000/*`
   - Web Origins: `*`

## API Integration

The frontend integrates with:
- **Vacancy Service**: Fetches open positions from `/api/vacancies`

## Building for Production

```bash
npm run build
```

This creates an optimized production build in the `build/` directory.
