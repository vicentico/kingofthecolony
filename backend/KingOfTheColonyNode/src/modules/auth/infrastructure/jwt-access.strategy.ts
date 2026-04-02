import { Injectable, UnauthorizedException } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { PassportStrategy } from '@nestjs/passport';
import { ExtractJwt, Strategy } from 'passport-jwt';
import type { AuthTokenPayload } from '../domain/auth-token.service';
import type { RequestUser } from '../application/auth.models';

@Injectable()
export class JwtAccessStrategy extends PassportStrategy(Strategy, 'jwt-access') {
  constructor(configService: ConfigService) {
    super({
      jwtFromRequest: ExtractJwt.fromAuthHeaderAsBearerToken(),
      ignoreExpiration: false,
      secretOrKey: configService.get<string>('auth.accessSecret', 'replace_me'),
      issuer: configService.get<string>('auth.issuer', 'kingofthecolony-node'),
      audience: configService.get<string>('auth.audience', 'kingofthecolony-clients'),
    });
  }

  validate(payload: AuthTokenPayload): RequestUser {
    if (payload.type !== 'access') {
      throw new UnauthorizedException('Invalid access token.');
    }

    return {
      userId: payload.sub,
      email: payload.email,
      roles: payload.roles,
    };
  }
}