import type { UserRole } from './auth-user';

export interface AuthTokenPayload {
  sub: string;
  email: string;
  roles: UserRole[];
  type: 'access' | 'refresh';
}

export interface AuthTokenPair {
  accessToken: string;
  refreshToken: string;
}

export interface AuthTokenService {
  issueTokenPair(payload: Omit<AuthTokenPayload, 'type'>): Promise<AuthTokenPair>;
  verifyRefreshToken(token: string): Promise<AuthTokenPayload>;
}