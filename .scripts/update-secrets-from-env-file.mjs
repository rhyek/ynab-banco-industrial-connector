#!/usr/bin/env node
import { config } from 'dotenv';
import { execaCommand as execa } from 'execa';
import { backendEnvironmentVariableKeys } from './consts/backend-environment-variable-keys.mjs';

config();

for (const key of backendEnvironmentVariableKeys) {
  await execa(
    `pulumi --cwd infra/deploy config set --secret ${key} ${process.env[key]}`,
    {
      stdio: 'inherit',
    }
  );
}
