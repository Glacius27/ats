import axios from 'axios';
import keycloak from '../config/keycloak';

const API_BASE_URL = process.env.REACT_APP_RECRUITMENT_SERVICE_URL || 'http://recruitment.local';

export interface Application {
  id: number;
  candidateId: string;
  vacancyId: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  feedbacks?: Feedback[];
  offers?: Offer[];
}

export interface Feedback {
  id: number;
  applicationId: number;
  recruiterId: string;
  comment: string;
  rating?: number;
  createdAt: string;
}

export interface Offer {
  id: number;
  applicationId: number;
  salary?: number;
  startDate?: string;
  status: string;
  createdAt: string;
}

export interface CreateApplicationRequest {
  candidateId: string;
  vacancyId: string;
  status?: string;
}

const recruitmentApi = axios.create({
  baseURL: `${API_BASE_URL}/api/applications`,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000,
});

// Add auth token to requests
recruitmentApi.interceptors.request.use(
  (config) => {
    // Add token from keycloak if available and not already set
    if (keycloak && keycloak.token && !config.headers.Authorization) {
      config.headers.Authorization = `Bearer ${keycloak.token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Add response interceptor for debugging
recruitmentApi.interceptors.response.use(
  (response) => {
    console.log('Recruitment service response:', response.data);
    return response;
  },
  (error) => {
    console.error('Recruitment service error:', error.response?.data || error.message);
    return Promise.reject(error);
  }
);

export const recruitmentService = {
  getAllApplications: async (token?: string): Promise<Application[]> => {
    const config = token ? {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    } : {};
    const response = await recruitmentApi.get<Application[]>('', config);
    return response.data;
  },

  getApplicationById: async (id: number, token?: string): Promise<Application> => {
    const config = token ? {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    } : {};
    const response = await recruitmentApi.get<Application>(`/${id}`, config);
    return response.data;
  },

  createApplication: async (request: CreateApplicationRequest, token?: string): Promise<Application> => {
    // Используем токен из параметра, если передан, иначе из keycloak
    const authToken = token || keycloak.token;
    const config = authToken ? {
      headers: {
        Authorization: `Bearer ${authToken}`,
        'Content-Type': 'application/json',
      },
    } : {
      headers: {
        'Content-Type': 'application/json',
      },
    };
    
    console.log('Creating application:', {
      url: `${API_BASE_URL}/api/applications`,
      request: request,
      hasToken: !!authToken,
    });
    
    const response = await recruitmentApi.post<Application>('', request, config);
    return response.data;
  },

  updateApplicationStatus: async (id: number, status: string, token?: string): Promise<Application> => {
    const config = token ? {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    } : {};
    const response = await recruitmentApi.put<Application>(`/${id}/status`, `"${status}"`, {
      ...config,
      headers: {
        ...config.headers,
        'Content-Type': 'application/json',
      },
    });
    return response.data;
  },
};
