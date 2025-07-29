import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);

  if (!document.cookie.split('; ').find(row => row.startsWith('AuthResponse='))) {
    console.warn('Auth token not found in cookies, redirecting to login.');
    router.navigate(['/login']);
    return false;
  }
  else {
    return true;
  }
};
