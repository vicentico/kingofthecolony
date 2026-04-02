import {
  BadRequestException,
  Body,
  Controller,
  Get,
  NotFoundException,
  Param,
  Post,
  UnauthorizedException,
  UseGuards,
} from '@nestjs/common';
import { ApiBearerAuth, ApiTags } from '@nestjs/swagger';
import type { RequestUser } from '../../auth/application/auth.models';
import { UserNotFoundError } from '../../auth/domain/auth.errors';
import { CurrentUser } from '../../auth/presentation/decorators/current-user.decorator';
import { JwtAccessAuthGuard } from '../../auth/presentation/guards/jwt-access-auth.guard';
import { InsufficientCreditsError } from '../../wallet/domain/wallet.errors';
import { CompleteMatchSessionUseCase } from '../application/complete-match-session.use-case';
import { CreateMatchSessionUseCase } from '../application/create-match-session.use-case';
import { CreateRoomUseCase } from '../application/create-room.use-case';
import { GetRoomStateUseCase } from '../application/get-room-state.use-case';
import { JoinQueueUseCase } from '../application/join-queue.use-case';
import { ListRoomsUseCase } from '../application/list-rooms.use-case';
import { StartMatchSessionUseCase } from '../application/start-match-session.use-case';
import {
  KingRoomConflictError,
  KingRoomNotFoundError,
  MatchSessionNotFoundError,
} from '../domain/king-room.errors';
import { CompleteMatchSessionDto } from './dto/complete-match-session.dto';
import { CreateMatchSessionDto } from './dto/create-match-session.dto';
import { CreateRoomDto } from './dto/create-room.dto';
import { StartMatchSessionDto } from './dto/start-match-session.dto';

@ApiTags('king-of-the-hill')
@Controller('king-of-the-hill')
export class KingOfTheHillController {
  constructor(
    private readonly createRoomUseCase: CreateRoomUseCase,
    private readonly listRoomsUseCase: ListRoomsUseCase,
    private readonly getRoomStateUseCase: GetRoomStateUseCase,
    private readonly joinQueueUseCase: JoinQueueUseCase,
    private readonly createMatchSessionUseCase: CreateMatchSessionUseCase,
    private readonly startMatchSessionUseCase: StartMatchSessionUseCase,
    private readonly completeMatchSessionUseCase: CompleteMatchSessionUseCase,
  ) {}

  @Get('rooms')
  async listRooms() {
    return this.listRoomsUseCase.execute();
  }

  @Get('rooms/:roomId')
  async getRoomState(@Param('roomId') roomId: string) {
    try {
      return await this.getRoomStateUseCase.execute(roomId);
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  @ApiBearerAuth()
  @UseGuards(JwtAccessAuthGuard)
  @Post('rooms')
  async createRoom(@CurrentUser() user: RequestUser, @Body() request: CreateRoomDto) {
    try {
      return await this.createRoomUseCase.execute({
        name: request.name,
        kingUserId: user.userId,
      });
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  @ApiBearerAuth()
  @UseGuards(JwtAccessAuthGuard)
  @Post('rooms/:roomId/join-queue')
  async joinQueue(@Param('roomId') roomId: string, @CurrentUser() user: RequestUser) {
    try {
      return await this.joinQueueUseCase.execute({
        roomId,
        userId: user.userId,
      });
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  @ApiBearerAuth()
  @UseGuards(JwtAccessAuthGuard)
  @Post('rooms/:roomId/match-sessions')
  async createMatchSession(
    @Param('roomId') roomId: string,
    @CurrentUser() user: RequestUser,
    @Body() request: CreateMatchSessionDto,
  ) {
    try {
      return await this.createMatchSessionUseCase.execute({
        roomId,
        createdByUserId: user.userId,
        kingUserId: request.kingUserId,
        challengerUserId: request.challengerUserId,
        gameRom: request.gameRom ?? null,
        launchSource: request.launchSource ?? null,
        clientInstanceId: request.clientInstanceId ?? null,
      });
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  @ApiBearerAuth()
  @UseGuards(JwtAccessAuthGuard)
  @Post('rooms/:roomId/match-sessions/:matchSessionId/start')
  async startMatchSession(
    @Param('roomId') roomId: string,
    @Param('matchSessionId') matchSessionId: string,
    @CurrentUser() user: RequestUser,
    @Body() request: StartMatchSessionDto,
  ) {
    try {
      return await this.startMatchSessionUseCase.execute({
        roomId,
        matchSessionId,
        reporterUserId: user.userId,
        emulatorProcessId: request.emulatorProcessId ?? null,
        startedAt: request.startedAt ? new Date(request.startedAt) : null,
      });
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  @ApiBearerAuth()
  @UseGuards(JwtAccessAuthGuard)
  @Post('rooms/:roomId/match-sessions/:matchSessionId/complete')
  async completeMatchSession(
    @Param('roomId') roomId: string,
    @Param('matchSessionId') matchSessionId: string,
    @CurrentUser() user: RequestUser,
    @Body() request: CompleteMatchSessionDto,
  ) {
    try {
      return await this.completeMatchSessionUseCase.execute({
        roomId,
        matchSessionId,
        reporterUserId: user.userId,
        winnerUserId: request.winnerUserId,
        loserUserId: request.loserUserId,
        resultSource: request.resultSource ?? null,
        idempotencyKey: request.idempotencyKey ?? null,
        endedAt: request.endedAt ? new Date(request.endedAt) : null,
      });
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  private rethrowKnownError(error: unknown): never {
    if (error instanceof KingRoomNotFoundError) {
      throw new NotFoundException('Room not found.');
    }

    if (error instanceof MatchSessionNotFoundError) {
      throw new NotFoundException('Match session not found.');
    }

    if (error instanceof UserNotFoundError) {
      throw new UnauthorizedException('Authenticated user was not found.');
    }

    if (error instanceof InsufficientCreditsError) {
      throw new BadRequestException('Insufficient credits.');
    }

    if (error instanceof KingRoomConflictError) {
      throw new BadRequestException(error.message);
    }

    throw error;
  }
}