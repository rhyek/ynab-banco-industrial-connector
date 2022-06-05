import * as awsx from '@pulumi/awsx';
import { projectName } from '../../consts';
import { pushNotificationTxApiHandler } from './lambdas/push-notification-tx-api-handler';

const api = new awsx.apigateway.API(`${projectName}-api-gateway`, {
  routes: [
    {
      path: '/new-push-notification-tx',
      method: 'POST',
      eventHandler: pushNotificationTxApiHandler,
    },
  ],
});

export const apiUrl = api.url;
