export class EmailAlreadyRegisteredError extends Error {
  constructor(email: string) {
    super(`The email '${email}' is already registered.`);
  }
}

export class InvalidCredentialsError extends Error {
  constructor() {
    super('Invalid credentials.');
  }
}

export class InvalidRefreshTokenError extends Error {
  constructor() {
    super('Invalid refresh token.');
  }
}

export class UserNotFoundError extends Error {
  constructor() {
    super('User not found.');
  }
}