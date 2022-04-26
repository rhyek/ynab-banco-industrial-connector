//@ts-check
import pulumi from '@pulumi/pulumi';

export const buildStack = new pulumi.StackReference(
  `rhyek/ynab-banco-industrial-connector.build/${pulumi.getStack()}`
);
