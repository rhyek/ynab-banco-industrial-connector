import * as aws from '@pulumi/aws';
import { projectName, projectTags } from '../../consts';
import { scrapeBankTransactionsConsumerFunc } from './lambdas/scrape-bank-transactions-consumer/lambda';

const queueName = `${projectName}-scrape-bank-transactions`;

export const queue = new aws.sqs.Queue(queueName, {
  fifoQueue: true,
  visibilityTimeoutSeconds: 5 * 60,
  tags: {
    ...projectTags,
  },
});

queue.onEvent(
  `${projectName}-scrape-bank-txs-event-handler`,
  scrapeBankTransactionsConsumerFunc,
  {
    batchSize: 1,
  }
);

export const scrapeBankTransactionsSqsUrl = queue.url;

export const scrapeBankTransactionsSqsUrlEnvironmentVariable = {
  APPLICATION__ScrapeBankTransactionsSqsUrl: scrapeBankTransactionsSqsUrl,
};
