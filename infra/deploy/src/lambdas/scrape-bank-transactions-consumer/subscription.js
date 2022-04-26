//@ts-check

import aws from '@pulumi/aws';
import { queue } from '../../sqs-scrape-bank-transactions.js';
import { scrapeBankTransactionsConsumerFunc } from './lambda.js';

new aws.lambda.EventSourceMapping('scrape-sqs-to-lambda-event-source-mapping', {
  eventSourceArn: queue.arn,
  functionName: scrapeBankTransactionsConsumerFunc.arn,
});

// queue.onEvent(
//   'scrape-sqs-to-lambda-subscription',
//   scrapeBankTransactionsConsumerFunc,
//   { batchSize: 1 }
// );
