import * as aws from '@pulumi/aws';
import { projectName } from '../../../consts';
import { scrapeConfirmedTxsScheduleEventHandler } from '../lambdas/scrape-confirmed-txs-schedule-event-handler';

export const scrapeConfirmedTxsScheduler: aws.cloudwatch.EventRuleEventSubscription =
  aws.cloudwatch.onSchedule(
    `${projectName}-scrape-confirmed-txs-scheduler`,
    // https://docs.aws.amazon.com/lambda/latest/dg/services-cloudwatchevents-expressions.html
    // cron(Minutes Hours Day-of-month Month Day-of-week Year)
    // One of the day-of-month or day-of-week values must be a question mark (?).
    'cron(0 13 * * ? *)',
    scrapeConfirmedTxsScheduleEventHandler
  );
