import { Routes } from '@angular/router';
import { Home } from './pages/home/home';
import { LoginFormComponent } from "./components/login-form/login-form";
import { Callback } from './pages/callback/callback';
import { authGuard } from './guards/auth-guard';

export const routes: Routes = [
    { path: '', component: Home , canActivate: [authGuard]},
    { path: 'login', component: LoginFormComponent },
    { path: 'callback', component: Callback },
    { path: '**', redirectTo: '' },
  ];
  
