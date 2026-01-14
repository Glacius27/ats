import Keycloak from 'keycloak-js';

const keycloakConfig = {
  url: process.env.REACT_APP_KEYCLOAK_URL || 'http://keycloak.ats.local',
  realm: process.env.REACT_APP_KEYCLOAK_REALM || 'ats',
  clientId: process.env.REACT_APP_KEYCLOAK_CLIENT_ID || 'ats-frontend',
};

const keycloak = new Keycloak(keycloakConfig);

export default keycloak;
export { keycloakConfig };
