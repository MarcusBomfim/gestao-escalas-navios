import http from 'k6/http'
import { check, fail, sleep } from 'k6'

const baseUrl = (__ENV.K6_BASE_URL || 'http://host.docker.internal:8080').replace(/\/$/, '')
const profile = __ENV.K6_PROFILE || 'smoke'

const profiles = {
  smoke: {
    executor: 'shared-iterations',
    vus: 1,
    iterations: 4,
    maxDuration: '30s',
  },
  load: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '30s', target: 5 },
      { duration: '1m', target: 15 },
      { duration: '30s', target: 15 },
      { duration: '30s', target: 0 },
    ],
    gracefulRampDown: '15s',
  },
}

if (!profiles[profile]) {
  throw new Error(`K6_PROFILE inválido: ${profile}. Use smoke ou load.`)
}

export const options = {
  scenarios: {
    port_management_read_traffic: profiles[profile],
  },
  thresholds: {
    checks: ['rate>0.99'],
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1000', 'p(99)<1800'],
    'http_req_duration{name:control_tower}': ['p(95)<1200'],
    'http_req_duration{name:list_vessels}': ['p(95)<500'],
    'http_req_duration{name:list_port_calls}': ['p(95)<500'],
    'http_req_duration{name:reference_ports}': ['p(95)<600'],
  },
  discardResponseBodies: true,
  userAgent: 'port-management-k6/1.0',
}

export function setup() {
  const email = __ENV.K6_USER_EMAIL
  const password = __ENV.K6_USER_PASSWORD

  if (!email || !password) {
    fail('K6_USER_EMAIL e K6_USER_PASSWORD são obrigatórios.')
  }

  const response = http.post(
    `${baseUrl}/api/v1/auth/login`,
    JSON.stringify({ email, password }),
    {
      headers: { 'Content-Type': 'application/json' },
      responseType: 'text',
      tags: { name: 'login' },
    },
  )

  const authenticated = check(response, {
    'login retorna 200': (result) => result.status === 200,
  })

  if (!authenticated) {
    fail(`Não foi possível autenticar o usuário de carga. Status: ${response.status}`)
  }

  let accessToken

  try {
    accessToken = response.json('accessToken')
  } catch (error) {
    fail(`A resposta de login não contém um token válido: ${error.message}`)
  }

  if (!accessToken) {
    fail('A resposta de login não contém accessToken.')
  }

  return {
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: 'application/json',
    },
  }
}

export default function (session) {
  const responses = http.batch([
    [
      'GET',
      `${baseUrl}/api/v1/control-tower`,
      null,
      { headers: session.headers, tags: { name: 'control_tower' } },
    ],
    [
      'GET',
      `${baseUrl}/api/v1/vessels?page=1&pageSize=10`,
      null,
      { headers: session.headers, tags: { name: 'list_vessels' } },
    ],
    [
      'GET',
      `${baseUrl}/api/v1/port-calls?page=1&pageSize=10`,
      null,
      { headers: session.headers, tags: { name: 'list_port_calls' } },
    ],
    [
      'GET',
      `${baseUrl}/api/v1/reference-data/ports`,
      null,
      { headers: session.headers, tags: { name: 'reference_ports' } },
    ],
  ])

  check(responses[0], {
    'torre de controle retorna 200': (response) => response.status === 200,
  })
  check(responses[1], {
    'navios retornam 200': (response) => response.status === 200,
  })
  check(responses[2], {
    'escalas retornam 200': (response) => response.status === 200,
  })
  check(responses[3], {
    'estrutura portuária retorna 200': (response) => response.status === 200,
  })

  sleep(0.3 + Math.random() * 0.7)
}
