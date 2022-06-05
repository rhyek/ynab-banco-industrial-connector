import {
  projectTags,
  pushNotificationTxDocumentHandlerNamespace,
} from '../../../../consts';
import * as aws from '@pulumi/aws';
import { buildStack } from '../../build-stack';
import { lambdaRole } from '../common/lambda-role';

const pushNotificationTxDocumentHandlerImage = buildStack.getOutput(
  'pushNotificationTxDocumentHandlerImage'
);

export const pushNotificationTxDocumentHandlerFunc = new aws.lambda.Function(
  `${pushNotificationTxDocumentHandlerNamespace}-lambda`,
  {
    packageType: 'Image',
    imageUri: pushNotificationTxDocumentHandlerImage,
    role: lambdaRole.arn,
    timeout: 3 * 60,
    tags: {
      ...projectTags,
    },
  }
);
