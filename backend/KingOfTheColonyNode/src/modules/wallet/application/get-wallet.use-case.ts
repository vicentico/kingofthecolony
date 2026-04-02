import { Inject, Injectable } from '@nestjs/common';
import { UserNotFoundError } from '../../auth/domain/auth.errors';
import { WALLET_REPOSITORY } from './wallet.tokens';
import type { WalletRepository } from '../domain/wallet.repository';
import { toWalletResponse } from './wallet.models';

@Injectable()
export class GetWalletUseCase {
  constructor(
    @Inject(WALLET_REPOSITORY) private readonly walletRepository: WalletRepository,
  ) {}

  async execute(userId: string) {
    const wallet = await this.walletRepository.getWallet(userId);
    if (!wallet) {
      throw new UserNotFoundError();
    }

    return toWalletResponse(wallet.userId, wallet.balance);
  }
}