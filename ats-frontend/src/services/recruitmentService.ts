import axios from 'axios';

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
    const keycloak = (window as any).keycloak;
    if (keycloak && keycloak.token) {
      config.headers.Authorization = `Bearer ${keycloak.token}`;
    }
    return config;
  },
  (error) => {
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
    const config = token ? {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    } : {};
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
