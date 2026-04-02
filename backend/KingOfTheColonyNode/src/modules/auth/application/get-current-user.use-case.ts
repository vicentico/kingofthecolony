import { Inject, Injectable } from '@nestjs/common';
import { AUTH_USER_REPOSITORY } from './auth.tokens';
import type { AuthUserRepository } from '../domain/auth-user.repository';
import { UserNotFoundError } from '../domain/auth.errors';
import { toAuthenticatedUser } from './auth.mapper';
import type { AuthenticatedUser } from './auth.models';

@Injectable()
export class GetCurrentUserUseCase {
  constructor(
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
  ) {}

  async execute(userId: string): Promise<AuthenticatedUser> {
    const user = await this.userRepository.findById(userId);
    if (!user) {
      throw new UserNotFoundError();
    }

    return toAuthenticatedUser(user);
  }
}