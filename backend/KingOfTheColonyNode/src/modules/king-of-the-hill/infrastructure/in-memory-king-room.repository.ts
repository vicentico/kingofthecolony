import { Inject, Injectable } from '@nestjs/common';
import { randomUUID } from 'node:crypto';
import { AUTH_USER_REPOSITORY } from '../../auth/application/auth.tokens';
import { UserNotFoundError } from '../../auth/domain/auth.errors';
import type { AuthUserRepository } from '../../auth/domain/auth-user.repository';
import { WALLET_REPOSITORY } from '../../wallet/application/wallet.tokens';
import type { WalletRepository } from '../../wallet/domain/wallet.repository';
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
import type { KingRoom, MatchSession, MatchSessionStatus } from '../domain/king-room.models';

@Injectable()
export class InMemoryKingRoomRepository implements KingRoomRepository {
  private readonly rooms = new Map<string, KingRoom>();
  private readonly sessions = new Map<string, MatchSession>();

  constructor(
    @Inject(AUTH_USER_REPOSITORY) private readonly userRepository: AuthUserRepository,
    @Inject(WALLET_REPOSITORY) private readonly walletRepository: WalletRepository,
  ) {}

  async createRoom(command: CreateKingRoomCommand): Promise<KingRoom> {
    const user = await this.userRepository.findById(command.kingUserId);

    if (!user) {
      throw new UserNotFoundError();
    }

    const now = new Date();
    const room: KingRoom = {
      id: randomUUID(),
      name: command.name,
      status: 'WaitingChallenger',
      kingUserId: command.kingUserId,
      challengerUserId: null,
      queue: [],
      createdAt: now,
      updatedAt: now,
    };

    this.rooms.set(room.id, room);
    return this.cloneRoom(room);
  }

  async findById(roomId: string): Promise<KingRoom | null> {
    const room = this.rooms.get(roomId);
    return room ? this.cloneRoom(room) : null;
  }

  async listRooms(): Promise<KingRoom[]> {
    return [...this.rooms.values()]
      .sort((left, right) => right.createdAt.getTime() - left.createdAt.getTime())
      .map((room) => this.cloneRoom(room));
  }

  async joinQueue(command: JoinKingRoomQueueCommand): Promise<JoinKingRoomQueueResult> {
    const room = this.rooms.get(command.roomId);

    if (!room) {
      throw new KingRoomNotFoundError();
    }

    if (room.kingUserId === command.userId) {
      throw new KingRoomConflictError('You are already the king of this room.');
    }

    if (room.challengerUserId === command.userId) {
      throw new KingRoomConflictError('You are already the current challenger.');
    }

    if (room.queue.some((entry) => entry.userId === command.userId)) {
      throw new KingRoomConflictError('You are already in this queue.');
    }

    await this.walletRepository.consumeCredits({
      userId: command.userId,
      amount: command.entryCost,
      type: 'GameEntry',
      description: command.walletDescription,
    });

    const now = new Date();
    let joinedAsChallenger = false;
    let queuePosition: number | null = null;

    if (!room.challengerUserId) {
      room.challengerUserId = command.userId;
      room.status = 'ReadyToPlay';
      joinedAsChallenger = true;
    } else {
      queuePosition = room.queue.length + 1;
      room.queue.push({
        id: randomUUID(),
        roomId: room.id,
        userId: command.userId,
        position: queuePosition,
        joinedAt: now,
      });
    }

    room.updatedAt = now;
    this.rooms.set(room.id, room);

    return {
      room: this.cloneRoom(room),
      joinedAsChallenger,
      queuePosition,
    };
  }

  async createMatchSession(command: CreateMatchSessionCommand): Promise<MatchSession> {
    const room = this.rooms.get(command.roomId);

    if (!room) {
      throw new KingRoomNotFoundError();
    }

    if (!room.challengerUserId) {
      throw new KingRoomConflictError('Room does not have a challenger ready to play.');
    }

    if (room.kingUserId !== command.kingUserId || room.challengerUserId !== command.challengerUserId) {
      throw new KingRoomConflictError('Room state changed. Refresh and try again.');
    }

    if (command.createdByUserId !== room.kingUserId && command.createdByUserId !== room.challengerUserId) {
      throw new KingRoomConflictError('Only the king or current challenger can create a match session.');
    }

    if (this.findActiveRoomSession(room.id)) {
      throw new KingRoomConflictError('There is already an active match session for this room.');
    }

    const session: MatchSession = {
      id: randomUUID(),
      roomId: room.id,
      status: 'Created',
      kingUserId: room.kingUserId,
      challengerUserId: room.challengerUserId,
      createdByUserId: command.createdByUserId,
      reportedByUserId: null,
      winnerUserId: null,
      loserUserId: null,
      gameRom: command.gameRom,
      launchSource: command.launchSource,
      clientInstanceId: command.clientInstanceId,
      emulatorProcessId: null,
      resultSource: null,
      lastIdempotencyKey: null,
      createdAt: new Date(),
      startedAt: null,
      endedAt: null,
    };

    this.sessions.set(session.id, session);
    return this.cloneSession(session);
  }

  async startMatchSession(command: StartMatchSessionCommand): Promise<MatchSession> {
    const session = this.sessions.get(command.matchSessionId);

    if (!session || session.roomId !== command.roomId) {
      throw new MatchSessionNotFoundError();
    }

    if (command.reporterUserId !== session.kingUserId && command.reporterUserId !== session.challengerUserId) {
      throw new KingRoomConflictError('Only an active player can start the match session.');
    }

    if (session.status === 'Completed') {
      throw new KingRoomConflictError('The match session is already closed.');
    }

    session.status = 'Running';
    session.emulatorProcessId = command.emulatorProcessId;
    session.startedAt = command.startedAt ?? new Date();
    this.sessions.set(session.id, session);

    const room = this.rooms.get(command.roomId);
    if (!room) {
      throw new KingRoomNotFoundError();
    }

    room.status = 'Playing';
    room.updatedAt = new Date();
    this.rooms.set(room.id, room);

    return this.cloneSession(session);
  }

  async completeMatchSession(command: CompleteMatchSessionCommand): Promise<CompleteMatchSessionResult> {
    const session = this.sessions.get(command.matchSessionId);

    if (!session || session.roomId !== command.roomId) {
      throw new MatchSessionNotFoundError();
    }

    const room = this.rooms.get(command.roomId);
    if (!room) {
      throw new KingRoomNotFoundError();
    }

    if (session.status === 'Completed') {
      if (command.idempotencyKey && session.lastIdempotencyKey === command.idempotencyKey) {
        return {
          session: this.cloneSession(session),
          room: this.cloneRoom(room),
          nextChallengerUserId: room.challengerUserId,
          idempotentReplay: true,
        };
      }

      throw new KingRoomConflictError('The match session is already closed.');
    }

    if (command.reporterUserId !== session.kingUserId && command.reporterUserId !== session.challengerUserId) {
      throw new KingRoomConflictError('Only an active player can report the match result.');
    }

    const participants = [session.kingUserId, session.challengerUserId];
    if (!participants.includes(command.winnerUserId) || !participants.includes(command.loserUserId)) {
      throw new KingRoomConflictError('Winner and loser must be the room participants.');
    }

    if (command.winnerUserId === command.loserUserId) {
      throw new KingRoomConflictError('Winner and loser cannot be the same user.');
    }

    session.status = 'Completed';
    session.winnerUserId = command.winnerUserId;
    session.loserUserId = command.loserUserId;
    session.reportedByUserId = command.reporterUserId;
    session.resultSource = command.resultSource;
    session.lastIdempotencyKey = command.idempotencyKey;
    session.endedAt = command.endedAt ?? new Date();
    this.sessions.set(session.id, session);

    room.kingUserId = command.winnerUserId;
    let nextChallengerUserId: string | null = null;

    if (room.queue[0]) {
      const sortedQueue = [...room.queue].sort((left, right) => left.position - right.position);
      const [nextEntry, ...remainingEntries] = sortedQueue;
      room.challengerUserId = nextEntry.userId;
      nextChallengerUserId = nextEntry.userId;
      room.queue = remainingEntries.map((entry, index) => ({
        ...entry,
        position: index + 1,
      }));
      room.status = 'ReadyToPlay';
    } else {
      room.challengerUserId = null;
      room.queue = [];
      room.status = 'WaitingChallenger';
    }

    room.updatedAt = new Date();
    this.rooms.set(room.id, room);

    return {
      session: this.cloneSession(session),
      room: this.cloneRoom(room),
      nextChallengerUserId,
      idempotentReplay: false,
    };
  }

  private cloneRoom(room: KingRoom): KingRoom {
    return {
      ...room,
      queue: room.queue
        .map((entry) => ({
          ...entry,
          joinedAt: new Date(entry.joinedAt),
        }))
        .sort((left, right) => left.position - right.position),
      createdAt: new Date(room.createdAt),
      updatedAt: new Date(room.updatedAt),
    };
  }

  private cloneSession(session: MatchSession): MatchSession {
    return {
      ...session,
      createdAt: new Date(session.createdAt),
      startedAt: session.startedAt ? new Date(session.startedAt) : null,
      endedAt: session.endedAt ? new Date(session.endedAt) : null,
    };
  }

  private findActiveRoomSession(roomId: string): MatchSession | undefined {
    return [...this.sessions.values()].find(
      (session) => session.roomId === roomId && isActiveSessionStatus(session.status),
    );
  }
}

function isActiveSessionStatus(status: MatchSessionStatus) {
  return status === 'Created' || status === 'Running';
}