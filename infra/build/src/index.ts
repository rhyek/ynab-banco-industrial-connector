import * as awsx from '@pulumi/awsx';
import {
  httpApiNamespace,
  projectTags,
  pushNotificationTxDocumentHandlerNamespace,
  scrapeBankTransactionsConsumerNamespace,
} from '../../consts';

const httpApiFuncRepo = new awsx.ecr.Repository(`${httpApiNamespace}-repo`, {
  tags: {
    ...projectTags,
  },
});

export const httpApiFuncImage = httpApiFuncRepo.buildAndPushImage({
  dockerfile:
    '../../src/YnabBancoIndustrialConnectorBackend/Programs/HttpApi/Dockerfile',
  context: '../../src/YnabBancoIndustrialConnectorBackend',
});

const scrapeBankTransactionsConsumerRepo = new awsx.ecr.Repository(
  `${scrapeBankTransactionsConsumerNamespace}-repo`,
  {
    tags: {
      ...projectTags,
    },
  }
);
export const scrapeBankTransactionsConsumerImage =
  scrapeBankTransactionsConsumerRepo.buildAndPushImage({
    dockerfile:
      '../../src/YnabBancoIndustrialConnectorBackend/Programs/ScrapeBankTransactionsConsumer/Dockerfile',
    context: '../../src/YnabBancoIndustrialConnectorBackend',
  });

const pushNotificationTxDocumentHandlerRepo = new awsx.ecr.Repository(
  `${pushNotificationTxDocumentHandlerNamespace}-repo`,
  {
    tags: {
      ...projectTags,
    },
  }
);
export const pushNotificationTxDocumentHandlerImage =
  pushNotificationTxDocumentHandlerRepo.buildAndPushImage({
    dockerfile:
      '../../src/YnabBancoIndustrialConnectorBackend/Programs/PushNotificationTransactionDocumentHandler/Dockerfile',
    context: '../../src/YnabBancoIndustrialConnectorBackend',
  });
