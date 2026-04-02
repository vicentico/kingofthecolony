import { IsDateString, IsOptional, IsString, IsUUID, MaxLength } from 'class-validator';

export class CompleteMatchSessionDto {
  @IsUUID()
  winnerUserId!: string;

  @IsUUID()
  loserUserId!: string;

  @IsOptional()
  @IsString()
  @MaxLength(100)
  resultSource?: string;

  @IsOptional()
  @IsString()
  @MaxLength(120)
  idempotencyKey?: string;

  @IsOptional()
  @IsDateString()
  endedAt?: string;
}