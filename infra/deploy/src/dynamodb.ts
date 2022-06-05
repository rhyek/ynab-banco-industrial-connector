import * as aws from '@pulumi/aws';
import { projectName, projectTags } from '../../consts';

export const mobileNotificationTxsTable = new aws.dynamodb.Table(
  `${projectName}-mobile-notif-txs`,
  {
    attributes: [{ name: 'id', type: 'S' }],
    hashKey: 'id',
    readCapacity: 1,
    writeCapacity: 1,
    tags: {
      ...projectTags,
    },
  }
);
