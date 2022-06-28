import * as aws from '@pulumi/aws';
import { projectName, projectTags } from '../../consts';
import { scrapeBankTransactionsConsumerFunc } from './lambdas/scrape-bank-transactions-consumer';

const queueName = `${projectName}-scrape-bank-transactions`;

export const scrapeBankTxsQueue = new aws.sqs.Queue(queueName, {
  fifoQueue: true,
  visibilityTimeoutSeconds: 5 * 60,
  tags: {
    ...projectTags,
  },
});

scrapeBankTxsQueue.onEvent(
  `${projectName}-scrape-bank-txs-event-handler`,
  scrapeBankTransactionsConsumerFunc,
  {
    batchSize: 1,
  }
);

export const scrapeBankTransactionsSqsUrl = scrapeBankTxsQueue.url;

export const scrapeBankTransactionsSqsUrlEnvironmentVariable = {
  APPLICATION__ScrapeBankTransactionsSqsUrl: scrapeBankTransactionsSqsUrl,
};
