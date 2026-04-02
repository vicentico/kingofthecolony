import { Module } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { AuthModule } from '../../auth/presentation/auth.module';
import { AddCreditsUseCase } from '../application/add-credits.use-case';
import { ConsumeCreditsUseCase } from '../application/consume-credits.use-case';
import { GetWalletUseCase } from '../application/get-wallet.use-case';
import { ListWalletTransactionsUseCase } from '../application/list-wallet-transactions.use-case';
import { WALLET_REPOSITORY } from '../application/wallet.tokens';
import { InMemoryWalletRepository } from '../infrastructure/in-memory-wallet.repository';
import { PostgresWalletRepository } from '../infrastructure/postgres-wallet.repository';
import { WalletController } from './wallet.controller';

@Module({
  imports: [AuthModule],
  controllers: [WalletController],
  providers: [
    InMemoryWalletRepository,
    {
      provide: WALLET_REPOSITORY,
      inject: [ConfigService, InMemoryWalletRepository],
      useFactory: (
        configService: ConfigService,
        inMemoryWalletRepository: InMemoryWalletRepository,
      ) => {
        const provider = configService.get<string>('database.provider', 'postgres');
        const postgresUrl = configService.get<string>('database.postgresUrl', '');

        if (provider === 'postgres' && postgresUrl) {
          return new PostgresWalletRepository(postgresUrl);
        }

        return inMemoryWalletRepository;
      },
    },
    AddCreditsUseCase,
    ConsumeCreditsUseCase,
    GetWalletUseCase,
    ListWalletTransactionsUseCase,
  ],
  exports: [WALLET_REPOSITORY],
})
export class WalletModule {}