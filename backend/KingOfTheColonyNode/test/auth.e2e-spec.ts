import { INestApplication, ValidationPipe } from '@nestjs/common';
import { Test } from '@nestjs/testing';
import request from 'supertest';
import { AppModule } from '../src/app.module';

describe('AuthController (e2e)', () => {
  let app: INestApplication;

  beforeAll(async () => {
    const moduleRef = await Test.createTestingModule({
      imports: [AppModule],
    }).compile();

    app = moduleRef.createNestApplication();
    app.setGlobalPrefix('api');
    app.useGlobalPipes(
      new ValidationPipe({
        whitelist: true,
        transform: true,
        forbidNonWhitelisted: true,
      }),
    );
    await app.init();
  });

  afterAll(async () => {
    await app.close();
  });

  it('registers, authenticates, refreshes and logs out a user', async () => {
    const registerResponse = await request(app.getHttpServer())
      .post('/api/auth/register')
      .send({
        email: 'player1@example.com',
        password: 'supersecure123',
        displayName: 'Player One',
      })
      .expect(201);

    expect(registerResponse.body.accessToken).toBeDefined();
    expect(registerResponse.body.refreshToken).toBeDefined();
    expect(registerResponse.body.user.email).toBe('player1@example.com');
    expect(registerResponse.body.user.displayName).toBe('Player One');

    const accessToken = registerResponse.body.accessToken as string;
    const refreshToken = registerResponse.body.refreshToken as string;

    const meResponse = await request(app.getHttpServer())
      .get('/api/auth/me')
      .set('Authorization', `Bearer ${accessToken}`)
      .expect(200);

    expect(meResponse.body.email).toBe('player1@example.com');
    expect(meResponse.body.roles).toContain('user');

    const loginResponse = await request(app.getHttpServer())
      .post('/api/auth/login')
      .send({
        email: 'player1@example.com',
        password: 'supersecure123',
      })
      .expect(201);

    expect(loginResponse.body.accessToken).toBeDefined();
    expect(loginResponse.body.refreshToken).toBeDefined();

    const refreshedResponse = await request(app.getHttpServer())
      .post('/api/auth/refresh')
      .send({ refreshToken: loginResponse.body.refreshToken })
      .expect(201);

    expect(refreshedResponse.body.accessToken).toBeDefined();
    expect(refreshedResponse.body.refreshToken).toBeDefined();
    expect(refreshedResponse.body.refreshToken).not.toBe(loginResponse.body.refreshToken);

    await request(app.getHttpServer())
      .post('/api/auth/logout')
      .set('Authorization', `Bearer ${refreshedResponse.body.accessToken}`)
      .expect(201);

    await request(app.getHttpServer())
      .post('/api/auth/refresh')
      .send({ refreshToken: refreshedResponse.body.refreshToken })
      .expect(401);
  }, 20000);
});