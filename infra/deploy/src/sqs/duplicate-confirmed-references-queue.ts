import * as aws from '@pulumi/aws';
import { projectName, projectTags } from '../../../consts';
import { duplicateConfirmedReferencesHandler } from '../lambdas/duplicate-confirmed-references-handler';

export const duplicateConfirmedReferencesQueue = new aws.sqs.Queue(
  `${projectName}-duplicate-confirmed-refs-queue`,
  {
    visibilityTimeoutSeconds: 5 * 60,
    tags: {
      ...projectTags,
    },
  }
);

duplicateConfirmedReferencesQueue.onEvent(
  `${projectName}duplicate-confirmed-refs-on-event`,
  duplicateConfirmedReferencesHandler
);

export const duplicateConfirmedReferencesQueueUrlEnvironmentVariable = {
  APPLICATION__DuplicateConfirmedReferencesSqsUrl:
    duplicateConfirmedReferencesQueue.url,
};
