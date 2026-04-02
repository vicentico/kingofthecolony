import { Inject, Injectable } from '@nestjs/common';
import { WALLET_REPOSITORY } from './wallet.tokens';
import type { WalletRepository } from '../domain/wallet.repository';
import { toWalletMutationResponse } from './wallet.models';

export interface ConsumeCreditsInput {
  userId: string;
  amount: number;
  description: string | null;
}

@Injectable()
export class ConsumeCreditsUseCase {
  constructor(
    @Inject(WALLET_REPOSITORY) private readonly walletRepository: WalletRepository,
  ) {}

  async execute(input: ConsumeCreditsInput) {
    const result = await this.walletRepository.consumeCredits({
      userId: input.userId,
      amount: input.amount,
      type: 'GameEntry',
      description: input.description,
    });

    return toWalletMutationResponse(input.userId, result.balance, result.transaction);
  }
}