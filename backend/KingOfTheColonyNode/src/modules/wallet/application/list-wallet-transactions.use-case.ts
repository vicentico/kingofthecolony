import { Inject, Injectable } from '@nestjs/common';
import { WALLET_REPOSITORY } from './wallet.tokens';
import type { WalletRepository } from '../domain/wallet.repository';
import { toWalletTransactionResponse } from './wallet.models';

@Injectable()
export class ListWalletTransactionsUseCase {
  constructor(
    @Inject(WALLET_REPOSITORY) private readonly walletRepository: WalletRepository,
  ) {}

  async execute(userId: string, limit = 20) {
    const transactions = await this.walletRepository.listTransactions(userId, limit);
    return transactions.map(toWalletTransactionResponse);
  }
}