import {
  Controller,
  Get,
  NotFoundException,
  Param,
  Patch,
  UseGuards,
  Body,
} from '@nestjs/common';
import { ApiBearerAuth, ApiTags } from '@nestjs/swagger';
import { CurrentUser } from '../../auth/presentation/decorators/current-user.decorator';
import { JwtAccessAuthGuard } from '../../auth/presentation/guards/jwt-access-auth.guard';
import { UserNotFoundError } from '../../auth/domain/auth.errors';
import type { RequestUser } from '../../auth/application/auth.models';
import { GetProfileUseCase } from '../application/get-profile.use-case';
import { UpdateProfileUseCase } from '../application/update-profile.use-case';
import { UpdateProfileDto } from './dto/update-profile.dto';

@ApiTags('profile')
@Controller('profile')
export class ProfileController {
  constructor(
    private readonly getProfileUseCase: GetProfileUseCase,
    private readonly updateProfileUseCase: UpdateProfileUseCase,
  ) {}

  @ApiBearerAuth()
  @UseGuards(JwtAccessAuthGuard)
  @Get('me')
  async getCurrentProfile(@CurrentUser() user: RequestUser) {
    try {
      return await this.getProfileUseCase.execute(user.userId);
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  @Get(':id')
  async getProfileById(@Param('id') userId: string) {
    try {
      return await this.getProfileUseCase.execute(userId);
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  @ApiBearerAuth()
  @UseGuards(JwtAccessAuthGuard)
  @Patch('me')
  async updateCurrentProfile(@CurrentUser() user: RequestUser, @Body() request: UpdateProfileDto) {
    try {
      return await this.updateProfileUseCase.execute({
        userId: user.userId,
        displayName: request.displayName,
        avatarUrl: request.avatarUrl,
        bio: request.bio,
      });
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  private rethrowKnownError(error: unknown): never {
    if (error instanceof UserNotFoundError) {
      throw new NotFoundException('Profile not found.');
    }

    throw error;
  }
}