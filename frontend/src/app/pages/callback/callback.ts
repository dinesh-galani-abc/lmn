import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-callback',
  imports: [],
  templateUrl: './callback.html',
  styleUrl: './callback.scss'
})

export class Callback {

  constructor(private route: ActivatedRoute, private http: HttpClient, private router: Router) { }
  private apiUri = 'https://localhost:7117/';
  // callback.component.ts
  ngOnInit(): void {

    if (!document.cookie.split('; ').find(row => row.startsWith('AuthResponse='))) {

      const code = this.route.snapshot.queryParamMap.get('code');
      const state = this.route.snapshot.queryParamMap.get('state');

      if (!code || !state) {
        alert('Missing code or state');
        return;
      }
      this.http.post(`${this.apiUri}api/auth/callback`, { code, state }).subscribe({
        next: (res: any) => {
          console.log('Login success:', res);
          // Save token or navigate
          const token = res['access_token'];
          const expirationTime = res['expires_in']; // Assuming the response contains expiration time in seconds
          const expirationDate = new Date(new Date().getTime() + expirationTime * 1000);

          document.cookie = `AuthResponse=${JSON.stringify(res)}; expires=${expirationDate.toUTCString()}; path=/`;

          let email = sessionStorage.getItem("email") ?? ""; // replace with actual email from login
          sessionStorage.removeItem("email"); // Clear session storage after use
          document.cookie = `userEmail=${encodeURIComponent(email)}; expires=${expirationDate.toUTCString()}; path=/`;

          this.navigateToHome();
        },
        error: (err) => {
          console.error('Auth failed:', err);
          alert('Authentication failed');
          this.router.navigate(['/login']);
        }
      });

    } else {
      console.log('Auth token already exists, skipping API call.');
      this.navigateToHome();
    }
  }

  navigateToHome(): void {
    this.router.navigate(['/home']);
  }
}
