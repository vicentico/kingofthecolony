import type { UserRole } from '../domain/auth-user';

export interface AuthenticatedUser {
  id: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
  bio: string | null;
  credits: number;
  roles: UserRole[];
  createdAt: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: AuthenticatedUser;
}

export interface RequestUser {
  userId: string;
  email: string;
  roles: UserRole[];
}