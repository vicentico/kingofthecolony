import type { AuthUser } from './auth-user';

export interface AuthUserRepository {
  create(user: AuthUser): Promise<AuthUser>;
  findByEmail(email: string): Promise<AuthUser | null>;
  findById(id: string): Promise<AuthUser | null>;
  update(user: AuthUser): Promise<AuthUser>;
}