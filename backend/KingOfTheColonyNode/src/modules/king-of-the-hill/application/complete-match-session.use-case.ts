import { Inject, Injectable } from '@nestjs/common';
import { AUTH_USER_REPOSITORY } from '../../auth/application/auth.tokens';
import type { AuthUserRepository } from '../../auth/domain/auth-user.repository';
import { UserNotFoundError } from '../../auth/domain/auth.errors';
import { KING_ROOM_REPOSITORY } from './king-of-the-hill.tokens';
import { toMatchSessionCompletionResponse } from './king-room-response.mapper';
import type { KingRoomRepository } from '../domain/king-room.repository';

export interface CompleteMatchSessionInput {
  roomId: string;
  matchSessionId: string;
  reporterUserId: string;
  winnerUserId: string;
  loserUserId: string;
  resultSource: string | null;
  idempotencyKey: string | null;
  endedAt: Date | null;
}

@Injectable()
export class CompleteMatchSessionUseCase {
  constructor(
    @Inject(KING_ROOM_REPOSITORY) private readonly kingRoomRepository: KingRoomRepository,
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
  ) {}

  async execute(input: CompleteMatchSessionInput) {
    const reporter = await this.userRepository.findById(input.reporterUserId);

    if (!reporter) {
      throw new UserNotFoundError();
    }

    const result = await this.kingRoomRepository.completeMatchSession({
      roomId: input.roomId,
      matchSessionId: input.matchSessionId,
      reporterUserId: input.reporterUserId,
      winnerUserId: input.winnerUserId,
      loserUserId: input.loserUserId,
      resultSource: input.resultSource,
      idempotencyKey: input.idempotencyKey,
      endedAt: input.endedAt,
    });

    return toMatchSessionCompletionResponse(
      result.session,
      result.room,
      this.userRepository,
      result.nextChallengerUserId,
      result.idempotentReplay,
    );
  }
}