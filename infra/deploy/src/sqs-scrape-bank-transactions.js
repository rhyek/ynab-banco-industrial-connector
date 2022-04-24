import * as aws from '@pulumi/aws';
import { projectName, projectTags } from '../../consts.mjs';

const queueName = `${projectName}-scrape-bank-transactions`;

const queue = new aws.sqs.Queue(queueName, {
  fifoQueue: true,
  tags: {
    ...projectTags,
  },
});

export const scrapeBankTransactionsSqsUrl = queue.url;
