// Package storage keeps notifications in an embedded SQLite database (pure Go driver, no CGO).
package storage

import (
	"context"
	"database/sql"
	"errors"
	"fmt"
	"time"

	"github.com/edybackend/orders-microservices/notifications/internal/notification"

	_ "modernc.org/sqlite" // registers the "sqlite" driver
)

const schema = `
CREATE TABLE IF NOT EXISTS notifications (
	id         INTEGER PRIMARY KEY AUTOINCREMENT,
	event_id   TEXT NOT NULL UNIQUE,
	order_id   TEXT NOT NULL,
	kind       TEXT NOT NULL,
	message    TEXT NOT NULL,
	created_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_notifications_order_id ON notifications (order_id);`

// SQLite implements notification.Store and the read queries used by the HTTP API.
type SQLite struct {
	db *sql.DB
}

// Open opens (or creates) the database at path and applies the schema.
func Open(ctx context.Context, path string) (*SQLite, error) {
	db, err := sql.Open("sqlite", path)
	if err != nil {
		return nil, fmt.Errorf("open sqlite %s: %w", path, err)
	}
	db.SetMaxOpenConns(1) // SQLite allows a single writer; one connection avoids "database is locked"
	if _, err := db.ExecContext(ctx, schema); err != nil {
		return nil, errors.Join(fmt.Errorf("apply schema: %w", err), db.Close())
	}
	return &SQLite{db: db}, nil
}

// Close releases the database.
func (s *SQLite) Close() error { return s.db.Close() }

// Save stores n and reports whether it was new. A duplicate event id is ignored, which makes consumers idempotent.
func (s *SQLite) Save(ctx context.Context, n notification.Notification) (bool, error) {
	res, err := s.db.ExecContext(ctx,
		`INSERT INTO notifications (event_id, order_id, kind, message, created_at)
		 VALUES (?, ?, ?, ?, ?) ON CONFLICT(event_id) DO NOTHING`,
		n.EventID, n.OrderID, n.Kind, n.Message, n.CreatedAt.Format(time.RFC3339Nano))
	if err != nil {
		return false, fmt.Errorf("insert notification: %w", err)
	}
	rows, err := res.RowsAffected()
	if err != nil {
		return false, fmt.Errorf("rows affected: %w", err)
	}
	return rows == 1, nil
}

// List returns the newest notifications, optionally filtered by order id.
func (s *SQLite) List(ctx context.Context, orderID string, limit int) (_ []notification.Notification, err error) {
	query := `SELECT id, event_id, order_id, kind, message, created_at FROM notifications`
	args := []any{}
	if orderID != "" {
		query += ` WHERE order_id = ?`
		args = append(args, orderID)
	}
	query += ` ORDER BY id DESC LIMIT ?`
	args = append(args, limit)

	rows, err := s.db.QueryContext(ctx, query, args...)
	if err != nil {
		return nil, fmt.Errorf("query notifications: %w", err)
	}
	defer func() { err = errors.Join(err, rows.Close()) }()

	result := []notification.Notification{}
	for rows.Next() {
		var n notification.Notification
		var createdAt string
		if err := rows.Scan(&n.ID, &n.EventID, &n.OrderID, &n.Kind, &n.Message, &createdAt); err != nil {
			return nil, fmt.Errorf("scan notification: %w", err)
		}
		if n.CreatedAt, err = time.Parse(time.RFC3339Nano, createdAt); err != nil {
			return nil, fmt.Errorf("parse created_at: %w", err)
		}
		result = append(result, n)
	}
	return result, rows.Err()
}
