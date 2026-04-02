import { INestApplication, ValidationPipe } from '@nestjs/common';
import { Test } from '@nestjs/testing';
import request from 'supertest';
import { AppModule } from '../src/app.module';

describe('WalletController (e2e)', () => {
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

  it('returns balance, top-up history and credit consumption', async () => {
    const registerResponse = await request(app.getHttpServer())
      .post('/api/auth/register')
      .send({
        email: 'wallet-user@example.com',
        password: 'supersecure123',
        displayName: 'Wallet User',
      })
      .expect(201);

    const accessToken = registerResponse.body.accessToken as string;
    expect(registerResponse.body.user.credits).toBe(3);

    const walletResponse = await request(app.getHttpServer())
      .get('/api/wallet/me')
      .set('Authorization', `Bearer ${accessToken}`)
      .expect(200);

    expect(walletResponse.body.balance).toBe(3);

    const topUpResponse = await request(app.getHttpServer())
      .post('/api/wallet/me/top-up')
      .set('Authorization', `Bearer ${accessToken}`)
      .send({ amount: 5, description: 'Compra inicial' })
      .expect(201);

    expect(topUpResponse.body.balance).toBe(8);
    expect(topUpResponse.body.transaction.amount).toBe(5);
    expect(topUpResponse.body.transaction.type).toBe('Purchase');

    const consumeResponse = await request(app.getHttpServer())
      .post('/api/wallet/me/consume')
      .set('Authorization', `Bearer ${accessToken}`)
      .send({ amount: 2, description: 'Entrada a partida' })
      .expect(201);

    expect(consumeResponse.body.balance).toBe(6);
    expect(consumeResponse.body.transaction.amount).toBe(-2);
    expect(consumeResponse.body.transaction.type).toBe('GameEntry');

    const transactionsResponse = await request(app.getHttpServer())
      .get('/api/wallet/me/transactions')
      .set('Authorization', `Bearer ${accessToken}`)
      .expect(200);

    expect(transactionsResponse.body).toHaveLength(2);
    expect(transactionsResponse.body[0].type).toBe('GameEntry');
    expect(transactionsResponse.body[1].type).toBe('Purchase');

    await request(app.getHttpServer())
      .post('/api/wallet/me/consume')
      .set('Authorization', `Bearer ${accessToken}`)
      .send({ amount: 99, description: 'Intento invalido' })
      .expect(400);
  }, 20000);
});