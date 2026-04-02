import { Inject, Injectable } from '@nestjs/common';
import { AUTH_USER_REPOSITORY } from '../../auth/application/auth.tokens';
import type { AuthUserRepository } from '../../auth/domain/auth-user.repository';
import { UserNotFoundError } from '../../auth/domain/auth.errors';
import { KING_ROOM_REPOSITORY } from './king-of-the-hill.tokens';
import { toMatchSessionResponse } from './king-room-response.mapper';
import type { KingRoomRepository } from '../domain/king-room.repository';

export interface CreateMatchSessionInput {
  roomId: string;
  createdByUserId: string;
  kingUserId: string;
  challengerUserId: string;
  gameRom: string | null;
  launchSource: string | null;
  clientInstanceId: string | null;
}

@Injectable()
export class CreateMatchSessionUseCase {
  constructor(
    @Inject(KING_ROOM_REPOSITORY) private readonly kingRoomRepository: KingRoomRepository,
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
  ) {}

  async execute(input: CreateMatchSessionInput) {
    const reporter = await this.userRepository.findById(input.createdByUserId);

    if (!reporter) {
      throw new UserNotFoundError();
    }

    const session = await this.kingRoomRepository.createMatchSession({
      roomId: input.roomId,
      createdByUserId: input.createdByUserId,
      kingUserId: input.kingUserId,
      challengerUserId: input.challengerUserId,
      gameRom: input.gameRom,
      launchSource: input.launchSource,
      clientInstanceId: input.clientInstanceId,
    });

    return toMatchSessionResponse(session, this.userRepository);
  }
}