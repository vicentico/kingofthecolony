import { IsString, MinLength } from 'class-validator';

export class RefreshRequestDto {
  @IsString()
  @MinLength(16)
  refreshToken!: string;
}