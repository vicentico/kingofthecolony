import { Module } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { AuthModule } from '../../auth/presentation/auth.module';
import { WalletModule } from '../../wallet/presentation/wallet.module';
import { CompleteMatchSessionUseCase } from '../application/complete-match-session.use-case';
import { CreateMatchSessionUseCase } from '../application/create-match-session.use-case';
import { CreateRoomUseCase } from '../application/create-room.use-case';
import { GetRoomStateUseCase } from '../application/get-room-state.use-case';
import { JoinQueueUseCase } from '../application/join-queue.use-case';
import { KING_ROOM_REPOSITORY } from '../application/king-of-the-hill.tokens';
import { ListRoomsUseCase } from '../application/list-rooms.use-case';
import { StartMatchSessionUseCase } from '../application/start-match-session.use-case';
import { InMemoryKingRoomRepository } from '../infrastructure/in-memory-king-room.repository';
import { PostgresKingRoomRepository } from '../infrastructure/postgres-king-room.repository';
import { KingOfTheHillController } from './king-of-the-hill.controller';

@Module({
  imports: [AuthModule, WalletModule],
  controllers: [KingOfTheHillController],
  providers: [
    InMemoryKingRoomRepository,
    {
      provide: KING_ROOM_REPOSITORY,
      inject: [ConfigService, InMemoryKingRoomRepository],
      useFactory: (
        configService: ConfigService,
        inMemoryKingRoomRepository: InMemoryKingRoomRepository,
      ) => {
        const provider = configService.get<string>('database.provider', 'postgres');
        const postgresUrl = configService.get<string>('database.postgresUrl', '');

        if (provider === 'postgres' && postgresUrl) {
          return new PostgresKingRoomRepository(postgresUrl);
        }

        return inMemoryKingRoomRepository;
      },
    },
    CreateRoomUseCase,
    GetRoomStateUseCase,
    JoinQueueUseCase,
    ListRoomsUseCase,
    CreateMatchSessionUseCase,
    StartMatchSessionUseCase,
    CompleteMatchSessionUseCase,
  ],
})
export class KingOfTheHillModule {}