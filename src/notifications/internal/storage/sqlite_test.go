package storage_test

import (
	"context"
	"path/filepath"
	"testing"
	"time"

	"github.com/edybackend/orders-microservices/notifications/internal/notification"
	"github.com/edybackend/orders-microservices/notifications/internal/storage"
)

func openStore(t *testing.T) *storage.SQLite {
	t.Helper()
	store, err := storage.Open(context.Background(), filepath.Join(t.TempDir(), "test.db"))
	if err != nil {
		t.Fatalf("open: %v", err)
	}
	t.Cleanup(func() {
		if err := store.Close(); err != nil {
			t.Errorf("close store: %v", err)
		}
	})
	return store
}

func TestSaveIsIdempotentByEventID(t *testing.T) {
	store := openStore(t)
	ctx := context.Background()
	n := notification.Notification{EventID: "e1", OrderID: "o1", Kind: "payment-approved", Message: "ok", CreatedAt: time.Now()}

	first, err := store.Save(ctx, n)
	if err != nil || !first {
		t.Fatalf("first save = %v, %v; want true, nil", first, err)
	}
	second, err := store.Save(ctx, n)
	if err != nil || second {
		t.Fatalf("duplicate save = %v, %v; want false, nil", second, err)
	}

	all, err := store.List(ctx, "", 10)
	if err != nil {
		t.Fatalf("list: %v", err)
	}
	if len(all) != 1 {
		t.Fatalf("stored %d rows, want 1", len(all))
	}
}

func TestListFiltersByOrderNewestFirst(t *testing.T) {
	store := openStore(t)
	ctx := context.Background()
	for _, n := range []notification.Notification{
		{EventID: "e1", OrderID: "o1", Kind: "k", Message: "first", CreatedAt: time.Now()},
		{EventID: "e2", OrderID: "o2", Kind: "k", Message: "other order", CreatedAt: time.Now()},
		{EventID: "e3", OrderID: "o1", Kind: "k", Message: "second", CreatedAt: time.Now()},
	} {
		if _, err := store.Save(ctx, n); err != nil {
			t.Fatalf("save: %v", err)
		}
	}

	got, err := store.List(ctx, "o1", 10)
	if err != nil {
		t.Fatalf("list: %v", err)
	}
	if len(got) != 2 || got[0].Message != "second" || got[1].Message != "first" {
		t.Fatalf("got %+v, want [second first]", got)
	}
}
