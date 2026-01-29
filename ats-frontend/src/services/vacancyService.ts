import axios from 'axios';
import keycloak from '../config/keycloak';

const API_BASE_URL = process.env.REACT_APP_VACANCY_SERVICE_URL || 'http://vacancy.local';

export interface Vacancy {
  id: string;
  title: string;
  description: string;
  location: string;
  department?: string;
  status?: string;
  recruiterId?: string;
  createdAt?: string;
}

const vacancyApi = axios.create({
  baseURL: `${API_BASE_URL}/api/vacancies`,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000, // 10 seconds timeout
});

// Add request interceptor for debugging
vacancyApi.interceptors.request.use(
  (config) => {
    console.log('Making request to:', config.url);
    console.log('Request method:', config.method);
    console.log('Request data:', config.data);
    // Add token from keycloak if available and not already set
    if (keycloak && keycloak.token && !config.headers.Authorization) {
      config.headers.Authorization = `Bearer ${keycloak.token}`;
    }
    return config;
  },
  (error) => {
    console.error('Request error:', error);
    return Promise.reject(error);
  }
);

// Add response interceptor for debugging
vacancyApi.interceptors.response.use(
  (response) => {
    console.log('Response received:', response.data);
    return response;
  },
  (error) => {
    console.error('Response error:', error.response?.data || error.message);
    return Promise.reject(error);
  }
);

export interface CreateVacancyRequest {
  title: string;
  description: string;
  location: string;
  department?: string;
  recruiterId?: string;
}

export interface UpdateVacancyRequest {
  title?: string;
  description?: string;
  location?: string;
  department?: string;
  status?: string;
}

export const vacancyService = {
  getAll: async (): Promise<Vacancy[]> => {
    const response = await vacancyApi.get<Vacancy[]>('');
    return response.data;
  },

  getById: async (id: string): Promise<Vacancy> => {
    const response = await vacancyApi.get<Vacancy>(`/${id}`);
    return response.data;
  },

  create: async (request: CreateVacancyRequest, token?: string): Promise<Vacancy> => {
    const config = token ? {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    } : {};
    const response = await vacancyApi.post<Vacancy>('', request, config);
    return response.data;
  },

  update: async (id: string, request: UpdateVacancyRequest, token?: string): Promise<void> => {
    const config = token ? {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    } : {};
    // Используем PATCH для частичных обновлений
    await vacancyApi.patch(`/${id}`, request, config);
  },

  delete: async (id: string, token?: string): Promise<void> => {
    const config = token ? {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    } : {};
    await vacancyApi.delete(`/${id}`, config);
  },
};
