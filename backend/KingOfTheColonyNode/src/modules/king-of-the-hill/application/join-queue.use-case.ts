import { Inject, Injectable } from '@nestjs/common';
import { AUTH_USER_REPOSITORY } from '../../auth/application/auth.tokens';
import type { AuthUserRepository } from '../../auth/domain/auth-user.repository';
import { UserNotFoundError } from '../../auth/domain/auth.errors';
import { KING_ROOM_REPOSITORY } from './king-of-the-hill.tokens';
import { toJoinQueueResponse } from './king-room-response.mapper';
import type { KingRoomRepository } from '../domain/king-room.repository';

export interface JoinQueueInput {
  roomId: string;
  userId: string;
}

@Injectable()
export class JoinQueueUseCase {
  constructor(
    @Inject(KING_ROOM_REPOSITORY) private readonly kingRoomRepository: KingRoomRepository,
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
  ) {}

  async execute(input: JoinQueueInput) {
    const user = await this.userRepository.findById(input.userId);

    if (!user) {
      throw new UserNotFoundError();
    }

    const result = await this.kingRoomRepository.joinQueue({
      roomId: input.roomId,
      userId: input.userId,
      entryCost: 1,
      walletDescription: `Queue entry for room ${input.roomId}`,
    });

    return toJoinQueueResponse(
      result.room,
      this.userRepository,
      result.joinedAsChallenger,
      result.queuePosition,
    );
  }
}