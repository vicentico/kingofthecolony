import { Module } from '@nestjs/common';
import { AuthModule } from '../../auth/presentation/auth.module';
import { GetProfileUseCase } from '../application/get-profile.use-case';
import { UpdateProfileUseCase } from '../application/update-profile.use-case';
import { ProfileController } from './profile.controller';

@Module({
  imports: [AuthModule],
  controllers: [ProfileController],
  providers: [GetProfileUseCase, UpdateProfileUseCase],
})
export class ProfileModule {}