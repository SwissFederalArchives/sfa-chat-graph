const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7075';

const PROXY_CONFIG = [
  {
    context: [
      "/api",
    ],
    target,
    secure: false
  },
  {
    context: [
      "/repositories"
    ],
    target: "http://localhost:7200",
    secure: false
  },
  {
    context: [
      "/api/v1/events/**"
    ],
    target,
    secure: false,
    ws: true
  }
]

module.exports = PROXY_CONFIG;
