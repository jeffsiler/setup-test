# Gmail setup for daily email digest

Two ways to connect Gmail to Cursor. **Option A (MailMCP)** is fastest; **Option B (OAuth)** is better if you do not want an app password.

---

## Option A: MailMCP + Gmail App Password (recommended to start)

Works with Cursor Automations via HTTP MCP (good for cloud agents).

### 1. Enable IMAP in Gmail

1. Gmail → **Settings** (gear) → **See all settings** → **Forwarding and POP/IMAP**
2. Enable **IMAP**
3. Save

### 2. Create a Google App Password

1. Google Account → **Security** → **2-Step Verification** (must be on)
2. **App passwords** → create one named `Cursor MailMCP`
3. Copy the 16-character password

### 3. Sign up and get MailMCP credentials

1. Create a free account at [mailmcp.io](https://mailmcp.io)
2. Add your Gmail account (IMAP/SMTP)
3. Copy your **Client ID** and **Client Secret** from the dashboard

### 4. Add to Cursor MCP

**Cursor Settings → Tools & Integrations → MCP** → Add server, or add to `~/.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "mailmcp": {
      "url": "https://mailmcp.io/mcp/YOUR_CLIENT_ID",
      "headers": {
        "Authorization": "Bearer YOUR_CLIENT_SECRET"
      }
    }
  }
}
```

Use **global** MCP config (`~/.cursor/mcp.json`) so automations can access it.

### 5. Test in Cursor chat

Ask: *"List my unread Gmail messages from today."*

---

## Option B: @kembec/email-mcp (Gmail OAuth)

Uses the Gmail API with OAuth — no app password.

### 1. Google Cloud project

1. [Google Cloud Console](https://console.cloud.google.com) → New project
2. **APIs & Services → Library** → enable **Gmail API**
3. **OAuth consent screen** → External (or Internal for Workspace) → add scopes:
   - `https://www.googleapis.com/auth/gmail.readonly`
   - `https://www.googleapis.com/auth/gmail.send`
4. **Credentials → Create OAuth client** → **Desktop app**
5. Add redirect URIs:
   - `http://127.0.0.1` (local auth)
   - `https://www.cursor.com/agents/mcp/oauth/callback` (cloud automations)
   - `cursor://anysphere.cursor-mcp/oauth/callback` (Cursor desktop)

### 2. Add MCP to Cursor

```json
{
  "mcpServers": {
    "email": {
      "command": "npx",
      "args": ["-y", "@kembec/email-mcp"]
    }
  }
}
```

### 3. Authorize Gmail

In Cursor Agent chat, ask the agent to run `auth_start` with:

```json
{
  "provider": "gmail",
  "account_id": "personal",
  "client_id": "YOUR_CLIENT_ID",
  "client_secret": "YOUR_CLIENT_SECRET"
}
```

Open the returned URL in your browser and approve access. Use `account_id: "personal"` in the automation prompt when calling email tools.

---

## Gmail search queries for the automation

The agent can use these in `search_messages` (kembec) or equivalent MailMCP search:

| Goal | Gmail query |
|------|-------------|
| Last 24 hours, inbox only | `newer_than:1d in:inbox` |
| Skip promotions/social | `newer_than:1d in:inbox -category:promotions -category:social` |
| Action-oriented only | `newer_than:1d in:inbox (is:important OR is:starred OR label:action)` |
| From a person | `newer_than:1d from:someone@company.com` |

Add a Gmail label `action` and filter important senders into it if you want finer control.

---

## Create the automation

1. [cursor.com/automations](https://cursor.com/automations) → **New automation**
2. **Trigger:** Scheduled — e.g. `0 22 * * *` for 6 PM US Eastern (22:00 UTC)
3. **Repository:** None
4. **Tools:** MCP (mailmcp or email) + **Memories** (recommended)
5. **Prompt:** Copy from [`prompt.md`](./prompt.md) and set your `@gmail.com` address

### Gmail-specific prompt lines to add

Under **Configuration**, add:

```
- **Email provider:** Gmail
- **MCP account_id:** personal (kembec) or mailmcp default account
- **Inbox search:** newer_than:1d in:inbox -category:promotions -category:social
```

---

## Troubleshooting (Gmail)

| Issue | Fix |
|-------|-----|
| "Less secure app" / auth failed | Use App Password (Option A), not your normal Gmail password |
| OAuth browser does not open | Copy the auth URL from Cursor logs manually; use Desktop OAuth client type |
| Automation can't see MCP | Put MCP in global config; re-auth for Team Owned automations |
| Digest not sent | Ensure `gmail.send` scope (OAuth) or SMTP send enabled (MailMCP) |
| Too many newsletters | Tighten search: `-category:promotions -category:updates` |
