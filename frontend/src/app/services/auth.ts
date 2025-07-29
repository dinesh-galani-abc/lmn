import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})

export class Auth {

  //private redirectUri = 'http://localhost:4200/callback';
  private apiUri = 'https://localhost:7117/';

  constructor(private http: HttpClient) { }

  redirectToIdp(email: string): void {
    const body = { email };
    const endpoint = `${this.apiUri.replace(/\/$/, '')}/api/auth/begin`;

    this.http
      .post<{ authUrl: string }>(endpoint, body /*, { headers } */)
      .subscribe({
        next: (response) => {
          console.log(response);
          if (response?.authUrl) {
            window.location.href = response.authUrl;
          } else {
            console.error('No authUrl in response');
            alert('Invalid response from server.');
          }
        },
        error: (err) => {
          console.error('Failed to get auth URL:', err);
          alert('Something went wrong while redirecting.');
        },
      });
  }
}