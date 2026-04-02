import { Injectable } from '@nestjs/common';
import { Pool } from 'pg';
import type { AuthUser } from '../domain/auth-user';
import type { AuthUserRepository } from '../domain/auth-user.repository';
import { getPostgresPool } from '../../../shared/infrastructure/database/postgres.pool';

interface AuthUserRow {
  id: string;
  email: string;
  display_name: string;
  avatar_url: string | null;
  bio: string | null;
  credits: number;
  password_hash: string;
  roles: string[];
  refresh_token_hash: string | null;
  created_at: Date;
  updated_at: Date;
}

@Injectable()
export class PostgresAuthUserRepository implements AuthUserRepository {
  private readonly pool: Pool;

  constructor(connectionString: string) {
    this.pool = getPostgresPool(connectionString);
  }

  async create(user: AuthUser): Promise<AuthUser> {
    const result = await this.pool.query<AuthUserRow>(
      `
        INSERT INTO auth_users (
          id,
          email,
          display_name,
          avatar_url,
          bio,
          credits,
          password_hash,
          roles,
          refresh_token_hash,
          created_at,
          updated_at
        )
        VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
        RETURNING *
      `,
      [
        user.id,
        user.email,
        user.displayName,
        user.avatarUrl,
        user.bio,
        user.credits,
        user.passwordHash,
        user.roles,
        user.refreshTokenHash,
        user.createdAt,
        user.updatedAt,
      ],
    );

    return this.mapRow(result.rows[0]);
  }

  async findByEmail(email: string): Promise<AuthUser | null> {
    const result = await this.pool.query<AuthUserRow>(
      `SELECT * FROM auth_users WHERE email = $1 LIMIT 1`,
      [email],
    );

    return result.rows[0] ? this.mapRow(result.rows[0]) : null;
  }

  async findById(id: string): Promise<AuthUser | null> {
    const result = await this.pool.query<AuthUserRow>(
      `SELECT * FROM auth_users WHERE id = $1 LIMIT 1`,
      [id],
    );

    return result.rows[0] ? this.mapRow(result.rows[0]) : null;
  }

  async update(user: AuthUser): Promise<AuthUser> {
    const result = await this.pool.query<AuthUserRow>(
      `
        UPDATE auth_users
        SET
          email = $2,
          display_name = $3,
          avatar_url = $4,
          bio = $5,
          credits = $6,
          password_hash = $7,
          roles = $8,
          refresh_token_hash = $9,
          updated_at = $10
        WHERE id = $1
        RETURNING *
      `,
      [
        user.id,
        user.email,
        user.displayName,
        user.avatarUrl,
        user.bio,
        user.credits,
        user.passwordHash,
        user.roles,
        user.refreshTokenHash,
        user.updatedAt,
      ],
    );

    return this.mapRow(result.rows[0]);
  }

  private mapRow(row: AuthUserRow): AuthUser {
    return {
      id: row.id,
      email: row.email,
      displayName: row.display_name,
      avatarUrl: row.avatar_url,
      bio: row.bio,
      credits: row.credits,
      passwordHash: row.password_hash,
      roles: row.roles as AuthUser['roles'],
      refreshTokenHash: row.refresh_token_hash,
      createdAt: new Date(row.created_at),
      updatedAt: new Date(row.updated_at),
    };
  }
}