export interface PasswordHasher {
  hash(value: string): Promise<string>;
  compare(rawValue: string, hashedValue: string): Promise<boolean>;
}