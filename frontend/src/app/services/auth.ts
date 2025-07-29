import { Injectable } from '@angular/core';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})

export class Auth {
  private idpMap: { [domain: string]: string } = {
    'companyA.com': 'https://idp.companyA.com/oauth2/authorize',
    'companyB.org': 'https://idp.companyB.org/oauth2/authorize'
  };

  private clientIdMap: { [domain: string]: string } = {
    'companyA.com': 'client-id-a',
    'companyB.org': 'client-id-b'
  };

  private redirectUri = 'http://localhost:4200/callback';

  constructor(private router: Router) {}

  redirectToIdp(email: string): void {
    const domain = email.split('@')[1];

    const idpUrl = this.idpMap[domain];
    const clientId = this.clientIdMap[domain];

    if (!idpUrl || !clientId) {
      alert('User not found.');
      return;
    }

    const authUrl = `${idpUrl}?response_type=code&client_id=${clientId}&redirect_uri=${encodeURIComponent(this.redirectUri)}&scope=openid profile email&state=randomState123`;

    window.location.href = authUrl;
  }
}
