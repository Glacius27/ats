# Quick Start Guide

## Prerequisites

Before starting the frontend application, ensure:

1. **Keycloak is running**:
   ```bash
   cd ats-keycloak
   docker-compose up -d
   ```
   Keycloak should be accessible at http://localhost:8080

2. **Vacancy Service is running**:
   The vacancy service should be running and accessible:
   - Kubernetes: http://vacancy.local
   - Local development: http://localhost:5039

3. **Keycloak Client Configuration**:
   - Realm: `ats`
   - Client ID: `ats-frontend`
   - Client Type: Public
   - Valid Redirect URIs: `http://localhost:3000/*`
   - Web Origins: `*`
   - Standard Flow Enabled: Yes
   - Direct Access Grants Enabled: Yes

## Setup Steps

1. **Install dependencies**:
   ```bash
   cd ats-frontend
   npm install
   ```

2. **Configure environment** (if not already done):
   Create a `.env` file in the `ats-frontend` directory:
   ```env
   # For Kubernetes:
   REACT_APP_KEYCLOAK_URL=http://keycloak.ats.local
   REACT_APP_KEYCLOAK_REALM=ats
   REACT_APP_KEYCLOAK_CLIENT_ID=ats-frontend
   REACT_APP_VACANCY_SERVICE_URL=http://vacancy.local
   REACT_APP_AUTHORIZATION_SERVICE_URL=http://authorization.local
   REACT_APP_CANDIDATE_SERVICE_URL=http://candidate.local
   
   # For local development:
   # REACT_APP_KEYCLOAK_URL=http://localhost:8080
   # REACT_APP_VACANCY_SERVICE_URL=http://localhost:5039
   ```

3. **Start the application**:
   ```bash
   npm start
   ```

   The application will open automatically at http://localhost:3000

## Testing Authentication

The Keycloak realm includes test users:
- **Username**: `candidate` / **Password**: `123456`
- **Username**: `recruiter` / **Password**: `123456`
- **Username**: `manager` / **Password**: `123456`

## Application Structure

- **Public Pages**:
  - `/` - Home page with company information
  - `/vacancies` - List of open positions

- **Private Pages** (requires authentication):
  - `/dashboard` - User dashboard with profile information

## Troubleshooting

### Keycloak Connection Issues
- Verify Keycloak is running: `docker ps | grep keycloak`
- Check Keycloak logs: `docker logs keycloak`
- Verify the realm and client are configured correctly

### Vacancy Service Connection Issues
- Verify the vacancy service is running
- Check the service URL in `.env` matches the actual service port
- Verify CORS is enabled on the vacancy service (should allow `http://localhost:3000`)

### Authentication Not Working
- Clear browser cache and cookies
- Verify Keycloak redirect URIs include `http://localhost:3000/*`
- Check browser console for errors
