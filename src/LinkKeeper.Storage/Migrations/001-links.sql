CREATE TABLE IF NOT EXISTS schema_versions (version INTEGER PRIMARY KEY);
CREATE TABLE IF NOT EXISTS links (
 id TEXT PRIMARY KEY, owner TEXT NOT NULL, original_url TEXT NOT NULL,
 normalized_url TEXT NOT NULL, category TEXT NOT NULL, description TEXT NOT NULL,
 source TEXT NOT NULL, created_at INTEGER NOT NULL, updated_at INTEGER NOT NULL,
 expires_at INTEGER, status TEXT NOT NULL CHECK(status IN ('Active','Expired','Archived')),
 archived_at INTEGER, UNIQUE(owner, normalized_url, category)
);
CREATE INDEX IF NOT EXISTS ix_links_expiry ON links(expires_at) WHERE status='Active';
CREATE INDEX IF NOT EXISTS ix_links_owner ON links(owner,status,created_at);
INSERT OR IGNORE INTO schema_versions VALUES(1);
