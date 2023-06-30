import * as awsx from '@pulumi/awsx';
import { projectName } from '../../consts';
import { pushNotificationTxApiHandler } from './lambdas/push-notification-tx-api-handler';
import { scrapeConfirmedBankTxsApiHandler } from './lambdas/scrape-confirmed-bank-txs-api-handler';

const api = new awsx.classic.apigateway.API(`${projectName}-api-gateway`, {
  routes: [
    {
      path: '/new-push-notification-tx',
      method: 'POST',
      eventHandler: pushNotificationTxApiHandler,
    },
    {
      path: '/scrape-confirmed-txs',
      method: 'POST',
      eventHandler: scrapeConfirmedBankTxsApiHandler,
    },
  ],
});

export const apiUrl = api.url;
