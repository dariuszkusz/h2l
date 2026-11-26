import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthApiService } from './auth-api.service';
import { components } from './generated/openapi.types';
import { SetupApiService } from './setup-api.service';
import { UsersApiService } from './users-api.service';

@Injectable({
  providedIn: 'root',
})
export class H2lApiService {
  private readonly setupApi = inject(SetupApiService);
  private readonly authApi = inject(AuthApiService);
  private readonly usersApi = inject(UsersApiService);

  getSetupStatus(): Observable<components['schemas']['SetupStatusResponse']> {
    return this.setupApi.getStatus();
  }

  initializeSetup(
    payload: components['schemas']['InitializeSetupRequest'],
  ): Observable<components['schemas']['InitializeSetupResponse']> {
    return this.setupApi.initialize(payload);
  }

  login(
    payload: components['schemas']['LoginRequest'],
  ): Observable<components['schemas']['AuthTokenResponse']> {
    return this.authApi.login(payload);
  }

  refreshToken(
    payload: components['schemas']['RefreshTokenRequest'],
  ): Observable<components['schemas']['AuthTokenResponse']> {
    return this.authApi.refresh(payload);
  }

  logout(payload: components['schemas']['LogoutRequest']): Observable<void> {
    return this.authApi.logout(payload);
  }

  activateAccount(
    payload: components['schemas']['ActivateAccountRequest'],
  ): Observable<void> {
    return this.authApi.activate(payload);
  }

  getCurrentUserProfile(): Observable<components['schemas']['UserProfileResponse']> {
    return this.usersApi.getMe();
  }

  updateUserPreferences(
    payload: components['schemas']['UpdateUserPreferencesRequest'],
  ): Observable<void> {
    return this.usersApi.updateMyPreferences(payload);
  }
}
