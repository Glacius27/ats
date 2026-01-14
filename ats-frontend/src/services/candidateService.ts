import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_CANDIDATE_SERVICE_URL || 'http://candidate.local';

export interface Candidate {
  id: number;
  fullName: string;
  email: string;
  phone: string;
  status: string;
  resumeFileName?: string;
  vacancyId?: string;
}

export interface CandidateCreateRequest {
  fullName: string;
  email: string;
  phone: string;
  resume?: File;
  vacancyId?: string;
}

const candidateApi = axios.create({
  baseURL: `${API_BASE_URL}/api/candidates`,
  timeout: 30000, // 30 seconds for file upload
});

// Add request interceptor for debugging
candidateApi.interceptors.request.use(
  (config) => {
    console.log('Making request to:', config.url);
    return config;
  },
  (error) => {
    console.error('Request error:', error);
    return Promise.reject(error);
  }
);

// Add response interceptor for debugging
candidateApi.interceptors.response.use(
  (response) => {
    console.log('Response received:', response.data);
    return response;
  },
  (error) => {
    console.error('Response error:', error.response?.data || error.message);
    return Promise.reject(error);
  }
);

// Add auth token to requests
candidateApi.interceptors.request.use(
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

export const candidateService = {
  getAll: async (token?: string): Promise<Candidate[]> => {
    const config = token ? {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    } : {};
    const response = await candidateApi.get<Candidate[]>('', config);
    return response.data;
  },

  getById: async (id: number, token?: string): Promise<Candidate> => {
    const config = token ? {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    } : {};
    const response = await candidateApi.get<Candidate>(`/${id}`, config);
    return response.data;
  },

  create: async (request: CandidateCreateRequest): Promise<any> => {
    const formData = new FormData();
    formData.append('FullName', request.fullName);
    formData.append('Email', request.email);
    formData.append('Phone', request.phone);
    formData.append('Status', 'active');
    
    if (request.vacancyId) {
      formData.append('VacancyId', request.vacancyId);
    }
    
    if (request.resume) {
      formData.append('Resume', request.resume);
    }

    try {
      // Axios automatically sets Content-Type to multipart/form-data with boundary for FormData
      // Don't set it manually, let axios handle it
      const response = await candidateApi.post('', formData);
      
      return response.data;
    } catch (error: any) {
      // If we get a network error but the request actually succeeded (status 200-299),
      // check if we can still extract data
      if (error.response && error.response.status >= 200 && error.response.status < 300) {
        return error.response.data;
      }
      
      // If it's a network error but we suspect CORS, log more details
      if (error.code === 'ERR_NETWORK' || error.message?.includes('Network Error')) {
        console.warn('Network error detected, but request may have succeeded. Check CORS configuration.');
      }
      
      throw error;
    }
  },
};
