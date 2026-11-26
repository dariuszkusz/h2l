# n8n-nodes-h2l

Official **n8n Community Node** for **h2l (Human-in-the-Loop) Workflow Integrator**.

## Verified Community Nodes Compliance

- **Zero external runtime dependencies** (`"dependencies": {}`) — uses only Node.js built-in `node:crypto` and `n8n-workflow` peer types.
- **Privacy by Default (Claim-Check)**: Sensitive form fields are excluded from n8n execution history unless explicitly opted into `PassThrough`.
- **HMAC-SHA256 + ±5 min replay protection**: Incoming h2l webhook callbacks are verified in constant time (`crypto.timingSafeEqual`).

## Operations

1. **Wait for Human Form** — Calls `POST /api/v1/webhooks/connectors/{connectorId}/tasks` with `$execution.resumeUrl`, pauses the workflow via `putExecutionToWait`, and routes the verified callback to **Completed**, **Expired**, or **Cancelled** outputs.
2. **Get Sensitive Data** — Calls `GET /api/v1/submissions/{submissionId}/sensitive-data` to fetch decrypted Claim-Check values (audited in `audit.audit_logs` on h2l).

## Development

```bash
npm install
npm run build
npm run lint
npm test
```
