// @ts-check
import aws from '@pulumi/aws';
import { httpApiNamespace, projectTags } from '../../../../consts.mjs';

const role = new aws.iam.Role(`${httpApiNamespace}-lambda-role`, {
  assumeRolePolicy: aws.iam.assumeRolePolicyForPrincipal({
    Service: 'lambda.amazonaws.com',
  }),
  tags: {
    ...projectTags,
  },
});
const lambdaRolePolicy = new aws.iam.Policy(`${httpApiNamespace}-role-policy`, {
  policy: {
    Version: '2012-10-17',
    Statement: [
      {
        Effect: 'Allow',
        Action: [
          'logs:CreateLogGroup',
          'logs:CreateLogStream',
          'logs:PutLogEvents',
          'sqs:SendMessage',
        ],
        Resource: '*',
      },
    ],
  },
  tags: {
    ...projectTags,
  },
});
new aws.iam.RolePolicyAttachment(`${httpApiNamespace}-role-policy-attachment`, {
  role: role.name,
  policyArn: lambdaRolePolicy.arn,
});

export { role };
