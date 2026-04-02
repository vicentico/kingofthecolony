import { IsDateString, IsInt, IsOptional, Min } from 'class-validator';

export class StartMatchSessionDto {
  @IsOptional()
  @IsInt()
  @Min(1)
  emulatorProcessId?: number;

  @IsOptional()
  @IsDateString()
  startedAt?: string;
}