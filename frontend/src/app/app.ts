import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrls: ['./app.scss']
})
export class AppComponent {
  email = '';
  errorMessage = '';

  constructor(private http: HttpClient) {}

  onSubmit() {
    this.errorMessage = '';
    this.http.post<{ redirectUrl: string }>('/api/auth/begin', { email: this.email })
      .subscribe({
        next: (res) => {
          window.location.href = res.redirectUrl;
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'An error occurred. Please try again.';
        }
      });
  }
}
