import ngrok from 'ngrok';
import { chromium } from 'playwright';

async function main() {
  const port = 3800;
  const playwrightServerToken = process.env.PLAYWRIGHT_SERVER_TOKEN;
  const ngrokAuthToken = process.env.NGROK_AUTH_TOKEN;
  const ngrokSubdomain = 'playwright.rhyek';

  await chromium.launchServer({
    headless: true,
    port,
    wsPath: playwrightServerToken,
  });
  await ngrok.connect({
    authtoken: ngrokAuthToken,
    subdomain: ngrokSubdomain,
    addr: port,
  });

  console.log(
    'Playwright Url:',
    `wss://${ngrokSubdomain}.ngrok.io/${playwrightServerToken}`
  );
}

main();
