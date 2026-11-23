package httpapi_test

import (
	"context"
	"encoding/json"
	"io"
	"log/slog"
	"net/http"
	"net/http/httptest"
	"testing"

	"github.com/edybackend/orders-microservices/notifications/internal/httpapi"
	"github.com/edybackend/orders-microservices/notifications/internal/notification"
)

type fakeLister struct{ gotOrderID string }

func (f *fakeLister) List(_ context.Context, orderID string, _ int) ([]notification.Notification, error) {
	f.gotOrderID = orderID
	return []notification.Notification{{EventID: "e1", OrderID: orderID, Message: "ok"}}, nil
}

func TestListNotifications(t *testing.T) {
	lister := &fakeLister{}
	handler := httpapi.NewHandler(lister, "http://localhost:8080", slog.New(slog.NewTextHandler(io.Discard, nil)))

	rec := httptest.NewRecorder()
	handler.ServeHTTP(rec, httptest.NewRequest(http.MethodGet, "/notifications?orderId=o1", nil))

	if rec.Code != http.StatusOK {
		t.Fatalf("status = %d, want 200", rec.Code)
	}
	if lister.gotOrderID != "o1" {
		t.Errorf("orderId passed to storage = %q, want o1", lister.gotOrderID)
	}
	if got := rec.Header().Get("Access-Control-Allow-Origin"); got != "http://localhost:8080" {
		t.Errorf("CORS origin = %q", got)
	}
	var body []notification.Notification
	if err := json.NewDecoder(rec.Body).Decode(&body); err != nil || len(body) != 1 {
		t.Fatalf("body = %v, err = %v", body, err)
	}
}
