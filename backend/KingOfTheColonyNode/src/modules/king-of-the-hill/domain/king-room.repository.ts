import type { KingRoom, MatchSession } from './king-room.models';

export interface CreateKingRoomCommand {
  name: string;
  kingUserId: string;
}

export interface JoinKingRoomQueueCommand {
  roomId: string;
  userId: string;
  entryCost: number;
  walletDescription: string | null;
}

export interface JoinKingRoomQueueResult {
  room: KingRoom;
  joinedAsChallenger: boolean;
  queuePosition: number | null;
}

export interface CreateMatchSessionCommand {
  roomId: string;
  createdByUserId: string;
  kingUserId: string;
  challengerUserId: string;
  gameRom: string | null;
  launchSource: string | null;
  clientInstanceId: string | null;
}

export interface StartMatchSessionCommand {
  roomId: string;
  matchSessionId: string;
  reporterUserId: string;
  emulatorProcessId: number | null;
  startedAt: Date | null;
}

export interface CompleteMatchSessionCommand {
  roomId: string;
  matchSessionId: string;
  reporterUserId: string;
  winnerUserId: string;
  loserUserId: string;
  resultSource: string | null;
  idempotencyKey: string | null;
  endedAt: Date | null;
}

export interface CompleteMatchSessionResult {
  session: MatchSession;
  room: KingRoom;
  nextChallengerUserId: string | null;
  idempotentReplay: boolean;
}

export interface KingRoomRepository {
  createRoom(command: CreateKingRoomCommand): Promise<KingRoom>;
  findById(roomId: string): Promise<KingRoom | null>;
  listRooms(): Promise<KingRoom[]>;
  joinQueue(command: JoinKingRoomQueueCommand): Promise<JoinKingRoomQueueResult>;
  createMatchSession(command: CreateMatchSessionCommand): Promise<MatchSession>;
  startMatchSession(command: StartMatchSessionCommand): Promise<MatchSession>;
  completeMatchSession(command: CompleteMatchSessionCommand): Promise<CompleteMatchSessionResult>;
}