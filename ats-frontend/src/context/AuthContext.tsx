import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import keycloak from '../config/keycloak';
import type { KeycloakTokenParsed } from 'keycloak-js';
import { authorizationService, UserResponse } from '../services/authorizationService';

interface AuthContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
  token: string | undefined;
  user: KeycloakTokenParsed | undefined;
  authorizedUser: UserResponse | undefined;
  roles: string[];
  login: () => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [token, setToken] = useState<string | undefined>();
  const [user, setUser] = useState<KeycloakTokenParsed | undefined>();
  const [authorizedUser, setAuthorizedUser] = useState<UserResponse | undefined>();
  const [roles, setRoles] = useState<string[]>([]);

  useEffect(() => {
    const initKeycloak = async () => {
      try {
        const authenticated = await keycloak.init({
          onLoad: 'check-sso',
          checkLoginIframe: false, // Disable iframe check to avoid redirect issues
          silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html',
          pkceMethod: 'S256',
          redirectUri: window.location.origin + '/', // Explicit redirect URI
        });

        setIsAuthenticated(authenticated);
        setToken(keycloak.token);
        setUser(keycloak.tokenParsed);

        if (authenticated) {
          keycloak.loadUserProfile().catch(console.error);
          
          // Authorize user if already authenticated
          if (keycloak.tokenParsed?.sub) {
            await authorizeUser(keycloak.tokenParsed.sub, keycloak.token);
          }
        }

        // Token refresh
        keycloak.onTokenExpired = () => {
          keycloak.updateToken(30).then((refreshed: boolean) => {
            if (refreshed) {
              setToken(keycloak.token || undefined);
            }
          }).catch(() => {
            setIsAuthenticated(false);
            setToken(undefined);
          });
        };

        // Auth state changes
        keycloak.onAuthSuccess = async () => {
          setIsAuthenticated(true);
          setToken(keycloak.token);
          setUser(keycloak.tokenParsed);
          
          // After successful authentication, authorize user in authorization service
          if (keycloak.tokenParsed?.sub) {
            await authorizeUser(keycloak.tokenParsed.sub, keycloak.token);
          }
        };

        keycloak.onAuthLogout = () => {
          setIsAuthenticated(false);
          setToken(undefined);
          setUser(undefined);
          setAuthorizedUser(undefined);
          setRoles([]);
        };
      } catch (error) {
        console.error('Failed to initialize Keycloak', error);
      } finally {
        setIsLoading(false);
      }
    };

    initKeycloak();
  }, []);

  const authorizeUser = async (keycloakUserId: string, authToken?: string) => {
    try {
      const userData = await authorizationService.getUserByKeycloakId(keycloakUserId, authToken);
      setAuthorizedUser(userData);
      setRoles(userData.roles || []);
      console.log('User authorized:', userData);
    } catch (error: any) {
      if (error.response?.status === 404) {
        // User not found in authorization service
        // This is expected for new users who haven't been created in the system yet
        console.warn('User not found in authorization service. User may need to be created by admin.');
        setAuthorizedUser(undefined);
        setRoles([]);
      } else {
        console.error('Failed to authorize user:', error);
        setAuthorizedUser(undefined);
        setRoles([]);
      }
    }
  };

  const login = () => {
    keycloak.login();
  };

  const logout = () => {
    keycloak.logout();
  };

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        isLoading,
        token,
        user,
        authorizedUser,
        roles,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
