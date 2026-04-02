import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import configuration from './config/configuration';
import { AuthModule } from './modules/auth/presentation/auth.module';
import { HealthModule } from './modules/health/presentation/health.module';
import { KingOfTheHillModule } from './modules/king-of-the-hill/presentation/king-of-the-hill.module';
import { ProfileModule } from './modules/profile/presentation/profile.module';
import { WalletModule } from './modules/wallet/presentation/wallet.module';

@Module({
  imports: [
    ConfigModule.forRoot({
      isGlobal: true,
      cache: true,
      load: [configuration],
    }),
    AuthModule,
    HealthModule,
    KingOfTheHillModule,
    ProfileModule,
    WalletModule,
  ],
})
export class AppModule {}