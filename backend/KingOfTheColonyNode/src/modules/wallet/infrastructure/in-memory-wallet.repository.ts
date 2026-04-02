import { Injectable, Inject } from '@nestjs/common';
import { randomUUID } from 'node:crypto';
import { AUTH_USER_REPOSITORY } from '../../auth/application/auth.tokens';
import type { AuthUserRepository } from '../../auth/domain/auth-user.repository';
import { UserNotFoundError } from '../../auth/domain/auth.errors';
import { InsufficientCreditsError } from '../domain/wallet.errors';
import type {
  AddCreditsCommand,
  ConsumeCreditsCommand,
  WalletRepository,
} from '../domain/wallet.repository';
import type { WalletMutationResult, WalletSnapshot, WalletTransaction } from '../domain/wallet.models';

@Injectable()
export class InMemoryWalletRepository implements WalletRepository {
  private readonly transactionsByUser = new Map<string, WalletTransaction[]>();

  constructor(
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
  ) {}

  async getWallet(userId: string): Promise<WalletSnapshot | null> {
    const user = await this.userRepository.findById(userId);
    if (!user) {
      return null;
    }

    return { userId, balance: user.credits };
  }

  async listTransactions(userId: string, limit: number): Promise<WalletTransaction[]> {
    const transactions = this.transactionsByUser.get(userId) ?? [];
    return transactions.slice(0, limit).map((transaction) => ({ ...transaction }));
  }

  async addCredits(command: AddCreditsCommand): Promise<WalletMutationResult> {
    const user = await this.userRepository.findById(command.userId);
    if (!user) {
      throw new UserNotFoundError();
    }

    user.credits += command.amount;
    user.updatedAt = new Date();
    await this.userRepository.update(user);

    const transaction = this.createTransaction(command.userId, command.amount, command.type, command.description);
    this.pushTransaction(command.userId, transaction);

    return { balance: user.credits, transaction };
  }

  async consumeCredits(command: ConsumeCreditsCommand): Promise<WalletMutationResult> {
    const user = await this.userRepository.findById(command.userId);
    if (!user) {
      throw new UserNotFoundError();
    }

    if (user.credits < command.amount) {
      throw new InsufficientCreditsError();
    }

    user.credits -= command.amount;
    user.updatedAt = new Date();
    await this.userRepository.update(user);

    const transaction = this.createTransaction(command.userId, -command.amount, command.type, command.description);
    this.pushTransaction(command.userId, transaction);

    return { balance: user.credits, transaction };
  }

  private createTransaction(
    userId: string,
    amount: number,
    type: WalletTransaction['type'],
    description: string | null,
  ): WalletTransaction {
    return {
      id: randomUUID(),
      userId,
      amount,
      type,
      description,
      createdAt: new Date(),
    };
  }

  private pushTransaction(userId: string, transaction: WalletTransaction) {
    const transactions = this.transactionsByUser.get(userId) ?? [];
    this.transactionsByUser.set(userId, [transaction, ...transactions]);
  }
}