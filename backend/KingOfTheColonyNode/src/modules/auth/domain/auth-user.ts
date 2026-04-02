export type UserRole = 'user' | 'moderator' | 'admin';

export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
  bio: string | null;
  credits: number;
  passwordHash: string;
  roles: UserRole[];
  refreshTokenHash: string | null;
  createdAt: Date;
  updatedAt: Date;
}