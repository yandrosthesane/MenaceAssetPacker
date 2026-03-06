# MCP Server for AI Assistants

This directory contains the Menace Modkit MCP (Model Context Protocol) server, which enables AI assistants like Claude to interact with your modpacks and the game.

## What is this?

The MCP server is an **optional component** that lets you use AI assistants to:
- Create and modify modpacks
- Query game state while playing
- Get help debugging mods
- Explore game templates and systems

## Quick Setup

### Automatic Configuration

The Modkit GUI app can automatically configure AI clients for you:
1. Open the Modkit app
2. Go to the Setup screen
3. Enable "AI Assistant Support"
4. Select your AI client (Claude Code, Claude Desktop, or OpenCode)
5. The app will configure everything automatically

### Manual Configuration

If you prefer to configure manually, see the example config in `claude_config_example.json`.

**For Claude Desktop:**

1. Find your config file:
   - **macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`
   - **Windows:** `%APPDATA%\Claude\claude_desktop_config.json`
   - **Linux:** `~/.config/Claude/claude_desktop_config.json`

2. Add this entry (adjust path to your installation):
   ```json
   {
     "mcpServers": {
       "menace-modkit": {
         "command": "/path/to/modkit/mcp/Menace.Modkit.Mcp"
       }
     }
   }
   ```

3. Restart Claude Desktop

**For Claude Code:**

1. Run Claude Code from your modkit directory, or add to `~/.claude/mcp.json`:
   ```json
   {
     "mcpServers": {
       "menace-modkit": {
         "command": "/path/to/modkit/mcp/Menace.Modkit.Mcp"
       }
     }
   }
   ```

**For OpenCode:**

Create or edit `~/.opencode/mcp.json` with the same format as Claude Code above.

## Requirements

- **No .NET SDK required** - The MCP server is a self-contained executable
- Works on Windows, Linux, and macOS
- Only needed if you want AI assistant integration

## Documentation

For detailed setup instructions and troubleshooting, see:
- [AI Assistant Setup Guide](../docs/system-guide/AI_ASSISTANT_SETUP.md)
- [MCP Servers Documentation](../docs/system-guide/mcp-servers.md)

## Do I need this?

**No!** The MCP server is completely optional. You can create mods using:
- The Modkit GUI app
- The Modkit CLI tool
- Manual editing of modpack files

The MCP server is just an extra tool for those who want AI assistance.
