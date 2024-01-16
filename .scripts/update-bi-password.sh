#!/bin/bash
__dirname=$(dirname "$(readlink -f "$0")")
workspace_dir=$__dirname/..

pulumi logout
pulumi login
current_dir=$(pwd)
cd $workspace_dir/infra/deploy
pulumi config set --secret ynab-banco-industrial-connector.deploy:BANCO_INDUSTRIAL_SCRAPER__Auth__Password
cd $current_dir
