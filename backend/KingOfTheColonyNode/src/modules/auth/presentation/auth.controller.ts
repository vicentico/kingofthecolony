import {
  Body,
  ConflictException,
  Controller,
  Get,
  Post,
  UnauthorizedException,
  UseGuards,
} from '@nestjs/common';
import { ApiBearerAuth, ApiTags } from '@nestjs/swagger';
import { GetCurrentUserUseCase } from '../application/get-current-user.use-case';
import { LoginUseCase } from '../application/login.use-case';
import { LogoutUseCase } from '../application/logout.use-case';
import { RefreshSessionUseCase } from '../application/refresh-session.use-case';
import { RegisterUserUseCase } from '../application/register-user.use-case';
import {
  EmailAlreadyRegisteredError,
  InvalidCredentialsError,
  InvalidRefreshTokenError,
  UserNotFoundError,
} from '../domain/auth.errors';
import { CurrentUser } from './decorators/current-user.decorator';
import { LoginRequestDto } from './dto/login-request.dto';
import { RefreshRequestDto } from './dto/refresh-request.dto';
import { RegisterRequestDto } from './dto/register-request.dto';
import { JwtAccessAuthGuard } from './guards/jwt-access-auth.guard';
import type { RequestUser } from '../application/auth.models';

@ApiTags('auth')
@Controller('auth')
export class AuthController {
  constructor(
    private readonly registerUserUseCase: RegisterUserUseCase,
    private readonly loginUseCase: LoginUseCase,
    private readonly refreshSessionUseCase: RefreshSessionUseCase,
    private readonly logoutUseCase: LogoutUseCase,
    private readonly getCurrentUserUseCase: GetCurrentUserUseCase,
  ) {}

  @Post('register')
  async register(@Body() request: RegisterRequestDto) {
    try {
      return await this.registerUserUseCase.execute(request);
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  @Post('login')
  async login(@Body() request: LoginRequestDto) {
    try {
      return await this.loginUseCase.execute(request);
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  @Post('refresh')
  async refresh(@Body() request: RefreshRequestDto) {
    try {
      return await this.refreshSessionUseCase.execute(request.refreshToken);
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  @ApiBearerAuth()
  @UseGuards(JwtAccessAuthGuard)
  @Post('logout')
  async logout(@CurrentUser() user: RequestUser) {
    try {
      await this.logoutUseCase.execute(user.userId);
      return { message: 'Session closed.' };
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  @ApiBearerAuth()
  @UseGuards(JwtAccessAuthGuard)
  @Get('me')
  async me(@CurrentUser() user: RequestUser) {
    try {
      return await this.getCurrentUserUseCase.execute(user.userId);
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  private rethrowKnownError(error: unknown): never {
    if (error instanceof EmailAlreadyRegisteredError) {
      throw new ConflictException('The email is already registered.');
    }

    if (error instanceof InvalidCredentialsError) {
      throw new UnauthorizedException('Invalid email or password.');
    }

    if (error instanceof InvalidRefreshTokenError) {
      throw new UnauthorizedException('Invalid refresh token.');
    }

    if (error instanceof UserNotFoundError) {
      throw new UnauthorizedException('Authenticated user was not found.');
    }

    throw error;
  }
}