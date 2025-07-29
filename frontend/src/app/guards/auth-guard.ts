import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);

  // This is just a placeholder for actual auth check (e.g., token existence)
  const token = localStorage.getItem('access_token');

  if (token) {
    return true;
  } else {
    router.navigate(['/login']);
    return false;
  }
};
