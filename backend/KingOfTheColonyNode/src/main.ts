import 'reflect-metadata';
import { ValidationPipe } from '@nestjs/common';
import { NestFactory } from '@nestjs/core';
import { DocumentBuilder, SwaggerModule } from '@nestjs/swagger';
import { ConfigService } from '@nestjs/config';
import { AppModule } from './app.module';

async function bootstrap() {
  const app = await NestFactory.create(AppModule, { bufferLogs: true });
  const configService = app.get(ConfigService);

  const apiPrefix = configService.get<string>('app.apiPrefix', '/api');
  const appName = configService.get<string>('app.name', 'kingofthecolony-node');
  const port = configService.get<number>('app.port', 3000);
  const enableSwagger = configService.get<boolean>('app.enableSwagger', true);
  const corsOrigin = configService.get<string>('app.corsOrigin', 'http://localhost:4200');

  app.setGlobalPrefix(apiPrefix.replace(/^\//, ''));
  app.enableCors({ origin: corsOrigin, credentials: true });
  app.useGlobalPipes(
    new ValidationPipe({
      whitelist: true,
      transform: true,
      forbidNonWhitelisted: true,
    }),
  );

  if (enableSwagger) {
    const swaggerConfig = new DocumentBuilder()
      .setTitle('KingOfTheColony Node API')
      .setDescription('API base para King of the Colony en Node.js')
      .setVersion('0.1.0')
      .addBearerAuth()
      .build();

    const document = SwaggerModule.createDocument(app, swaggerConfig);
    SwaggerModule.setup('swagger', app, document);
  }

  await app.listen(port);
  console.log(`${appName} listening on port ${port}`);
}

void bootstrap();