import * as aws from '@pulumi/aws';
import { projectName, projectTags } from '../../../consts';
import { scrapeBankTransactionsConsumerFunc } from '../lambdas/scrape-bank-transactions-consumer';

export const scrapeBankTxsQueue = new aws.sqs.Queue(
  `${projectName}-scrape-bank-transactions`,
  {
    fifoQueue: true,
    visibilityTimeoutSeconds: 5 * 60,
    tags: {
      ...projectTags,
    },
  }
);

scrapeBankTxsQueue.onEvent(
  `${projectName}-scrape-bank-txs-event-handler`,
  scrapeBankTransactionsConsumerFunc,
  {
    batchSize: 1,
  }
);

export const scrapeBankTransactionsSqsUrlEnvironmentVariable = {
  APPLICATION__ScrapeBankTransactionsSqsUrl: scrapeBankTxsQueue.url,
};
