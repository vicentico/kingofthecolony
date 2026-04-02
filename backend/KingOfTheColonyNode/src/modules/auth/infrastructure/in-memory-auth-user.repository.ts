import { Injectable } from '@nestjs/common';
import type { AuthUser } from '../domain/auth-user';
import type { AuthUserRepository } from '../domain/auth-user.repository';

@Injectable()
export class InMemoryAuthUserRepository implements AuthUserRepository {
  private readonly usersById = new Map<string, AuthUser>();
  private readonly idsByEmail = new Map<string, string>();

  async create(user: AuthUser): Promise<AuthUser> {
    this.usersById.set(user.id, { ...user });
    this.idsByEmail.set(user.email, user.id);
    return { ...user };
  }

  async findByEmail(email: string): Promise<AuthUser | null> {
    const userId = this.idsByEmail.get(email);
    if (!userId) {
      return null;
    }

    const user = this.usersById.get(userId);
    return user ? { ...user } : null;
  }

  async findById(id: string): Promise<AuthUser | null> {
    const user = this.usersById.get(id);
    return user ? { ...user } : null;
  }

  async update(user: AuthUser): Promise<AuthUser> {
    this.usersById.set(user.id, { ...user });
    this.idsByEmail.set(user.email, user.id);
    return { ...user };
  }
}