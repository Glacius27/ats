declare module 'keycloak-js' {
  export type KeycloakOnLoad = 'login-required' | 'check-sso';
  export type KeycloakResponseMode = 'query' | 'fragment';
  export type KeycloakResponseType = 'code' | 'id_token token' | 'code id_token token';
  export type KeycloakFlow = 'standard' | 'implicit' | 'hybrid';
  export type KeycloakPkceMethod = 'S256' | false;

  export interface KeycloakTokenParsed {
    exp?: number;
    iat?: number;
    auth_time?: number;
    jti?: string;
    iss?: string;
    aud?: string | string[];
    sub?: string;
    typ?: string;
    azp?: string;
    session_state?: string;
    acr?: string;
    'allowed-origins'?: string[];
    realm_access?: KeycloakRoles;
    resource_access?: KeycloakResourceAccess;
    scope?: string;
    email_verified?: boolean;
    name?: string;
    preferred_username?: string;
    given_name?: string;
    family_name?: string;
    email?: string;
    [key: string]: any;
  }

  export interface KeycloakRoles {
    roles: string[];
  }

  export interface KeycloakResourceAccess {
    [key: string]: KeycloakRoles;
  }

  export interface KeycloakInitOptions {
    useNonce?: boolean;
    adapter?: 'default' | 'cordova' | 'cordova-native' | any;
    onLoad?: KeycloakOnLoad;
    token?: string;
    refreshToken?: string;
    idToken?: string;
    timeSkew?: number;
    checkLoginIframe?: boolean;
    checkLoginIframeInterval?: number;
    responseMode?: KeycloakResponseMode;
    redirectUri?: string;
    silentCheckSsoRedirectUri?: string;
    silentCheckSsoFallback?: boolean;
    flow?: KeycloakFlow;
    pkceMethod?: KeycloakPkceMethod;
  }

  export interface KeycloakLoginOptions {
    scope?: string;
    redirectUri?: string;
    prompt?: string;
    action?: string;
    maxAge?: number;
    loginHint?: string;
    idpHint?: string;
    locale?: string;
    acr?: {
      values?: string[];
      essential?: boolean;
    };
  }

  export interface KeycloakProfile {
    id?: string;
    username?: string;
    email?: string;
    firstName?: string;
    lastName?: string;
    enabled?: boolean;
    emailVerified?: boolean;
    totp?: boolean;
    createdTimestamp?: number;
    [key: string]: any;
  }

  export interface KeycloakUserInfo {
    sub?: string;
    email_verified?: boolean;
    name?: string;
    preferred_username?: string;
    given_name?: string;
    family_name?: string;
    email?: string;
    [key: string]: any;
  }

  export interface KeycloakInstance {
    authenticated?: boolean;
    token?: string;
    tokenParsed?: KeycloakTokenParsed;
    refreshToken?: string;
    refreshTokenParsed?: KeycloakTokenParsed;
    idToken?: string;
    idTokenParsed?: KeycloakTokenParsed;
    realmAccess?: KeycloakRoles;
    resourceAccess?: KeycloakResourceAccess;
    timeSkew?: number;
    loginRequired?: boolean;
    authServerUrl?: string;
    realm?: string;
    clientId?: string;
    clientSecret?: string;
    redirectUri?: string;
    responseMode?: KeycloakResponseMode;
    responseType?: KeycloakResponseType;
    flow?: KeycloakFlow;
    adapter?: any;
    useNativePromise?: boolean;
    enableLogging?: boolean;
    pkceMethod?: KeycloakPkceMethod;
    checkLoginIframe?: boolean;
    checkLoginIframeInterval?: number;
    onReady?: (authenticated?: boolean) => void;
    onAuthSuccess?: () => void;
    onAuthError?: (errorData?: any) => void;
    onAuthRefreshSuccess?: () => void;
    onAuthRefreshError?: () => void;
    onTokenExpired?: () => void;
    onAuthLogout?: () => void;
    init(initOptions?: KeycloakInitOptions): Promise<boolean>;
    login(options?: KeycloakLoginOptions): Promise<void>;
    logout(options?: any): Promise<void>;
    register(options?: KeycloakLoginOptions): Promise<void>;
    accountManagement(): Promise<void>;
    createLoginUrl(options?: KeycloakLoginOptions): string;
    createLogoutUrl(options?: any): string;
    createRegisterUrl(options?: KeycloakLoginOptions): string;
    createAccountUrl(options?: any): string;
    isTokenExpired(minValidity?: number): boolean;
    updateToken(minValidity?: number): Promise<boolean>;
    clearToken(): void;
    hasRealmRole(role: string): boolean;
    hasResourceRole(role: string, resource?: string): boolean;
    loadUserProfile(): Promise<KeycloakProfile>;
    loadUserInfo(): Promise<KeycloakUserInfo>;
  }

  class Keycloak {
    constructor(config?: {
      url?: string;
      realm?: string;
      clientId?: string;
    });

    authenticated?: boolean;
    token?: string;
    tokenParsed?: KeycloakTokenParsed;
    refreshToken?: string;
    refreshTokenParsed?: KeycloakTokenParsed;
    idToken?: string;
    idTokenParsed?: KeycloakTokenParsed;
    realmAccess?: KeycloakRoles;
    resourceAccess?: KeycloakResourceAccess;
    timeSkew?: number;
    loginRequired?: boolean;
    authServerUrl?: string;
    realm?: string;
    clientId?: string;
    clientSecret?: string;
    redirectUri?: string;
    responseMode?: KeycloakResponseMode;
    responseType?: KeycloakResponseType;
    flow?: KeycloakFlow;
    adapter?: any;
    useNativePromise?: boolean;
    enableLogging?: boolean;
    pkceMethod?: KeycloakPkceMethod;
    checkLoginIframe?: boolean;
    checkLoginIframeInterval?: number;
    onReady?: (authenticated?: boolean) => void;
    onAuthSuccess?: () => void;
    onAuthError?: (errorData?: any) => void;
    onAuthRefreshSuccess?: () => void;
    onAuthRefreshError?: () => void;
    onTokenExpired?: () => void;
    onAuthLogout?: () => void;
    init(initOptions?: KeycloakInitOptions): Promise<boolean>;
    login(options?: KeycloakLoginOptions): Promise<void>;
    logout(options?: any): Promise<void>;
    register(options?: KeycloakLoginOptions): Promise<void>;
    accountManagement(): Promise<void>;
    createLoginUrl(options?: KeycloakLoginOptions): string;
    createLogoutUrl(options?: any): string;
    createRegisterUrl(options?: KeycloakLoginOptions): string;
    createAccountUrl(options?: any): string;
    isTokenExpired(minValidity?: number): boolean;
    updateToken(minValidity?: number): Promise<boolean>;
    clearToken(): void;
    hasRealmRole(role: string): boolean;
    hasResourceRole(role: string, resource?: string): boolean;
    loadUserProfile(): Promise<KeycloakProfile>;
    loadUserInfo(): Promise<KeycloakUserInfo>;
  }

  export default Keycloak;
}
