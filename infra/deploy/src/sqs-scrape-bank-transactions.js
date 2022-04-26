import * as aws from '@pulumi/aws';
import { projectName, projectTags } from '../../consts.mjs';

const queueName = `${projectName}-scrape-bank-transactions`;

export const queue = new aws.sqs.Queue(queueName, {
  fifoQueue: true,
  visibilityTimeoutSeconds: 5 * 60,
  tags: {
    ...projectTags,
  },
});

export const scrapeBankTransactionsSqsUrl = queue.url;
