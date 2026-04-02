import { IsOptional, IsString, IsUUID, MaxLength } from 'class-validator';

export class CreateMatchSessionDto {
  @IsUUID()
  kingUserId!: string;

  @IsUUID()
  challengerUserId!: string;

  @IsOptional()
  @IsString()
  @MaxLength(100)
  gameRom?: string;

  @IsOptional()
  @IsString()
  @MaxLength(100)
  launchSource?: string;

  @IsOptional()
  @IsString()
  @MaxLength(120)
  clientInstanceId?: string;
}