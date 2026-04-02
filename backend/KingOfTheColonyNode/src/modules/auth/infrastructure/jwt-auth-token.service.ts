import { Injectable } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { JwtService } from '@nestjs/jwt';
import type { StringValue } from 'ms';
import type { AuthTokenPair, AuthTokenPayload, AuthTokenService } from '../domain/auth-token.service';
import { InvalidRefreshTokenError } from '../domain/auth.errors';

@Injectable()
export class JwtAuthTokenService implements AuthTokenService {
  constructor(
    private readonly jwtService: JwtService,
    private readonly configService: ConfigService,
  ) {}

  async issueTokenPair(payload: Omit<AuthTokenPayload, 'type'>): Promise<AuthTokenPair> {
    const issuer = this.configService.get<string>('auth.issuer', 'kingofthecolony-node');
    const audience = this.configService.get<string>('auth.audience', 'kingofthecolony-clients');
    const accessSecret = this.configService.get<string>('auth.accessSecret', 'replace_me');
    const refreshSecret = this.configService.get<string>('auth.refreshSecret', 'replace_me');
    const accessTtl = this.configService.get<string>('auth.accessTtl', '15m') as StringValue;
    const refreshTtl = this.configService.get<string>('auth.refreshTtl', '7d') as StringValue;
    const accessPayload: AuthTokenPayload = { ...payload, type: 'access' };
    const refreshPayload: AuthTokenPayload = { ...payload, type: 'refresh' };

    const [accessToken, refreshToken] = await Promise.all([
      this.jwtService.signAsync(accessPayload, {
        secret: accessSecret,
        expiresIn: accessTtl,
        issuer,
        audience,
      }),
      this.jwtService.signAsync(refreshPayload, {
        secret: refreshSecret,
        expiresIn: refreshTtl,
        issuer,
        audience,
      }),
    ]);

    return { accessToken, refreshToken };
  }

  async verifyRefreshToken(token: string): Promise<AuthTokenPayload> {
    const issuer = this.configService.get<string>('auth.issuer', 'kingofthecolony-node');
    const audience = this.configService.get<string>('auth.audience', 'kingofthecolony-clients');
    const refreshSecret = this.configService.get<string>('auth.refreshSecret', 'replace_me');

    try {
      return await this.jwtService.verifyAsync<AuthTokenPayload>(token, {
        secret: refreshSecret,
        issuer,
        audience,
      });
    } catch {
      throw new InvalidRefreshTokenError();
    }
  }
}