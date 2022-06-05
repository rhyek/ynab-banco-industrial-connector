import * as awsx from '@pulumi/awsx';
import { projectName } from '../../consts';
import { newMobileAppNotificationTxHandler } from './lambdas/new-mobile-app-notification-tx-handler';

const api = new awsx.apigateway.API(`${projectName}-api-gateway`, {
  routes: [
    {
      path: '/new-mobile-app-notification-tx',
      method: 'POST',
      eventHandler: newMobileAppNotificationTxHandler,
    },
  ],
});

export const apiUrl = api.url;
