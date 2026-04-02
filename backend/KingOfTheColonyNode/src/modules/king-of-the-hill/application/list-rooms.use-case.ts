import { Inject, Injectable } from '@nestjs/common';
import { AUTH_USER_REPOSITORY } from '../../auth/application/auth.tokens';
import type { AuthUserRepository } from '../../auth/domain/auth-user.repository';
import { KING_ROOM_REPOSITORY } from './king-of-the-hill.tokens';
import { toKingRoomResponse } from './king-room-response.mapper';
import type { KingRoomRepository } from '../domain/king-room.repository';

@Injectable()
export class ListRoomsUseCase {
  constructor(
    @Inject(KING_ROOM_REPOSITORY) private readonly kingRoomRepository: KingRoomRepository,
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
  ) {}

  async execute() {
    const rooms = await this.kingRoomRepository.listRooms();
    return Promise.all(rooms.map((room) => toKingRoomResponse(room, this.userRepository)));
  }
}