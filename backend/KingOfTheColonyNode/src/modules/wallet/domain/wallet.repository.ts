import type { WalletMutationResult, WalletSnapshot, WalletTransaction, WalletTransactionType } from './wallet.models';

export interface AddCreditsCommand {
  userId: string;
  amount: number;
  type: WalletTransactionType;
  description: string | null;
}

export interface ConsumeCreditsCommand {
  userId: string;
  amount: number;
  type: WalletTransactionType;
  description: string | null;
}

export interface WalletRepository {
  getWallet(userId: string): Promise<WalletSnapshot | null>;
  listTransactions(userId: string, limit: number): Promise<WalletTransaction[]>;
  addCredits(command: AddCreditsCommand): Promise<WalletMutationResult>;
  consumeCredits(command: ConsumeCreditsCommand): Promise<WalletMutationResult>;
}