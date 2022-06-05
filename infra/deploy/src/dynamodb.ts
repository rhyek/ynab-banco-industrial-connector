import * as aws from '@pulumi/aws';
import { projectName, projectTags } from '../../consts';
import { pushNotificationTxDocumentHandlerFunc } from './lambdas/push-notification-tx-document-handler';

const pushNotificationTxsTable = new aws.dynamodb.Table(
  `${projectName}-push-notif-txs`,
  {
    attributes: [{ name: 'id', type: 'S' }],
    hashKey: 'id',
    readCapacity: 1,
    writeCapacity: 1,
    streamEnabled: true,
    streamViewType: 'NEW_AND_OLD_IMAGES',
    tags: {
      ...projectTags,
    },
  }
);

export const pushNotificationTxsTableName = pushNotificationTxsTable.name;

pushNotificationTxsTable.onEvent(
  `${projectName}-push-notif-txs-table-event-handler`,
  pushNotificationTxDocumentHandlerFunc,
  {
    startingPosition: 'LATEST',
  }
);
