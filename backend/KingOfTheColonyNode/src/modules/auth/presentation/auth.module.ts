import { Module } from '@nestjs/common';
import { JwtModule } from '@nestjs/jwt';
import { PassportModule } from '@nestjs/passport';
import { ConfigService } from '@nestjs/config';
import { AuthController } from './auth.controller';
import { GetCurrentUserUseCase } from '../application/get-current-user.use-case';
import { LoginUseCase } from '../application/login.use-case';
import { LogoutUseCase } from '../application/logout.use-case';
import { RefreshSessionUseCase } from '../application/refresh-session.use-case';
import { RegisterUserUseCase } from '../application/register-user.use-case';
import { AUTH_TOKEN_SERVICE, AUTH_USER_REPOSITORY, PASSWORD_HASHER } from '../application/auth.tokens';
import { BcryptPasswordHasher } from '../infrastructure/bcrypt-password-hasher';
import { InMemoryAuthUserRepository } from '../infrastructure/in-memory-auth-user.repository';
import { JwtAccessStrategy } from '../infrastructure/jwt-access.strategy';
import { JwtAuthTokenService } from '../infrastructure/jwt-auth-token.service';
import { PostgresAuthUserRepository } from '../infrastructure/postgres-auth-user.repository';
import { JwtAccessAuthGuard } from './guards/jwt-access-auth.guard';

@Module({
  imports: [
    PassportModule,
    JwtModule.registerAsync({
      inject: [ConfigService],
      useFactory: (configService: ConfigService) => ({
        secret: configService.get<string>('auth.accessSecret', 'replace_me'),
        signOptions: {
          issuer: configService.get<string>('auth.issuer', 'kingofthecolony-node'),
          audience: configService.get<string>('auth.audience', 'kingofthecolony-clients'),
        },
      }),
    }),
  ],
  controllers: [AuthController],
  providers: [
    {
      provide: AUTH_USER_REPOSITORY,
      inject: [ConfigService],
      useFactory: (configService: ConfigService) => {
        const provider = configService.get<string>('database.provider', 'postgres');
        const postgresUrl = configService.get<string>('database.postgresUrl', '');

        if (provider === 'postgres' && postgresUrl) {
          return new PostgresAuthUserRepository(postgresUrl);
        }

        return new InMemoryAuthUserRepository();
      },
    },
    {
      provide: PASSWORD_HASHER,
      useClass: BcryptPasswordHasher,
    },
    {
      provide: AUTH_TOKEN_SERVICE,
      useClass: JwtAuthTokenService,
    },
    RegisterUserUseCase,
    LoginUseCase,
    RefreshSessionUseCase,
    LogoutUseCase,
    GetCurrentUserUseCase,
    JwtAccessStrategy,
    JwtAccessAuthGuard,
  ],
  exports: [JwtAccessAuthGuard, AUTH_USER_REPOSITORY],
})
export class AuthModule {}