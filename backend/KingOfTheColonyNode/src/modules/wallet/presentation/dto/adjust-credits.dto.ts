import { IsInt, IsOptional, IsString, MaxLength, Min } from 'class-validator';

export class AdjustCreditsDto {
  @IsInt()
  @Min(1)
  amount!: number;

  @IsOptional()
  @IsString()
  @MaxLength(200)
  description?: string;
}