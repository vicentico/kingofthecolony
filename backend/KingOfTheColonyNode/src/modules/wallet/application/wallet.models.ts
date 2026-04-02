import type { WalletTransaction } from '../domain/wallet.models';

export interface WalletResponse {
  userId: string;
  balance: number;
}

export interface WalletMutationResponse extends WalletResponse {
  transaction: WalletTransactionResponse;
}

export interface WalletTransactionResponse {
  id: string;
  userId: string;
  amount: number;
  type: string;
  description: string | null;
  createdAt: string;
}

export function toWalletResponse(userId: string, balance: number): WalletResponse {
  return { userId, balance };
}

export function toWalletTransactionResponse(transaction: WalletTransaction): WalletTransactionResponse {
  return {
    id: transaction.id,
    userId: transaction.userId,
    amount: transaction.amount,
    type: transaction.type,
    description: transaction.description,
    createdAt: transaction.createdAt.toISOString(),
  };
}

export function toWalletMutationResponse(
  userId: string,
  balance: number,
  transaction: WalletTransaction,
): WalletMutationResponse {
  return {
    userId,
    balance,
    transaction: toWalletTransactionResponse(transaction),
  };
}