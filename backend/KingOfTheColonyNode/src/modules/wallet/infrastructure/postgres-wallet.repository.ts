import { Injectable } from '@nestjs/common';
import { randomUUID } from 'node:crypto';
import { Pool } from 'pg';
import { getPostgresPool } from '../../../shared/infrastructure/database/postgres.pool';
import { UserNotFoundError } from '../../auth/domain/auth.errors';
import { InsufficientCreditsError } from '../domain/wallet.errors';
import type {
  AddCreditsCommand,
  ConsumeCreditsCommand,
  WalletRepository,
} from '../domain/wallet.repository';
import type { WalletMutationResult, WalletSnapshot, WalletTransaction } from '../domain/wallet.models';

interface WalletTransactionRow {
  id: string;
  user_id: string;
  amount: number;
  type: WalletTransaction['type'];
  description: string | null;
  created_at: Date;
}

@Injectable()
export class PostgresWalletRepository implements WalletRepository {
  private readonly pool: Pool;

  constructor(connectionString: string) {
    this.pool = getPostgresPool(connectionString);
  }

  async getWallet(userId: string): Promise<WalletSnapshot | null> {
    const result = await this.pool.query<{ id: string; credits: number }>(
      `SELECT id, credits FROM auth_users WHERE id = $1 LIMIT 1`,
      [userId],
    );

    if (!result.rows[0]) {
      return null;
    }

    return {
      userId: result.rows[0].id,
      balance: result.rows[0].credits,
    };
  }

  async listTransactions(userId: string, limit: number): Promise<WalletTransaction[]> {
    const result = await this.pool.query<WalletTransactionRow>(
      `
        SELECT id, user_id, amount, type, description, created_at
        FROM wallet_transactions
        WHERE user_id = $1
        ORDER BY created_at DESC
        LIMIT $2
      `,
      [userId, limit],
    );

    return result.rows.map(this.mapTransactionRow);
  }

  async addCredits(command: AddCreditsCommand): Promise<WalletMutationResult> {
    const client = await this.pool.connect();

    try {
      await client.query('BEGIN');
      const userResult = await client.query<{ credits: number }>(
        `UPDATE auth_users SET credits = credits + $2, updated_at = now() WHERE id = $1 RETURNING credits`,
        [command.userId, command.amount],
      );

      if (!userResult.rows[0]) {
        throw new UserNotFoundError();
      }

      const transactionId = randomUUID();
      const transactionResult = await client.query<WalletTransactionRow>(
        `
          INSERT INTO wallet_transactions (id, user_id, amount, type, description)
          VALUES ($1, $2, $3, $4, $5)
          RETURNING id, user_id, amount, type, description, created_at
        `,
        [transactionId, command.userId, command.amount, command.type, command.description],
      );

      await client.query('COMMIT');

      return {
        balance: userResult.rows[0].credits,
        transaction: this.mapTransactionRow(transactionResult.rows[0]),
      };
    } catch (error) {
      await client.query('ROLLBACK');
      throw error;
    } finally {
      client.release();
    }
  }

  async consumeCredits(command: ConsumeCreditsCommand): Promise<WalletMutationResult> {
    const client = await this.pool.connect();

    try {
      await client.query('BEGIN');
      const userResult = await client.query<{ credits: number }>(
        `
          UPDATE auth_users
          SET credits = credits - $2, updated_at = now()
          WHERE id = $1 AND credits >= $2
          RETURNING credits
        `,
        [command.userId, command.amount],
      );

      if (!userResult.rows[0]) {
        const exists = await client.query<{ id: string }>(
          `SELECT id FROM auth_users WHERE id = $1 LIMIT 1`,
          [command.userId],
        );

        if (!exists.rows[0]) {
          throw new UserNotFoundError();
        }

        throw new InsufficientCreditsError();
      }

      const transactionId = randomUUID();
      const transactionResult = await client.query<WalletTransactionRow>(
        `
          INSERT INTO wallet_transactions (id, user_id, amount, type, description)
          VALUES ($1, $2, $3, $4, $5)
          RETURNING id, user_id, amount, type, description, created_at
        `,
        [transactionId, command.userId, -command.amount, command.type, command.description],
      );

      await client.query('COMMIT');

      return {
        balance: userResult.rows[0].credits,
        transaction: this.mapTransactionRow(transactionResult.rows[0]),
      };
    } catch (error) {
      await client.query('ROLLBACK');
      throw error;
    } finally {
      client.release();
    }
  }

  private mapTransactionRow(row: WalletTransactionRow): WalletTransaction {
    return {
      id: row.id,
      userId: row.user_id,
      amount: row.amount,
      type: row.type,
      description: row.description,
      createdAt: new Date(row.created_at),
    };
  }
}