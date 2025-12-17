export const environment = {
  production: true,
  apiUrl: 'https://projectorganizer-api.azurewebsites.net/api',
  auth0: {
    domain: 'dev-gp57sox40kt34si8.us.auth0.com',
    clientId: 'rOVVRDGgscn6oAqvO0vu09kA61JUKmPp',
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: 'https://projectorganizer.api'
    }
  }
};
