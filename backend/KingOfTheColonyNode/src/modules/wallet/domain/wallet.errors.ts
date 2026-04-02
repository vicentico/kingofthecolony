export class InsufficientCreditsError extends Error {
  constructor() {
    super('Insufficient credits.');
  }
}