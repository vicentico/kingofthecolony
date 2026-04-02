import { Injectable } from '@nestjs/common';
import { compare, hash } from 'bcryptjs';
import type { PasswordHasher } from '../domain/password-hasher';

@Injectable()
export class BcryptPasswordHasher implements PasswordHasher {
  async hash(value: string): Promise<string> {
    return hash(value, 12);
  }

  async compare(rawValue: string, hashedValue: string): Promise<boolean> {
    return compare(rawValue, hashedValue);
  }
}