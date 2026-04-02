import type { AuthResponse, AuthenticatedUser } from './auth.models';
import type { AuthTokenPair } from '../domain/auth-token.service';
import type { AuthUser } from '../domain/auth-user';

export function toAuthenticatedUser(user: AuthUser): AuthenticatedUser {
  return {
    id: user.id,
    email: user.email,
    displayName: user.displayName,
    avatarUrl: user.avatarUrl,
    bio: user.bio,
    credits: user.credits,
    roles: user.roles,
    createdAt: user.createdAt.toISOString(),
  };
}

export function toAuthResponse(user: AuthUser, tokenPair: AuthTokenPair): AuthResponse {
  return {
    accessToken: tokenPair.accessToken,
    refreshToken: tokenPair.refreshToken,
    user: toAuthenticatedUser(user),
  };
}