import { Inject, Injectable } from '@nestjs/common';
import { AUTH_TOKEN_SERVICE, AUTH_USER_REPOSITORY, PASSWORD_HASHER } from './auth.tokens';
import type { AuthTokenService } from '../domain/auth-token.service';
import type { AuthUserRepository } from '../domain/auth-user.repository';
import type { PasswordHasher } from '../domain/password-hasher';
import { InvalidCredentialsError } from '../domain/auth.errors';
import { toAuthResponse } from './auth.mapper';
import type { AuthResponse } from './auth.models';

export interface LoginCommand {
  email: string;
  password: string;
}

@Injectable()
export class LoginUseCase {
  constructor(
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
    @Inject(PASSWORD_HASHER) private readonly passwordHasher: PasswordHasher,
    @Inject(AUTH_TOKEN_SERVICE) private readonly authTokenService: AuthTokenService,
  ) {}

  async execute(command: LoginCommand): Promise<AuthResponse> {
    const normalizedEmail = command.email.trim().toLowerCase();
    const user = await this.userRepository.findByEmail(normalizedEmail);

    if (!user) {
      throw new InvalidCredentialsError();
    }

    const passwordMatches = await this.passwordHasher.compare(command.password, user.passwordHash);
    if (!passwordMatches) {
      throw new InvalidCredentialsError();
    }

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