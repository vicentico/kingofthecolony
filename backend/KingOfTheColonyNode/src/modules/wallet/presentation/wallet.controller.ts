import {
  BadRequestException,
  Body,
  Controller,
  Get,
  Query,
  Post,
  UnauthorizedException,
  UseGuards,
} from '@nestjs/common';
import { ApiBearerAuth, ApiTags } from '@nestjs/swagger';
import type { RequestUser } from '../../auth/application/auth.models';
import { UserNotFoundError } from '../../auth/domain/auth.errors';
import { CurrentUser } from '../../auth/presentation/decorators/current-user.decorator';
import { JwtAccessAuthGuard } from '../../auth/presentation/guards/jwt-access-auth.guard';
import { AddCreditsUseCase } from '../application/add-credits.use-case';
import { ConsumeCreditsUseCase } from '../application/consume-credits.use-case';
import { GetWalletUseCase } from '../application/get-wallet.use-case';
import { ListWalletTransactionsUseCase } from '../application/list-wallet-transactions.use-case';
import { InsufficientCreditsError } from '../domain/wallet.errors';
import { AdjustCreditsDto } from './dto/adjust-credits.dto';

@ApiTags('wallet')
@ApiBearerAuth()
@UseGuards(JwtAccessAuthGuard)
@Controller('wallet')
export class WalletController {
  constructor(
    private readonly getWalletUseCase: GetWalletUseCase,
    private readonly listWalletTransactionsUseCase: ListWalletTransactionsUseCase,
    private readonly addCreditsUseCase: AddCreditsUseCase,
    private readonly consumeCreditsUseCase: ConsumeCreditsUseCase,
  ) {}

  @Get('me')
  async getCurrentWallet(@CurrentUser() user: RequestUser) {
    try {
      return await this.getWalletUseCase.execute(user.userId);
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  @Get('me/transactions')
  async listCurrentWalletTransactions(
    @CurrentUser() user: RequestUser,
    @Query('limit') limit?: string,
  ) {
    const parsedLimit = Number(limit ?? 20);
    const safeLimit = Number.isFinite(parsedLimit) && parsedLimit > 0 ? Math.min(parsedLimit, 100) : 20;
    return this.listWalletTransactionsUseCase.execute(user.userId, safeLimit);
  }

  @Post('me/top-up')
  async topUpCredits(@CurrentUser() user: RequestUser, @Body() request: AdjustCreditsDto) {
    try {
      return await this.addCreditsUseCase.execute({
        userId: user.userId,
        amount: request.amount,
        description: request.description ?? `Top-up de ${request.amount} credito(s)`,
      });
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  @Post('me/consume')
  async consumeCredits(@CurrentUser() user: RequestUser, @Body() request: AdjustCreditsDto) {
    try {
      return await this.consumeCreditsUseCase.execute({
        userId: user.userId,
        amount: request.amount,
        description: request.description ?? `Consumo de ${request.amount} credito(s)`,
      });
    } catch (error) {
      this.rethrowKnownError(error);
    }
  }

  private rethrowKnownError(error: unknown): never {
    if (error instanceof UserNotFoundError) {
      throw new UnauthorizedException('Authenticated user was not found.');
    }

    if (error instanceof InsufficientCreditsError) {
      throw new BadRequestException('Insufficient credits.');
    }

    throw error;
  }
}