using Microsoft.Data.Sqlite;

namespace MeatSpeak.Client.Core.Data;

public sealed class ClientDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public ClientDatabase(string dbPath)
    {
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        Initialize();
    }

    private void Initialize()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS server_profiles (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                host TEXT NOT NULL,
                port INTEGER NOT NULL DEFAULT 6667,
                use_ssl INTEGER NOT NULL DEFAULT 0,
                nickname TEXT NOT NULL,
                username TEXT,
                realname TEXT,
                password TEXT,
                sasl_username TEXT,
                sasl_password TEXT,
                use_identity_auth INTEGER NOT NULL DEFAULT 0,
                identity_domain TEXT,
                auto_join_channels TEXT,
                auto_connect INTEGER NOT NULL DEFAULT 0,
                sort_order INTEGER NOT NULL DEFAULT 0,
                server_type INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS message_cache (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                server_id TEXT NOT NULL,
                target TEXT NOT NULL,
                sender_nick TEXT NOT NULL,
                content TEXT NOT NULL,
                message_type INTEGER NOT NULL DEFAULT 0,
                timestamp TEXT NOT NULL,
                is_own_message INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS idx_message_cache_target
                ON message_cache(server_id, target, timestamp);

            CREATE TABLE IF NOT EXISTS tofu_pins (
                entity_id TEXT PRIMARY KEY,
                key_fingerprint TEXT NOT NULL,
                first_seen TEXT NOT NULL,
                sources INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS user_preferences (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public List<ServerProfile> LoadServerProfiles()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM server_profiles ORDER BY sort_order";

        var profiles = new List<ServerProfile>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            profiles.Add(new ServerProfile
            {
                Id = Guid.Parse(reader.GetString(0)),
                Name = reader.GetString(1),
                Host = reader.GetString(2),
                Port = reader.GetInt32(3),
                UseSsl = reader.GetInt32(4) != 0,
                Nickname = reader.GetString(5),
                Username = reader.IsDBNull(6) ? null : reader.GetString(6),
                Realname = reader.IsDBNull(7) ? null : reader.GetString(7),
                Password = reader.IsDBNull(8) ? null : reader.GetString(8),
                SaslUsername = reader.IsDBNull(9) ? null : reader.GetString(9),
                SaslPassword = reader.IsDBNull(10) ? null : reader.GetString(10),
                UseIdentityAuth = reader.GetInt32(11) != 0,
                IdentityDomain = reader.IsDBNull(12) ? null : reader.GetString(12),
                AutoJoinChannels = (reader.IsDBNull(13) ? "" : reader.GetString(13))
                    .Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                AutoConnect = reader.GetInt32(14) != 0,
                SortOrder = reader.GetInt32(15),
                Type = (ServerType)reader.GetInt32(16),
            });
        }

        return profiles;
    }

    public void SaveServerProfile(ServerProfile profile)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO server_profiles
            (id, name, host, port, use_ssl, nickname, username, realname, password,
             sasl_username, sasl_password, use_identity_auth, identity_domain,
             auto_join_channels, auto_connect, sort_order, server_type)
            VALUES
            (@id, @name, @host, @port, @ssl, @nick, @user, @real, @pass,
             @saslUser, @saslPass, @identAuth, @identDomain,
             @autoJoin, @autoConnect, @sortOrder, @serverType)
            """;

        cmd.Parameters.AddWithValue("@id", profile.Id.ToString());
        cmd.Parameters.AddWithValue("@name", profile.Name);
        cmd.Parameters.AddWithValue("@host", profile.Host);
        cmd.Parameters.AddWithValue("@port", profile.Port);
        cmd.Parameters.AddWithValue("@ssl", profile.UseSsl ? 1 : 0);
        cmd.Parameters.AddWithValue("@nick", profile.Nickname);
        cmd.Parameters.AddWithValue("@user", (object?)profile.Username ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@real", (object?)profile.Realname ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pass", (object?)profile.Password ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@saslUser", (object?)profile.SaslUsername ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@saslPass", (object?)profile.SaslPassword ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@identAuth", profile.UseIdentityAuth ? 1 : 0);
        cmd.Parameters.AddWithValue("@identDomain", (object?)profile.IdentityDomain ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@autoJoin", string.Join(',', profile.AutoJoinChannels));
        cmd.Parameters.AddWithValue("@autoConnect", profile.AutoConnect ? 1 : 0);
        cmd.Parameters.AddWithValue("@sortOrder", profile.SortOrder);
        cmd.Parameters.AddWithValue("@serverType", (int)profile.Type);

        cmd.ExecuteNonQuery();
    }

    public void DeleteServerProfile(Guid id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM server_profiles WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id.ToString());
        cmd.ExecuteNonQuery();
    }

    public void CacheMessage(string serverId, string target, State.ChatMessage message)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO message_cache (server_id, target, sender_nick, content, message_type, timestamp, is_own_message)
            VALUES (@serverId, @target, @nick, @content, @type, @ts, @own)
            """;
        cmd.Parameters.AddWithValue("@serverId", serverId);
        cmd.Parameters.AddWithValue("@target", target);
        cmd.Parameters.AddWithValue("@nick", message.SenderNick);
        cmd.Parameters.AddWithValue("@content", message.Content);
        cmd.Parameters.AddWithValue("@type", (int)message.Type);
        cmd.Parameters.AddWithValue("@ts", message.Timestamp.ToString("O"));
        cmd.Parameters.AddWithValue("@own", message.IsOwnMessage ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public List<State.ChatMessage> LoadCachedMessages(string serverId, string target, int limit = 100)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT sender_nick, content, message_type, timestamp, is_own_message
            FROM message_cache
            WHERE server_id = @serverId AND target = @target
            ORDER BY timestamp DESC
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@serverId", serverId);
        cmd.Parameters.AddWithValue("@target", target);
        cmd.Parameters.AddWithValue("@limit", limit);

        var messages = new List<State.ChatMessage>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            messages.Add(new State.ChatMessage
            {
                SenderNick = reader.GetString(0),
                Content = reader.GetString(1),
                Type = (State.ChatMessageType)reader.GetInt32(2),
                Timestamp = DateTimeOffset.Parse(reader.GetString(3)),
                IsOwnMessage = reader.GetInt32(4) != 0,
            });
        }

        messages.Reverse();
        return messages;
    }

    public string? GetPreference(string key)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT value FROM user_preferences WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);
        return cmd.ExecuteScalar() as string;
    }

    public void SetPreference(string key, string value)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO user_preferences (key, value) VALUES (@key, @value)";
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@value", value);
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
