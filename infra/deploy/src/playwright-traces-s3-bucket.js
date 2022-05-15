import * as aws from '@pulumi/aws';
import { projectTags } from '../../consts.mjs';

// Create our bucket using infrastructure as code.
export const playwrightTracesBucket = new aws.s3.Bucket('playwright-traces', {
  tags: {
    ...projectTags,
  },
});

export const playwrightTracesBucketName = playwrightTracesBucket.id;
