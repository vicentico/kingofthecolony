import { Inject, Injectable } from '@nestjs/common';
import { AUTH_USER_REPOSITORY } from '../../auth/application/auth.tokens';
import type { AuthUserRepository } from '../../auth/domain/auth-user.repository';
import { UserNotFoundError } from '../../auth/domain/auth.errors';
import { KING_ROOM_REPOSITORY } from './king-of-the-hill.tokens';
import { toKingRoomResponse } from './king-room-response.mapper';
import type { KingRoomRepository } from '../domain/king-room.repository';

export interface CreateRoomInput {
  name: string;
  kingUserId: string;
}

@Injectable()
export class CreateRoomUseCase {
  constructor(
    @Inject(KING_ROOM_REPOSITORY) private readonly kingRoomRepository: KingRoomRepository,
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
  ) {}

  async execute(input: CreateRoomInput) {
    const kingUser = await this.userRepository.findById(input.kingUserId);

    if (!kingUser) {
      throw new UserNotFoundError();
    }

    const room = await this.kingRoomRepository.createRoom({
      name: input.name.trim(),
      kingUserId: input.kingUserId,
    });

    return toKingRoomResponse(room, this.userRepository);
  }
}