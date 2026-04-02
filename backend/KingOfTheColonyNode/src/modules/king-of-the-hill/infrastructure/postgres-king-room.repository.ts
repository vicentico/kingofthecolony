import { Injectable } from '@nestjs/common';
import { randomUUID } from 'node:crypto';
import type { Pool, PoolClient } from 'pg';
import { getPostgresPool } from '../../../shared/infrastructure/database/postgres.pool';
import { UserNotFoundError } from '../../auth/domain/auth.errors';
import { InsufficientCreditsError } from '../../wallet/domain/wallet.errors';
import {
  KingRoomConflictError,
  KingRoomNotFoundError,
  MatchSessionNotFoundError,
} from '../domain/king-room.errors';
import type {
  CompleteMatchSessionCommand,
  CompleteMatchSessionResult,
  CreateKingRoomCommand,
  CreateMatchSessionCommand,
  JoinKingRoomQueueCommand,
  JoinKingRoomQueueResult,
  KingRoomRepository,
  StartMatchSessionCommand,
} from '../domain/king-room.repository';
import type {
  KingRoom,
  KingRoomQueueEntry,
  KingRoomStatus,
  MatchSession,
  MatchSessionStatus,
} from '../domain/king-room.models';

interface KingRoomRow {
  id: string;
  name: string;
  status: KingRoomStatus;
  king_user_id: string;
  challenger_user_id: string | null;
  created_at: Date;
  updated_at: Date;
}

interface KingRoomQueueRow {
  id: string;
  room_id: string;
  user_id: string;
  position: number;
  joined_at: Date;
}

interface MatchSessionRow {
  id: string;
  room_id: string;
  status: MatchSessionStatus;
  king_user_id: string;
  challenger_user_id: string;
  created_by_user_id: string;
  reported_by_user_id: string | null;
  winner_user_id: string | null;
  loser_user_id: string | null;
  game_rom: string | null;
  launch_source: string | null;
  client_instance_id: string | null;
  emulator_process_id: number | null;
  result_source: string | null;
  last_idempotency_key: string | null;
  created_at: Date;
  started_at: Date | null;
  ended_at: Date | null;
}

@Injectable()
export class PostgresKingRoomRepository implements KingRoomRepository {
  private readonly pool: Pool;

  constructor(connectionString: string) {
    this.pool = getPostgresPool(connectionString);
  }

  async createRoom(command: CreateKingRoomCommand): Promise<KingRoom> {
    const roomId = randomUUID();
    const result = await this.pool.query<KingRoomRow>(
      `
        INSERT INTO king_rooms (id, name, status, king_user_id)
        VALUES ($1, $2, 'WaitingChallenger', $3)
        RETURNING id, name, status, king_user_id, challenger_user_id, created_at, updated_at
      `,
      [roomId, command.name, command.kingUserId],
    );

    return this.mapRoomRow(result.rows[0], []);
  }

  async findById(roomId: string): Promise<KingRoom | null> {
    const roomResult = await this.pool.query<KingRoomRow>(
      `
        SELECT id, name, status, king_user_id, challenger_user_id, created_at, updated_at
        FROM king_rooms
        WHERE id = $1
        LIMIT 1
      `,
      [roomId],
    );

    const room = roomResult.rows[0];
    if (!room) {
      return null;
    }

    const queue = await this.getQueueEntries(this.pool, [roomId]);
    return this.mapRoomRow(room, queue.get(roomId) ?? []);
  }

  async listRooms(): Promise<KingRoom[]> {
    const roomResult = await this.pool.query<KingRoomRow>(
      `
        SELECT id, name, status, king_user_id, challenger_user_id, created_at, updated_at
        FROM king_rooms
        ORDER BY created_at DESC
      `,
    );

    const roomIds = roomResult.rows.map((row) => row.id);
    const queue = await this.getQueueEntries(this.pool, roomIds);

    return roomResult.rows.map((room) => this.mapRoomRow(room, queue.get(room.id) ?? []));
  }

  async joinQueue(command: JoinKingRoomQueueCommand): Promise<JoinKingRoomQueueResult> {
    const client = await this.pool.connect();

    try {
      await client.query('BEGIN');

      const room = await this.getRoomByIdForUpdate(client, command.roomId);

      if (room.king_user_id === command.userId) {
        throw new KingRoomConflictError('You are already the king of this room.');
      }

      if (room.challenger_user_id === command.userId) {
        throw new KingRoomConflictError('You are already the current challenger.');
      }

      const duplicateQueueEntry = await client.query<{ id: string }>(
        `
          SELECT id
          FROM king_room_queue_entries
          WHERE room_id = $1 AND user_id = $2
          LIMIT 1
        `,
        [command.roomId, command.userId],
      );

      if (duplicateQueueEntry.rows[0]) {
        throw new KingRoomConflictError('You are already in this queue.');
      }

      const userResult = await client.query<{ id: string; credits: number }>(
        `
          SELECT id, credits
          FROM auth_users
          WHERE id = $1
          LIMIT 1
          FOR UPDATE
        `,
        [command.userId],
      );

      const user = userResult.rows[0];
      if (!user) {
        throw new UserNotFoundError();
      }

      if (user.credits < command.entryCost) {
        throw new InsufficientCreditsError();
      }

      await client.query(
        `UPDATE auth_users SET credits = credits - $2, updated_at = now() WHERE id = $1`,
        [command.userId, command.entryCost],
      );

      await client.query(
        `
          INSERT INTO wallet_transactions (id, user_id, amount, type, description)
          VALUES ($1, $2, $3, $4, $5)
        `,
        [
          randomUUID(),
          command.userId,
          -command.entryCost,
          'GameEntry',
          command.walletDescription,
        ],
      );

      let joinedAsChallenger = false;
      let queuePosition: number | null = null;

      if (!room.challenger_user_id) {
        await client.query(
          `
            UPDATE king_rooms
            SET challenger_user_id = $2, status = 'ReadyToPlay', updated_at = now()
            WHERE id = $1
          `,
          [command.roomId, command.userId],
        );
        joinedAsChallenger = true;
      } else {
        const maxPositionResult = await client.query<{ max_position: number | null }>(
          `SELECT MAX(position) AS max_position FROM king_room_queue_entries WHERE room_id = $1`,
          [command.roomId],
        );

        queuePosition = (maxPositionResult.rows[0]?.max_position ?? 0) + 1;

        await client.query(
          `
            INSERT INTO king_room_queue_entries (id, room_id, user_id, position)
            VALUES ($1, $2, $3, $4)
          `,
          [randomUUID(), command.roomId, command.userId, queuePosition],
        );

        await client.query(`UPDATE king_rooms SET updated_at = now() WHERE id = $1`, [command.roomId]);
      }

      const updatedRoom = await this.getRoomByIdForUpdate(client, command.roomId);
      const queue = await this.getQueueEntries(client, [command.roomId]);

      await client.query('COMMIT');

      return {
        room: this.mapRoomRow(updatedRoom, queue.get(command.roomId) ?? []),
        joinedAsChallenger,
        queuePosition,
      };
    } catch (error) {
      await client.query('ROLLBACK');
      throw error;
    } finally {
      client.release();
    }
  }

  async createMatchSession(command: CreateMatchSessionCommand): Promise<MatchSession> {
    const client = await this.pool.connect();

    try {
      await client.query('BEGIN');

      const room = await this.getRoomByIdForUpdate(client, command.roomId);

      if (!room.challenger_user_id) {
        throw new KingRoomConflictError('Room does not have a challenger ready to play.');
      }

      if (room.king_user_id !== command.kingUserId || room.challenger_user_id !== command.challengerUserId) {
        throw new KingRoomConflictError('Room state changed. Refresh and try again.');
      }

      if (command.createdByUserId !== room.king_user_id && command.createdByUserId !== room.challenger_user_id) {
        throw new KingRoomConflictError('Only the king or current challenger can create a match session.');
      }

      const activeSessionResult = await client.query<{ id: string }>(
        `
          SELECT id
          FROM king_match_sessions
          WHERE room_id = $1 AND status = ANY($2::text[])
          LIMIT 1
        `,
        [command.roomId, ['Created', 'Running']],
      );

      if (activeSessionResult.rows[0]) {
        throw new KingRoomConflictError('There is already an active match session for this room.');
      }

      const sessionId = randomUUID();
      const sessionResult = await client.query<MatchSessionRow>(
        `
          INSERT INTO king_match_sessions (
            id,
            room_id,
            status,
            king_user_id,
            challenger_user_id,
            created_by_user_id,
            game_rom,
            launch_source,
            client_instance_id
          )
          VALUES ($1, $2, 'Created', $3, $4, $5, $6, $7, $8)
          RETURNING
            id,
            room_id,
            status,
            king_user_id,
            challenger_user_id,
            created_by_user_id,
            reported_by_user_id,
            winner_user_id,
            loser_user_id,
            game_rom,
            launch_source,
            client_instance_id,
            emulator_process_id,
            result_source,
            last_idempotency_key,
            created_at,
            started_at,
            ended_at
        `,
        [
          sessionId,
          command.roomId,
          room.king_user_id,
          room.challenger_user_id,
          command.createdByUserId,
          command.gameRom,
          command.launchSource,
          command.clientInstanceId,
        ],
      );

      await client.query('COMMIT');
      return this.mapMatchSessionRow(sessionResult.rows[0]);
    } catch (error) {
      await client.query('ROLLBACK');
      throw error;
    } finally {
      client.release();
    }
  }

  async startMatchSession(command: StartMatchSessionCommand): Promise<MatchSession> {
    const client = await this.pool.connect();

    try {
      await client.query('BEGIN');

      const session = await this.getMatchSessionForUpdate(client, command.matchSessionId, command.roomId);

      if (command.reporterUserId !== session.king_user_id && command.reporterUserId !== session.challenger_user_id) {
        throw new KingRoomConflictError('Only an active player can start the match session.');
      }

      if (session.status === 'Completed') {
        throw new KingRoomConflictError('The match session is already closed.');
      }

      const updatedSessionResult = await client.query<MatchSessionRow>(
        `
          UPDATE king_match_sessions
          SET status = 'Running', emulator_process_id = $3, started_at = COALESCE($4, now())
          WHERE id = $1 AND room_id = $2
          RETURNING
            id,
            room_id,
            status,
            king_user_id,
            challenger_user_id,
            created_by_user_id,
            reported_by_user_id,
            winner_user_id,
            loser_user_id,
            game_rom,
            launch_source,
            client_instance_id,
            emulator_process_id,
            result_source,
            last_idempotency_key,
            created_at,
            started_at,
            ended_at
        `,
        [command.matchSessionId, command.roomId, command.emulatorProcessId, command.startedAt],
      );

      await client.query(
        `UPDATE king_rooms SET status = 'Playing', updated_at = now() WHERE id = $1`,
        [command.roomId],
      );

      await client.query('COMMIT');
      return this.mapMatchSessionRow(updatedSessionResult.rows[0]);
    } catch (error) {
      await client.query('ROLLBACK');
      throw error;
    } finally {
      client.release();
    }
  }

  async completeMatchSession(command: CompleteMatchSessionCommand): Promise<CompleteMatchSessionResult> {
    const client = await this.pool.connect();

    try {
      await client.query('BEGIN');

      const session = await this.getMatchSessionForUpdate(client, command.matchSessionId, command.roomId);
      const room = await this.getRoomByIdForUpdate(client, command.roomId);

      if (session.status === 'Completed') {
        const queue = await this.getQueueEntries(client, [command.roomId]);

        if (command.idempotencyKey && session.last_idempotency_key === command.idempotencyKey) {
          await client.query('COMMIT');
          return {
            session: this.mapMatchSessionRow(session),
            room: this.mapRoomRow(room, queue.get(command.roomId) ?? []),
            nextChallengerUserId: room.challenger_user_id,
            idempotentReplay: true,
          };
        }

        throw new KingRoomConflictError('The match session is already closed.');
      }

      if (command.reporterUserId !== session.king_user_id && command.reporterUserId !== session.challenger_user_id) {
        throw new KingRoomConflictError('Only an active player can report the match result.');
      }

      const participants = [session.king_user_id, session.challenger_user_id];
      if (!participants.includes(command.winnerUserId) || !participants.includes(command.loserUserId)) {
        throw new KingRoomConflictError('Winner and loser must be the room participants.');
      }

      if (command.winnerUserId === command.loserUserId) {
        throw new KingRoomConflictError('Winner and loser cannot be the same user.');
      }

      const queueEntriesResult = await client.query<KingRoomQueueRow>(
        `
          SELECT id, room_id, user_id, position, joined_at
          FROM king_room_queue_entries
          WHERE room_id = $1
          ORDER BY position ASC
          FOR UPDATE
        `,
        [command.roomId],
      );

      const nextQueueEntry = queueEntriesResult.rows[0] ?? null;
      const remainingQueueEntries = queueEntriesResult.rows.slice(1);
      const nextChallengerUserId = nextQueueEntry?.user_id ?? null;
      const nextRoomStatus: KingRoomStatus = nextChallengerUserId ? 'ReadyToPlay' : 'WaitingChallenger';

      const updatedSessionResult = await client.query<MatchSessionRow>(
        `
          UPDATE king_match_sessions
          SET
            status = 'Completed',
            reported_by_user_id = $3,
            winner_user_id = $4,
            loser_user_id = $5,
            result_source = $6,
            last_idempotency_key = $7,
            ended_at = COALESCE($8, now())
          WHERE id = $1 AND room_id = $2
          RETURNING
            id,
            room_id,
            status,
            king_user_id,
            challenger_user_id,
            created_by_user_id,
            reported_by_user_id,
            winner_user_id,
            loser_user_id,
            game_rom,
            launch_source,
            client_instance_id,
            emulator_process_id,
            result_source,
            last_idempotency_key,
            created_at,
            started_at,
            ended_at
        `,
        [
          command.matchSessionId,
          command.roomId,
          command.reporterUserId,
          command.winnerUserId,
          command.loserUserId,
          command.resultSource,
          command.idempotencyKey,
          command.endedAt,
        ],
      );

      await client.query(
        `
          INSERT INTO king_match_history (id, room_id, match_session_id, winner_user_id, loser_user_id)
          VALUES ($1, $2, $3, $4, $5)
        `,
        [randomUUID(), command.roomId, command.matchSessionId, command.winnerUserId, command.loserUserId],
      );

      await client.query(
        `
          UPDATE king_rooms
          SET king_user_id = $2, challenger_user_id = $3, status = $4, updated_at = now()
          WHERE id = $1
        `,
        [command.roomId, command.winnerUserId, nextChallengerUserId, nextRoomStatus],
      );

      if (nextQueueEntry) {
        await client.query(`DELETE FROM king_room_queue_entries WHERE id = $1`, [nextQueueEntry.id]);
      }

      for (let index = 0; index < remainingQueueEntries.length; index += 1) {
        await client.query(
          `UPDATE king_room_queue_entries SET position = $2 WHERE id = $1`,
          [remainingQueueEntries[index].id, index + 1],
        );
      }

      const updatedRoom = await this.getRoomByIdForUpdate(client, command.roomId);
      const queue = await this.getQueueEntries(client, [command.roomId]);

      await client.query('COMMIT');
      return {
        session: this.mapMatchSessionRow(updatedSessionResult.rows[0]),
        room: this.mapRoomRow(updatedRoom, queue.get(command.roomId) ?? []),
        nextChallengerUserId,
        idempotentReplay: false,
      };
    } catch (error) {
      await client.query('ROLLBACK');
      throw error;
    } finally {
      client.release();
    }
  }

  private async getRoomByIdForUpdate(client: PoolClient, roomId: string): Promise<KingRoomRow> {
    const result = await client.query<KingRoomRow>(
      `
        SELECT id, name, status, king_user_id, challenger_user_id, created_at, updated_at
        FROM king_rooms
        WHERE id = $1
        LIMIT 1
        FOR UPDATE
      `,
      [roomId],
    );

    if (!result.rows[0]) {
      throw new KingRoomNotFoundError();
    }

    return result.rows[0];
  }

  private async getMatchSessionForUpdate(
    client: PoolClient,
    matchSessionId: string,
    roomId: string,
  ): Promise<MatchSessionRow> {
    const result = await client.query<MatchSessionRow>(
      `
        SELECT
          id,
          room_id,
          status,
          king_user_id,
          challenger_user_id,
          created_by_user_id,
          reported_by_user_id,
          winner_user_id,
          loser_user_id,
          game_rom,
          launch_source,
          client_instance_id,
          emulator_process_id,
          result_source,
          last_idempotency_key,
          created_at,
          started_at,
          ended_at
        FROM king_match_sessions
        WHERE id = $1 AND room_id = $2
        LIMIT 1
        FOR UPDATE
      `,
      [matchSessionId, roomId],
    );

    if (!result.rows[0]) {
      throw new MatchSessionNotFoundError();
    }

    return result.rows[0];
  }

  private async getQueueEntries(
    executor: Pool | PoolClient,
    roomIds: string[],
  ): Promise<Map<string, KingRoomQueueEntry[]>> {
    const queueMap = new Map<string, KingRoomQueueEntry[]>();

    roomIds.forEach((roomId) => {
      queueMap.set(roomId, []);
    });

    if (roomIds.length === 0) {
      return queueMap;
    }

    const result = await executor.query<KingRoomQueueRow>(
      `
        SELECT id, room_id, user_id, position, joined_at
        FROM king_room_queue_entries
        WHERE room_id = ANY($1::uuid[])
        ORDER BY room_id ASC, position ASC
      `,
      [roomIds],
    );

    result.rows.forEach((row) => {
      const entries = queueMap.get(row.room_id) ?? [];
      entries.push({
        id: row.id,
        roomId: row.room_id,
        userId: row.user_id,
        position: row.position,
        joinedAt: new Date(row.joined_at),
      });
      queueMap.set(row.room_id, entries);
    });

    return queueMap;
  }

  private mapRoomRow(row: KingRoomRow, queue: KingRoomQueueEntry[]): KingRoom {
    return {
      id: row.id,
      name: row.name,
      status: row.status,
      kingUserId: row.king_user_id,
      challengerUserId: row.challenger_user_id,
      queue,
      createdAt: new Date(row.created_at),
      updatedAt: new Date(row.updated_at),
    };
  }

  private mapMatchSessionRow(row: MatchSessionRow): MatchSession {
    return {
      id: row.id,
      roomId: row.room_id,
      status: row.status,
      kingUserId: row.king_user_id,
      challengerUserId: row.challenger_user_id,
      createdByUserId: row.created_by_user_id,
      reportedByUserId: row.reported_by_user_id,
      winnerUserId: row.winner_user_id,
      loserUserId: row.loser_user_id,
      gameRom: row.game_rom,
      launchSource: row.launch_source,
      clientInstanceId: row.client_instance_id,
      emulatorProcessId: row.emulator_process_id,
      resultSource: row.result_source,
      lastIdempotencyKey: row.last_idempotency_key,
      createdAt: new Date(row.created_at),
      startedAt: row.started_at ? new Date(row.started_at) : null,
      endedAt: row.ended_at ? new Date(row.ended_at) : null,
    };
  }
}