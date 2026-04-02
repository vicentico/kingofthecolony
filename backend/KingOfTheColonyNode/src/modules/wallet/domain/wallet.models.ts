export type WalletTransactionType = 'Purchase' | 'GameEntry' | 'Refund' | 'Admin';

export interface WalletTransaction {
  id: string;
  userId: string;
  amount: number;
  type: WalletTransactionType;
  description: string | null;
  createdAt: Date;
}

export interface WalletSnapshot {
  userId: string;
  balance: number;
}

export interface WalletMutationResult {
  balance: number;
  transaction: WalletTransaction;
}