import { HttpClient, JsonpInterceptor } from '@angular/common/http';
import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home',
  imports: [],
  templateUrl: './home.html',
  styleUrl: './home.scss'
})
export class Home {

  email: any = "";
  private apiUri = 'https://localhost:7117/';
  constructor(private http: HttpClient, private router: Router) { }

  ngOnInit() {
    this.email = this.getEmailFromCookie() ?? "";
  }

  logout() {
    if (!this.email) {
      alert('Unable to logout: Email not found.');
      return;
    }

    let email = this.email;
    let IdToken = this.getIdTokenFromCookie() ?? "";
    this.http.post<{ logoutUrl: string }>(`${this.apiUri}api/auth/logout`, { email, IdToken }).subscribe({
      next: (res) => {
        // Redirect to IDP logout
        window.location.href = res.logoutUrl;

        // Clear cookie
        document.cookie = 'AuthResponse=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
        document.cookie = 'userEmail=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
      },
      error: (err) => {
        console.error('Logout failed:', err);
        alert('Logout failed');
      }
    });


  }

  private getEmailFromCookie(): string | null {
    const match = document.cookie
      .split('; ')
      .find(row => row.startsWith('userEmail='));
    return match ? decodeURIComponent(match.split('=')[1]) : null;
  }

  private getIdTokenFromCookie(): string | null {
    const match = document.cookie
      .split('; ')
      .find(row => row.startsWith('AuthResponse='));

    let hellp = decodeURIComponent(match?.split('=')[1] ?? '');
    if (hellp) {
      return JSON.parse(hellp).id_token;
    }
    else {
      alert('No AuthResponse cookie found. Please log in again.');
      return "";
    }
  }
}
