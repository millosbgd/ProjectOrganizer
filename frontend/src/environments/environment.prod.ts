export const environment = {
  production: true,
  apiUrl: 'https://projectorganizer-bxcdcbbuhxayacbb.westeurope-01.azurewebsites.net/api',
  msalClientId: 'c6d551d1-5cce-4c82-95c8-ddfa76f8d42b',
  auth0: {
    domain: 'dev-gp57sox40kt34si8.us.auth0.com',
    clientId: 'rOVVRDGgscn6oAqvO0vu09kA61JUKmPp',
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: 'https://projectorganizer.api',
      scope: 'openid profile email offline_access'
    }
  }
};
