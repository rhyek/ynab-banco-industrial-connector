// @ts-check
import * as awsx from '@pulumi/awsx';
import {
  httpApiNamespace,
  projectTags,
  scrapeBankTransactionsConsumerNamespace,
} from '../../consts.mjs';

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
