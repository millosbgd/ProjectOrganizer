import { Injectable } from '@angular/core';
import { PublicClientApplication, InteractionRequiredAuthError } from '@azure/msal-browser';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MsalMailService {
  private msalInstance: PublicClientApplication;
  private readonly scopes = ['https://graph.microsoft.com/Mail.Read'];

  constructor() {
    this.msalInstance = new PublicClientApplication({
      auth: {
        clientId: environment.msalClientId,
        authority: 'https://login.microsoftonline.com/common',
        redirectUri: window.location.origin
      },
      cache: {
        cacheLocation: 'sessionStorage'
      }
    });
  }

  async initialize(): Promise<void> {
    await this.msalInstance.initialize();
  }

  async getAccessToken(): Promise<string> {
    await this.initialize();

    const accounts = this.msalInstance.getAllAccounts();

    if (accounts.length > 0) {
      try {
        const result = await this.msalInstance.acquireTokenSilent({
          scopes: this.scopes,
          account: accounts[0]
        });
        return result.accessToken;
      } catch (error) {
        if (error instanceof InteractionRequiredAuthError) {
          return await this.loginPopup();
        }
        throw error;
      }
    }

    return await this.loginPopup();
  }

  private async loginPopup(): Promise<string> {
    const result = await this.msalInstance.loginPopup({
      scopes: this.scopes,
      prompt: 'select_account'
    });
    return result.accessToken;
  }

  getLoggedInAccount(): string | null {
    const accounts = this.msalInstance.getAllAccounts();
    return accounts.length > 0 ? accounts[0].username : null;
  }

  async logout(): Promise<void> {
    await this.initialize();
    const accounts = this.msalInstance.getAllAccounts();
    if (accounts.length > 0) {
      await this.msalInstance.logoutPopup({ account: accounts[0] });
    }
  }
}
