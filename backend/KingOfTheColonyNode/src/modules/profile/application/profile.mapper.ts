import type { AuthenticatedUser } from '../../auth/application/auth.models';
import type { AuthUser } from '../../auth/domain/auth-user';

export function toProfileResponse(user: AuthUser): AuthenticatedUser {
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