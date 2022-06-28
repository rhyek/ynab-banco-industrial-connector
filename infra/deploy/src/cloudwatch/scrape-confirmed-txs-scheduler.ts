import * as aws from '@pulumi/aws';
import { projectName } from '../../../consts';
import { scrapeConfirmedTxsScheduleEventHandler } from '../lambdas/scrape-confirmed-txs-schedule-event-handler';

export const scrapeConfirmedTxsScheduler: aws.cloudwatch.EventRuleEventSubscription =
  aws.cloudwatch.onSchedule(
    `${projectName}-scrape-confirmed-txs-scheduler`,
    // cron(Minutes Hours Day-of-month Month Day-of-week Year)
    'cron(0 13 * * ? *)',
    scrapeConfirmedTxsScheduleEventHandler
  );
