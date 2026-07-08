# Daily Email Summary Automation

A Cursor Automation that reads your inbox each day, summarizes communications, and emails you a digest with the **top 5 priorities for tomorrow**.

## What you need

1. **Cursor Automations** — create at [cursor.com/automations](https://cursor.com/automations)
2. **Email MCP** — Cursor has no built-in Gmail/Outlook integration; connect an email MCP server so the agent can read and send mail
3. **Your delivery email** — the address where the daily digest should be sent (usually the same inbox)

## Recommended architecture

```
Scheduled trigger (daily, UTC cron)
        ↓
   Cloud Agent (no repository)
        ↓
   Email MCP — read today's messages
        ↓
   Summarize + rank top 5 for tomorrow
        ↓
   Email MCP — send digest to you
```

## Step 1: Connect email MCP

Pick one email MCP and add it in **Cursor Settings → Tools & Integrations → MCP** (or your team dashboard for team automations).

| Option | Gmail | Outlook | Notes |
|--------|-------|---------|-------|
| [MailMCP](https://mailmcp.io/en/docs/cursor) | Yes | Yes | Hosted; read/search/send |
| [@kembec/email-mcp](https://github.com/Kembec/email-mcp) | Yes | Yes | OAuth; `list_messages`, `search_messages`, `send_message` |

For cloud automations, prefer **HTTP/SSE MCP** over local stdio so OAuth tokens stay in Cursor's backend.

OAuth callback URL for cloud agents:

```
https://www.cursor.com/agents/mcp/oauth/callback
```

Grant at least **read** access to inbox. Grant **send** if the agent will email you the digest (recommended).

## Step 2: Create the automation

1. Open [cursor.com/automations](https://cursor.com/automations) → **New automation**
2. **Trigger:** Scheduled → daily at your preferred time
3. **Repository:** None (this workflow does not need code)
4. **Tools:** Enable **MCP server** and select your email MCP. Optionally enable **Memories** to track the last processed message ID.
5. **Prompt:** Copy the contents of [`prompt.md`](./prompt.md) and replace the placeholders at the top
6. Save and activate

### Cron and timezone

Scheduled triggers use **UTC**. Convert your local time:

| Local time (example) | UTC cron |
|--------------------|----------|
| 6:00 PM US Eastern (EDT, UTC-4) | `0 22 * * *` |
| 6:00 PM US Pacific (PDT, UTC-7) | `0 1 * * *` |
| 8:00 AM US Eastern (EDT) | `0 12 * * *` |

Weekdays only: `0 22 * * 1-5` (Mon–Fri at 22:00 UTC).

## Step 3: Customize placeholders

In `prompt.md`, set:

- `YOUR_EMAIL@example.com` — inbox to read and where to send the digest
- `YOUR_NAME` — how the email should greet you
- Optional filters (labels, folders, domains to skip)

## Billing note

Each daily run is a Cloud Agent run billed at your plan's cloud-agent / API rates. Automations always run in Max Mode.

## Troubleshooting

| Issue | Fix |
|-------|-----|
| MCP not available in automation | Add MCP in dashboard Integrations; for Team Owned automations, re-auth under the team service account |
| Duplicate summaries | Enable Memories; prompt asks agent to record last message ID processed |
| Missing messages | Widen the time window in the prompt or check MCP search filters |
| Send fails | Confirm send permission on the MCP OAuth scope |

## Related docs

- [Cursor Automations](https://cursor.com/docs/cloud-agent/automations)
- [Cloud agent MCP](https://cursor.com/docs/cloud-agent/capabilities)
