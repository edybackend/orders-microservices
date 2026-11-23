// Package notification turns payment events into customer notifications.
package notification

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"time"
)

// Event types published by the Payments service (see contracts/events.md).
const (
	TypePaymentApproved = "PaymentApproved"
	TypePaymentRejected = "PaymentRejected"
)

// ErrUnknownEventType is returned for messages this service does not handle.
var ErrUnknownEventType = errors.New("unknown event type")

// Notification is what the customer would receive (e-mail, push...). Here it is stored and exposed over HTTP.
type Notification struct {
	ID        int64     `json:"id"`
	EventID   string    `json:"eventId"`
	OrderID   string    `json:"orderId"`
	Kind      string    `json:"kind"`
	Message   string    `json:"message"`
	CreatedAt time.Time `json:"createdAt"`
}

// Store persists notifications. Save must ignore an event id it has already stored.
type Store interface {
	Save(ctx context.Context, n Notification) (bool, error)
}

type paymentApproved struct {
	EventID string      `json:"eventId"`
	OrderID string      `json:"orderId"`
	Amount  json.Number `json:"amount"` // kept as text: money is never parsed into a float
}

type paymentRejected struct {
	EventID string `json:"eventId"`
	OrderID string `json:"orderId"`
	Reason  string `json:"reason"`
}

// Service handles one message at a time. It is safe to call it twice with the same event.
type Service struct {
	store Store
	now   func() time.Time
}

// NewService creates a Service. now is injected so tests control the clock.
func NewService(store Store, now func() time.Time) *Service {
	return &Service{store: store, now: now}
}

// Handle decodes the event and stores the matching notification.
// It returns ErrUnknownEventType or a decode error so the caller can dead-letter the message.
func (s *Service) Handle(ctx context.Context, eventType string, body []byte) error {
	n, err := s.build(eventType, body)
	if err != nil {
		return err
	}
	if _, err := s.store.Save(ctx, n); err != nil {
		return fmt.Errorf("save notification for event %s: %w", n.EventID, err)
	}
	return nil
}

func (s *Service) build(eventType string, body []byte) (Notification, error) {
	switch eventType {
	case TypePaymentApproved:
		var e paymentApproved
		if err := decode(body, &e); err != nil {
			return Notification{}, err
		}
		return s.newNotification(e.EventID, e.OrderID, "payment-approved",
			fmt.Sprintf("Payment of %s approved for order %s.", e.Amount, e.OrderID)), nil
	case TypePaymentRejected:
		var e paymentRejected
		if err := decode(body, &e); err != nil {
			return Notification{}, err
		}
		return s.newNotification(e.EventID, e.OrderID, "payment-rejected",
			fmt.Sprintf("Payment rejected for order %s: %s", e.OrderID, e.Reason)), nil
	default:
		return Notification{}, fmt.Errorf("%w: %q", ErrUnknownEventType, eventType)
	}
}

func (s *Service) newNotification(eventID, orderID, kind, message string) Notification {
	return Notification{EventID: eventID, OrderID: orderID, Kind: kind, Message: message, CreatedAt: s.now().UTC()}
}

func decode(body []byte, target interface{ validate() error }) error {
	if err := json.Unmarshal(body, target); err != nil {
		return fmt.Errorf("decode event: %w", err)
	}
	return target.validate()
}

func (e *paymentApproved) validate() error { return requireIDs(e.EventID, e.OrderID) }
func (e *paymentRejected) validate() error { return requireIDs(e.EventID, e.OrderID) }

func requireIDs(eventID, orderID string) error {
	if eventID == "" || orderID == "" {
		return errors.New("decode event: eventId and orderId are required")
	}
	return nil
}
