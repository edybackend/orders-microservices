package notification_test

import (
	"context"
	"errors"
	"testing"
	"time"

	"github.com/edybackend/orders-microservices/notifications/internal/notification"
)

type memoryStore struct {
	saved map[string]notification.Notification
}

func (m *memoryStore) Save(_ context.Context, n notification.Notification) (bool, error) {
	if _, exists := m.saved[n.EventID]; exists {
		return false, nil
	}
	m.saved[n.EventID] = n
	return true, nil
}

func TestHandle(t *testing.T) {
	fixedNow := time.Date(2026, 1, 1, 12, 0, 0, 0, time.UTC)

	tests := []struct {
		name        string
		eventType   string
		body        string
		wantErr     error // sentinel checked with errors.Is; nil means success
		wantAnyErr  bool
		wantKind    string
		wantMessage string
	}{
		{
			name:        "approved payment",
			eventType:   notification.TypePaymentApproved,
			body:        `{"eventId":"e1","orderId":"o1","amount":149.90}`,
			wantKind:    "payment-approved",
			wantMessage: "Payment of 149.90 approved for order o1.",
		},
		{
			name:        "rejected payment",
			eventType:   notification.TypePaymentRejected,
			body:        `{"eventId":"e2","orderId":"o2","reason":"Amount exceeds the approval limit of 5000.00."}`,
			wantKind:    "payment-rejected",
			wantMessage: "Payment rejected for order o2: Amount exceeds the approval limit of 5000.00.",
		},
		{name: "unknown type", eventType: "OrderPlaced", body: `{}`, wantErr: notification.ErrUnknownEventType},
		{name: "malformed json", eventType: notification.TypePaymentApproved, body: `{not json`, wantAnyErr: true},
		{name: "missing ids", eventType: notification.TypePaymentRejected, body: `{"reason":"x"}`, wantAnyErr: true},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			store := &memoryStore{saved: map[string]notification.Notification{}}
			svc := notification.NewService(store, func() time.Time { return fixedNow })

			err := svc.Handle(context.Background(), tt.eventType, []byte(tt.body))

			switch {
			case tt.wantErr != nil:
				if !errors.Is(err, tt.wantErr) {
					t.Fatalf("error = %v, want %v", err, tt.wantErr)
				}
				return
			case tt.wantAnyErr:
				if err == nil {
					t.Fatal("expected an error, got nil")
				}
				return
			case err != nil:
				t.Fatalf("unexpected error: %v", err)
			}

			if len(store.saved) != 1 {
				t.Fatalf("saved %d notifications, want 1", len(store.saved))
			}
			for _, n := range store.saved {
				if n.Kind != tt.wantKind || n.Message != tt.wantMessage || !n.CreatedAt.Equal(fixedNow) {
					t.Errorf("got %+v, want kind %q message %q", n, tt.wantKind, tt.wantMessage)
				}
			}
		})
	}
}
