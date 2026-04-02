import { Inject, Injectable } from '@nestjs/common';
import { AUTH_TOKEN_SERVICE, AUTH_USER_REPOSITORY, PASSWORD_HASHER } from './auth.tokens';
import type { AuthTokenService } from '../domain/auth-token.service';
import type { AuthUserRepository } from '../domain/auth-user.repository';
import type { PasswordHasher } from '../domain/password-hasher';
import { InvalidRefreshTokenError, UserNotFoundError } from '../domain/auth.errors';
import { toAuthResponse } from './auth.mapper';
import type { AuthResponse } from './auth.models';

@Injectable()
export class RefreshSessionUseCase {
  constructor(
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
    @Inject(PASSWORD_HASHER) private readonly passwordHasher: PasswordHasher,
    @Inject(AUTH_TOKEN_SERVICE) private readonly authTokenService: AuthTokenService,
  ) {}

  async execute(refreshToken: string): Promise<AuthResponse> {
    const payload = await this.authTokenService.verifyRefreshToken(refreshToken);

    if (payload.type !== 'refresh') {
      throw new InvalidRefreshTokenError();
    }

    const user = await this.userRepository.findById(payload.sub);
    if (!user) {
      throw new UserNotFoundError();
    }

    if (!user.refreshTokenHash) {
      throw new InvalidRefreshTokenError();
    }

    const refreshMatches = await this.passwordHasher.compare(refreshToken, user.refreshTokenHash);
    if (!refreshMatches) {
      throw new InvalidRefreshTokenError();
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