import { Inject, Injectable } from '@nestjs/common';
import { AUTH_USER_REPOSITORY } from '../../auth/application/auth.tokens';
import type { AuthUserRepository } from '../../auth/domain/auth-user.repository';
import { UserNotFoundError } from '../../auth/domain/auth.errors';
import { toProfileResponse } from './profile.mapper';

export interface UpdateProfileCommand {
  userId: string;
  displayName?: string;
  avatarUrl?: string | null;
  bio?: string | null;
}

@Injectable()
export class UpdateProfileUseCase {
  constructor(
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
  ) {}

  async execute(command: UpdateProfileCommand) {
    const user = await this.userRepository.findById(command.userId);
    if (!user) {
      throw new UserNotFoundError();
    }

    if (typeof command.displayName === 'string') {
      user.displayName = command.displayName.trim();
    }

    if (command.avatarUrl !== undefined) {
      user.avatarUrl = command.avatarUrl;
    }

    if (command.bio !== undefined) {
      user.bio = command.bio;
    }

    user.updatedAt = new Date();
    const updated = await this.userRepository.update(user);
    return toProfileResponse(updated);
  }
}