import { createParamDecorator, ExecutionContext } from '@nestjs/common';
import type { RequestUser } from '../../application/auth.models';

export const CurrentUser = createParamDecorator(
  (_data: unknown, context: ExecutionContext): RequestUser => {
    const request = context.switchToHttp().getRequest<{ user: RequestUser }>();
    return request.user;
  },
);