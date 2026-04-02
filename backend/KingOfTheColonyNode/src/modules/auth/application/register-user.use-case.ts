import { Inject, Injectable } from '@nestjs/common';
import { randomUUID } from 'node:crypto';
import { AUTH_TOKEN_SERVICE, AUTH_USER_REPOSITORY, PASSWORD_HASHER } from './auth.tokens';
import type { AuthTokenService } from '../domain/auth-token.service';
import type { AuthUserRepository } from '../domain/auth-user.repository';
import type { PasswordHasher } from '../domain/password-hasher';
import { EmailAlreadyRegisteredError } from '../domain/auth.errors';
import { toAuthResponse } from './auth.mapper';
import type { AuthResponse } from './auth.models';

export interface RegisterUserCommand {
  email: string;
  password: string;
  displayName: string;
}

@Injectable()
export class RegisterUserUseCase {
  constructor(
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
    @Inject(PASSWORD_HASHER) private readonly passwordHasher: PasswordHasher,
    @Inject(AUTH_TOKEN_SERVICE) private readonly authTokenService: AuthTokenService,
  ) {}

  async execute(command: RegisterUserCommand): Promise<AuthResponse> {
    const normalizedEmail = command.email.trim().toLowerCase();
    const existingUser = await this.userRepository.findByEmail(normalizedEmail);

    if (existingUser) {
      throw new EmailAlreadyRegisteredError(normalizedEmail);
    }

    const passwordHash = await this.passwordHasher.hash(command.password);
    const now = new Date();

    const user = await this.userRepository.create({
      id: randomUUID(),
      email: normalizedEmail,
      displayName: command.displayName.trim(),
      avatarUrl: null,
      bio: null,
      credits: 3,
      passwordHash,
      roles: ['user'],
      refreshTokenHash: null,
      createdAt: now,
      updatedAt: now,
    });

    const tokenPair = await this.authTokenService.issueTokenPair({
      sub: user.id,
      email: user.email,
      roles: user.roles,
    });

    user.refreshTokenHash = await this.passwordHasher.hash(tokenPair.refreshToken);
    user.updatedAt = new Date();
    await this.userRepository.update(user);

    return toAuthResponse(user, tokenPair);
  }
}