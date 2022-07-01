export { apiUrl } from './api-gateway';
export { pushNotificationTxsTableName } from './dynamodb';
export { playwrightTracesBucketName } from './playwright-traces-s3-bucket';

// sqs
import { scrapeBankTxsQueue } from './sqs/sqs-scrape-bank-transactions';
import {} from './sqs/duplicate-confirmed-references-queue';
export const scrapeBankTransactionsSqsUrl = scrapeBankTxsQueue.url;

// schedules
import './cloudwatch/schedules';
