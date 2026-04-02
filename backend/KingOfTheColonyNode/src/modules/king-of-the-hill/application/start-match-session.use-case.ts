import { Inject, Injectable } from '@nestjs/common';
import { AUTH_USER_REPOSITORY } from '../../auth/application/auth.tokens';
import type { AuthUserRepository } from '../../auth/domain/auth-user.repository';
import { UserNotFoundError } from '../../auth/domain/auth.errors';
import { KING_ROOM_REPOSITORY } from './king-of-the-hill.tokens';
import { toMatchSessionResponse } from './king-room-response.mapper';
import type { KingRoomRepository } from '../domain/king-room.repository';

export interface StartMatchSessionInput {
  roomId: string;
  matchSessionId: string;
  reporterUserId: string;
  emulatorProcessId: number | null;
  startedAt: Date | null;
}

@Injectable()
export class StartMatchSessionUseCase {
  constructor(
    @Inject(KING_ROOM_REPOSITORY) private readonly kingRoomRepository: KingRoomRepository,
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
  ) {}

  async execute(input: StartMatchSessionInput) {
    const reporter = await this.userRepository.findById(input.reporterUserId);

    if (!reporter) {
      throw new UserNotFoundError();
    }

    const session = await this.kingRoomRepository.startMatchSession({
      roomId: input.roomId,
      matchSessionId: input.matchSessionId,
      reporterUserId: input.reporterUserId,
      emulatorProcessId: input.emulatorProcessId,
      startedAt: input.startedAt,
    });

    return toMatchSessionResponse(session, this.userRepository);
  }
}