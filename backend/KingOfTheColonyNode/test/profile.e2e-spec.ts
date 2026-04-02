import { INestApplication, ValidationPipe } from '@nestjs/common';
import { Test } from '@nestjs/testing';
import request from 'supertest';
import { AppModule } from '../src/app.module';

describe('ProfileController (e2e)', () => {
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

  it('gets and updates the authenticated profile', async () => {
    const registerResponse = await request(app.getHttpServer())
      .post('/api/auth/register')
      .send({
        email: 'profile-user@example.com',
        password: 'supersecure123',
        displayName: 'Profile User',
      })
      .expect(201);

    const accessToken = registerResponse.body.accessToken as string;
    const userId = registerResponse.body.user.id as string;

    const meResponse = await request(app.getHttpServer())
      .get('/api/profile/me')
      .set('Authorization', `Bearer ${accessToken}`)
      .expect(200);

    expect(meResponse.body.displayName).toBe('Profile User');
    expect(meResponse.body.avatarUrl).toBeNull();
    expect(meResponse.body.bio).toBeNull();

    const updatedResponse = await request(app.getHttpServer())
      .patch('/api/profile/me')
      .set('Authorization', `Bearer ${accessToken}`)
      .send({
        displayName: 'Updated User',
        avatarUrl: 'https://example.com/avatar.png',
        bio: 'Jugador competitivo de KOF.',
      })
      .expect(200);

    expect(updatedResponse.body.displayName).toBe('Updated User');
    expect(updatedResponse.body.avatarUrl).toBe('https://example.com/avatar.png');
    expect(updatedResponse.body.bio).toBe('Jugador competitivo de KOF.');

    const publicProfileResponse = await request(app.getHttpServer())
      .get(`/api/profile/${userId}`)
      .expect(200);

    expect(publicProfileResponse.body.displayName).toBe('Updated User');
    expect(publicProfileResponse.body.avatarUrl).toBe('https://example.com/avatar.png');
    expect(publicProfileResponse.body.bio).toBe('Jugador competitivo de KOF.');
  }, 20000);
});