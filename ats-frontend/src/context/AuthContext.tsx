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

  // Функция для извлечения ролей из Keycloak token
  const getRolesFromKeycloakToken = (tokenParsed: KeycloakTokenParsed | undefined): string[] => {
    if (!tokenParsed) return [];
    
    // Получаем роли из realm_access
    const realmRoles = tokenParsed.realm_access?.roles || [];
    
    // Также можно получить роли из resource_access, если нужно
    // const resourceRoles = Object.values(tokenParsed.resource_access || {})
    //   .flatMap((access: any) => access.roles || []);
    
    return realmRoles;
  };

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
          
          // Получаем роли из Keycloak token сразу (fallback)
          const keycloakRoles = getRolesFromKeycloakToken(keycloak.tokenParsed);
          if (keycloakRoles.length > 0) {
            setRoles(keycloakRoles);
            console.log('Initial roles from Keycloak token:', keycloakRoles);
          }
          
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
          
          // Получаем роли из Keycloak token сразу (fallback)
          const keycloakRoles = getRolesFromKeycloakToken(keycloak.tokenParsed);
          if (keycloakRoles.length > 0) {
            setRoles(keycloakRoles);
            console.log('Roles from Keycloak token after login:', keycloakRoles);
          }
          
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
      // Используем роли из authorization service, если они есть
      const serviceRoles = userData.roles || [];
      setRoles(serviceRoles);
      console.log('User authorized:', userData);
      console.log('Roles from authorization service:', serviceRoles);
    } catch (error: any) {
      if (error.response?.status === 404) {
        // User not found in authorization service
        // Fallback: используем роли из Keycloak token
        console.warn('User not found in authorization service. Using roles from Keycloak token.');
        setAuthorizedUser(undefined);
        
        // Получаем роли из Keycloak token как fallback
        const currentTokenParsed = keycloak.tokenParsed;
        const keycloakRoles = getRolesFromKeycloakToken(currentTokenParsed);
        setRoles(keycloakRoles);
        console.log('Roles from Keycloak token:', keycloakRoles);
      } else {
        console.error('Failed to authorize user:', error);
        setAuthorizedUser(undefined);
        
        // В случае другой ошибки тоже используем роли из Keycloak token
        const currentTokenParsed = keycloak.tokenParsed;
        const keycloakRoles = getRolesFromKeycloakToken(currentTokenParsed);
        setRoles(keycloakRoles);
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
