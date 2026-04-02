import { UserNotFoundError } from '../../auth/domain/auth.errors';
import type { AuthUserRepository } from '../../auth/domain/auth-user.repository';
import type {
  JoinQueueResponse,
  KingRoom,
  KingRoomQueueEntryResponse,
  KingRoomResponse,
  KingRoomUserSummary,
  MatchSession,
  MatchSessionCompletionResponse,
  MatchSessionResponse,
} from '../domain/king-room.models';

export async function toKingRoomResponse(
  room: KingRoom,
  userRepository: AuthUserRepository,
): Promise<KingRoomResponse> {
  const userMap = await buildUserSummaryMap(
    [
      room.kingUserId,
      ...(room.challengerUserId ? [room.challengerUserId] : []),
      ...room.queue.map((entry) => entry.userId),
    ],
    userRepository,
  );

  return {
    roomId: room.id,
    name: room.name,
    status: room.status,
    king: getUserSummary(userMap, room.kingUserId),
    challenger: room.challengerUserId ? getUserSummary(userMap, room.challengerUserId) : null,
    queue: room.queue.map<KingRoomQueueEntryResponse>((entry) => ({
      position: entry.position,
      joinedAt: entry.joinedAt.toISOString(),
      user: getUserSummary(userMap, entry.userId),
    })),
    createdAt: room.createdAt.toISOString(),
    updatedAt: room.updatedAt.toISOString(),
  };
}

export async function toJoinQueueResponse(
  room: KingRoom,
  userRepository: AuthUserRepository,
  joinedAsChallenger: boolean,
  queuePosition: number | null,
): Promise<JoinQueueResponse> {
  return {
    message: joinedAsChallenger
      ? 'You are now the current challenger.'
      : `You joined the queue in position ${queuePosition}.`,
    joinedAsChallenger,
    queuePosition,
    room: await toKingRoomResponse(room, userRepository),
  };
}

export async function toMatchSessionResponse(
  session: MatchSession,
  userRepository: AuthUserRepository,
): Promise<MatchSessionResponse> {
  const userMap = await buildUserSummaryMap(
    [
      session.kingUserId,
      session.challengerUserId,
      ...(session.winnerUserId ? [session.winnerUserId] : []),
      ...(session.loserUserId ? [session.loserUserId] : []),
    ],
    userRepository,
  );

  return {
    matchSessionId: session.id,
    roomId: session.roomId,
    status: session.status,
    gameRom: session.gameRom,
    launchSource: session.launchSource,
    clientInstanceId: session.clientInstanceId,
    emulatorProcessId: session.emulatorProcessId,
    king: getUserSummary(userMap, session.kingUserId),
    challenger: getUserSummary(userMap, session.challengerUserId),
    winner: session.winnerUserId ? getUserSummary(userMap, session.winnerUserId) : null,
    loser: session.loserUserId ? getUserSummary(userMap, session.loserUserId) : null,
    createdAt: session.createdAt.toISOString(),
    startedAt: session.startedAt?.toISOString() ?? null,
    endedAt: session.endedAt?.toISOString() ?? null,
  };
}

export async function toMatchSessionCompletionResponse(
  session: MatchSession,
  room: KingRoom,
  userRepository: AuthUserRepository,
  nextChallengerUserId: string | null,
  idempotentReplay: boolean,
): Promise<MatchSessionCompletionResponse> {
  const nextChallenger = nextChallengerUserId
    ? getUserSummary(
        await buildUserSummaryMap([nextChallengerUserId], userRepository),
        nextChallengerUserId,
      )
    : null;

  return {
    matchSession: await toMatchSessionResponse(session, userRepository),
    room: await toKingRoomResponse(room, userRepository),
    nextChallenger,
    idempotentReplay,
  };
}

async function buildUserSummaryMap(
  userIds: string[],
  userRepository: AuthUserRepository,
): Promise<Map<string, KingRoomUserSummary>> {
  const uniqueUserIds = [...new Set(userIds)];
  const users = await Promise.all(uniqueUserIds.map((userId) => userRepository.findById(userId)));
  const userMap = new Map<string, KingRoomUserSummary>();

  users.forEach((user, index) => {
    if (!user) {
      throw new UserNotFoundError();
    }

    userMap.set(uniqueUserIds[index], {
      id: user.id,
      displayName: user.displayName,
      avatarUrl: user.avatarUrl,
      credits: user.credits,
    });
  });

  return userMap;
}

function getUserSummary(
  userMap: Map<string, KingRoomUserSummary>,
  userId: string,
): KingRoomUserSummary {
  const user = userMap.get(userId);

  if (!user) {
    throw new UserNotFoundError();
  }

  return user;
}