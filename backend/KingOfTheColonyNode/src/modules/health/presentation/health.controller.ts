import { Controller, Get } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';

@Controller('health')
export class HealthController {
  constructor(private readonly configService: ConfigService) {}

  @Get()
  getHealth() {
    return {
      status: 'ok',
      service: this.configService.get<string>('app.name', 'kingofthecolony-node'),
      environment: this.configService.get<string>('app.nodeEnv', 'development'),
      timestamp: new Date().toISOString(),
    };
  }
}