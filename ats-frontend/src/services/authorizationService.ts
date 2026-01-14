import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_AUTHORIZATION_SERVICE_URL || 'http://authorization.local';

export interface UserResponse {
  id: string;
  username: string;
  email: string;
  keycloakUserId?: string;
  isActive: boolean;
  roles: string[];
}

const authorizationApi = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000,
});

// Request interceptor will be set up per-request with token

// Add response interceptor for debugging
authorizationApi.interceptors.response.use(
  (response) => {
    console.log('Authorization service response:', response.data);
    return response;
  },
  (error) => {
    console.error('Authorization service error:', error.response?.data || error.message);
    return Promise.reject(error);
  }
);

export const authorizationService = {
  getUserByKeycloakId: async (keycloakUserId: string, token?: string): Promise<UserResponse> => {
    const config = token ? {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    } : {};
    
    const response = await authorizationApi.get<UserResponse>(
      `/users/by-keycloak-id/${keycloakUserId}`,
      config
    );
    
    return response.data;
  },

  getUserById: async (userId: string, token?: string): Promise<UserResponse> => {
    const config = token ? {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    } : {};
    
    const response = await authorizationApi.get<UserResponse>(
      `/users/${userId}`,
      config
    );
    
    return response.data;
  },

  getUserRoles: async (userId: string, token?: string): Promise<string[]> => {
    const config = token ? {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    } : {};
    
    const response = await authorizationApi.get<string[]>(
      `/roles/${userId}`,
      config
    );
    
    return response.data;
  },
};
