export const environment = {
  production: true,
  apiUrl: 'https://projectorganizer-api.azurewebsites.net/api',
  auth0: {
    domain: '[TVOJ_AUTH0_DOMAIN]',
    clientId: '[TVOJ_AUTH0_CLIENT_ID]',
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: '[TVOJ_AUTH0_AUDIENCE]'
    }
  }
};
