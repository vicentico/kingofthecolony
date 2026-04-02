import { Inject, Injectable } from '@nestjs/common';
import { AUTH_USER_REPOSITORY } from './auth.tokens';
import type { AuthUserRepository } from '../domain/auth-user.repository';
import { UserNotFoundError } from '../domain/auth.errors';

@Injectable()
export class LogoutUseCase {
  constructor(
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
  ) {}

  async execute(userId: string): Promise<void> {
    const user = await this.userRepository.findById(userId);
    if (!user) {
      throw new UserNotFoundError();
    }

    user.refreshTokenHash = null;
    user.updatedAt = new Date();
    await this.userRepository.update(user);
  }
}