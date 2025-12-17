export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',  // Lokalni backend
  auth0: {
    domain: 'dev-gp57sox40kt34si8.us.auth0.com',  // Zameni sa tvojim Auth0 domenom
    clientId: 'rOVVRDGgscn6oAqvO0vu09kA61JUKmPp',  // Zameni sa tvojim Auth0 Client ID
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: 'https://projectorganizer.api'  // Zameni sa tvojim Auth0 API identifikatorom
    }
  }
};
