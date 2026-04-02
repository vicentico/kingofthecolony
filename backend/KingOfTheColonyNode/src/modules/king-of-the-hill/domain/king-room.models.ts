export type KingRoomStatus = 'WaitingChallenger' | 'ReadyToPlay' | 'Playing';

export type MatchSessionStatus = 'Created' | 'Running' | 'Completed';

export interface KingRoomQueueEntry {
  id: string;
  roomId: string;
  userId: string;
  position: number;
  joinedAt: Date;
}

export interface KingRoom {
  id: string;
  name: string;
  status: KingRoomStatus;
  kingUserId: string;
  challengerUserId: string | null;
  queue: KingRoomQueueEntry[];
  createdAt: Date;
  updatedAt: Date;
}

export interface MatchSession {
  id: string;
  roomId: string;
  status: MatchSessionStatus;
  kingUserId: string;
  challengerUserId: string;
  createdByUserId: string;
  reportedByUserId: string | null;
  winnerUserId: string | null;
  loserUserId: string | null;
  gameRom: string | null;
  launchSource: string | null;
  clientInstanceId: string | null;
  emulatorProcessId: number | null;
  resultSource: string | null;
  lastIdempotencyKey: string | null;
  createdAt: Date;
  startedAt: Date | null;
  endedAt: Date | null;
}

export interface KingRoomUserSummary {
  id: string;
  displayName: string;
  avatarUrl: string | null;
  credits: number;
}

export interface KingRoomQueueEntryResponse {
  position: number;
  joinedAt: string;
  user: KingRoomUserSummary;
}

export interface KingRoomResponse {
  roomId: string;
  name: string;
  status: KingRoomStatus;
  king: KingRoomUserSummary;
  challenger: KingRoomUserSummary | null;
  queue: KingRoomQueueEntryResponse[];
  createdAt: string;
  updatedAt: string;
}

export interface JoinQueueResponse {
  message: string;
  joinedAsChallenger: boolean;
  queuePosition: number | null;
  room: KingRoomResponse;
}

export interface MatchSessionResponse {
  matchSessionId: string;
  roomId: string;
  status: MatchSessionStatus;
  gameRom: string | null;
  launchSource: string | null;
  clientInstanceId: string | null;
  emulatorProcessId: number | null;
  king: KingRoomUserSummary;
  challenger: KingRoomUserSummary;
  winner: KingRoomUserSummary | null;
  loser: KingRoomUserSummary | null;
  createdAt: string;
  startedAt: string | null;
  endedAt: string | null;
}

export interface MatchSessionCompletionResponse {
  matchSession: MatchSessionResponse;
  room: KingRoomResponse;
  nextChallenger: KingRoomUserSummary | null;
  idempotentReplay: boolean;
}