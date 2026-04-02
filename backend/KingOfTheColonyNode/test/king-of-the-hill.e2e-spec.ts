import { INestApplication, ValidationPipe } from '@nestjs/common';
import { Test } from '@nestjs/testing';
import request from 'supertest';
import { AppModule } from '../src/app.module';

describe('KingOfTheHillController (e2e)', () => {
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

  it('creates rooms and manages challenger plus queue order', async () => {
    const kingRegister = await request(app.getHttpServer())
      .post('/api/auth/register')
      .send({
        email: 'king-room@example.com',
        password: 'supersecure123',
        displayName: 'Current King',
      })
      .expect(201);

    const challengerRegister = await request(app.getHttpServer())
      .post('/api/auth/register')
      .send({
        email: 'challenger-room@example.com',
        password: 'supersecure123',
        displayName: 'First Challenger',
      })
      .expect(201);

    const queuedRegister = await request(app.getHttpServer())
      .post('/api/auth/register')
      .send({
        email: 'queued-room@example.com',
        password: 'supersecure123',
        displayName: 'Queued Player',
      })
      .expect(201);

    const roomResponse = await request(app.getHttpServer())
      .post('/api/king-of-the-hill/rooms')
      .set('Authorization', `Bearer ${kingRegister.body.accessToken as string}`)
      .send({ name: 'Sala Central' })
      .expect(201);

    expect(roomResponse.body.status).toBe('WaitingChallenger');
    expect(roomResponse.body.king.displayName).toBe('Current King');
    expect(roomResponse.body.challenger).toBeNull();
    expect(roomResponse.body.queue).toHaveLength(0);

    const roomId = roomResponse.body.roomId as string;

    const joinAsChallengerResponse = await request(app.getHttpServer())
      .post(`/api/king-of-the-hill/rooms/${roomId}/join-queue`)
      .set('Authorization', `Bearer ${challengerRegister.body.accessToken as string}`)
      .expect(201);

    expect(joinAsChallengerResponse.body.joinedAsChallenger).toBe(true);
    expect(joinAsChallengerResponse.body.queuePosition).toBeNull();
    expect(joinAsChallengerResponse.body.room.challenger.displayName).toBe('First Challenger');
    expect(joinAsChallengerResponse.body.room.challenger.credits).toBe(2);

    const walletAfterChallenge = await request(app.getHttpServer())
      .get('/api/wallet/me')
      .set('Authorization', `Bearer ${challengerRegister.body.accessToken as string}`)
      .expect(200);

    expect(walletAfterChallenge.body.balance).toBe(2);

    const joinQueueResponse = await request(app.getHttpServer())
      .post(`/api/king-of-the-hill/rooms/${roomId}/join-queue`)
      .set('Authorization', `Bearer ${queuedRegister.body.accessToken as string}`)
      .expect(201);

    expect(joinQueueResponse.body.joinedAsChallenger).toBe(false);
    expect(joinQueueResponse.body.queuePosition).toBe(1);
    expect(joinQueueResponse.body.room.queue).toHaveLength(1);
    expect(joinQueueResponse.body.room.queue[0].position).toBe(1);
    expect(joinQueueResponse.body.room.queue[0].user.displayName).toBe('Queued Player');
    expect(joinQueueResponse.body.room.queue[0].user.credits).toBe(2);

    await request(app.getHttpServer())
      .post(`/api/king-of-the-hill/rooms/${roomId}/join-queue`)
      .set('Authorization', `Bearer ${queuedRegister.body.accessToken as string}`)
      .expect(400);

    const roomStateResponse = await request(app.getHttpServer())
      .get(`/api/king-of-the-hill/rooms/${roomId}`)
      .expect(200);

    expect(roomStateResponse.body.status).toBe('ReadyToPlay');
    expect(roomStateResponse.body.king.displayName).toBe('Current King');
    expect(roomStateResponse.body.challenger.displayName).toBe('First Challenger');
    expect(roomStateResponse.body.queue).toHaveLength(1);

    const roomsResponse = await request(app.getHttpServer())
      .get('/api/king-of-the-hill/rooms')
      .expect(200);

    expect(roomsResponse.body).toHaveLength(1);
    expect(roomsResponse.body[0].roomId).toBe(roomId);

    const createMatchSessionResponse = await request(app.getHttpServer())
      .post(`/api/king-of-the-hill/rooms/${roomId}/match-sessions`)
      .set('Authorization', `Bearer ${kingRegister.body.accessToken as string}`)
      .send({
        kingUserId: kingRegister.body.user.id,
        challengerUserId: challengerRegister.body.user.id,
        gameRom: 'kof2002',
        launchSource: 'LauncherWpf',
      })
      .expect(201);

    expect(createMatchSessionResponse.body.status).toBe('Created');
    expect(createMatchSessionResponse.body.king.displayName).toBe('Current King');
    expect(createMatchSessionResponse.body.challenger.displayName).toBe('First Challenger');

    const matchSessionId = createMatchSessionResponse.body.matchSessionId as string;

    const startMatchSessionResponse = await request(app.getHttpServer())
      .post(`/api/king-of-the-hill/rooms/${roomId}/match-sessions/${matchSessionId}/start`)
      .set('Authorization', `Bearer ${challengerRegister.body.accessToken as string}`)
      .send({ emulatorProcessId: 321 })
      .expect(201);

    expect(startMatchSessionResponse.body.status).toBe('Running');
    expect(startMatchSessionResponse.body.emulatorProcessId).toBe(321);

    const playingRoomStateResponse = await request(app.getHttpServer())
      .get(`/api/king-of-the-hill/rooms/${roomId}`)
      .expect(200);

    expect(playingRoomStateResponse.body.status).toBe('Playing');

    const completeMatchSessionResponse = await request(app.getHttpServer())
      .post(`/api/king-of-the-hill/rooms/${roomId}/match-sessions/${matchSessionId}/complete`)
      .set('Authorization', `Bearer ${challengerRegister.body.accessToken as string}`)
      .send({
        winnerUserId: challengerRegister.body.user.id,
        loserUserId: kingRegister.body.user.id,
        resultSource: 'ManualSelection',
        idempotencyKey: 'result-1',
      })
      .expect(201);

    expect(completeMatchSessionResponse.body.matchSession.status).toBe('Completed');
    expect(completeMatchSessionResponse.body.matchSession.winner.displayName).toBe('First Challenger');
    expect(completeMatchSessionResponse.body.room.king.displayName).toBe('First Challenger');
    expect(completeMatchSessionResponse.body.room.challenger.displayName).toBe('Queued Player');
    expect(completeMatchSessionResponse.body.room.queue).toHaveLength(0);
    expect(completeMatchSessionResponse.body.nextChallenger.displayName).toBe('Queued Player');
    expect(completeMatchSessionResponse.body.idempotentReplay).toBe(false);

    const replayResponse = await request(app.getHttpServer())
      .post(`/api/king-of-the-hill/rooms/${roomId}/match-sessions/${matchSessionId}/complete`)
      .set('Authorization', `Bearer ${challengerRegister.body.accessToken as string}`)
      .send({
        winnerUserId: challengerRegister.body.user.id,
        loserUserId: kingRegister.body.user.id,
        resultSource: 'ManualSelection',
        idempotencyKey: 'result-1',
      })
      .expect(201);

    expect(replayResponse.body.idempotentReplay).toBe(true);

    const finalRoomStateResponse = await request(app.getHttpServer())
      .get(`/api/king-of-the-hill/rooms/${roomId}`)
      .expect(200);

    expect(finalRoomStateResponse.body.status).toBe('ReadyToPlay');
    expect(finalRoomStateResponse.body.king.displayName).toBe('First Challenger');
    expect(finalRoomStateResponse.body.challenger.displayName).toBe('Queued Player');
  }, 20000);
});