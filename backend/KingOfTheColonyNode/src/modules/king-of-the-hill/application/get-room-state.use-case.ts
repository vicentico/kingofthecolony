import { Inject, Injectable } from '@nestjs/common';
import { AUTH_USER_REPOSITORY } from '../../auth/application/auth.tokens';
import type { AuthUserRepository } from '../../auth/domain/auth-user.repository';
import { KING_ROOM_REPOSITORY } from './king-of-the-hill.tokens';
import { toKingRoomResponse } from './king-room-response.mapper';
import { KingRoomNotFoundError } from '../domain/king-room.errors';
import type { KingRoomRepository } from '../domain/king-room.repository';

@Injectable()
export class GetRoomStateUseCase {
  constructor(
    @Inject(KING_ROOM_REPOSITORY) private readonly kingRoomRepository: KingRoomRepository,
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
  ) {}

  async execute(roomId: string) {
    const room = await this.kingRoomRepository.findById(roomId);

    if (!room) {
      throw new KingRoomNotFoundError();
    }

    return toKingRoomResponse(room, this.userRepository);
  }
}