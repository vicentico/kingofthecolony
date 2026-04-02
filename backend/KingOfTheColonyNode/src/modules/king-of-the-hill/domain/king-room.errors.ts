export class KingRoomNotFoundError extends Error {
  constructor() {
    super('Room not found.');
  }
}

export class KingRoomConflictError extends Error {
  constructor(message: string) {
    super(message);
  }
}

export class MatchSessionNotFoundError extends Error {
  constructor() {
    super('Match session not found.');
  }
}