import { Inject, Injectable } from '@nestjs/common';
import { WALLET_REPOSITORY } from './wallet.tokens';
import type { WalletRepository } from '../domain/wallet.repository';
import { toWalletMutationResponse } from './wallet.models';

export interface AddCreditsInput {
  userId: string;
  amount: number;
  description: string | null;
}

@Injectable()
export class AddCreditsUseCase {
  constructor(
    @Inject(WALLET_REPOSITORY) private readonly walletRepository: WalletRepository,
  ) {}

  async execute(input: AddCreditsInput) {
    const result = await this.walletRepository.addCredits({
      userId: input.userId,
      amount: input.amount,
      type: 'Purchase',
      description: input.description,
    });

    return toWalletMutationResponse(input.userId, result.balance, result.transaction);
  }
}