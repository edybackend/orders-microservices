// Package messaging consumes payment events from RabbitMQ.
package messaging

import (
	"context"
	"errors"
	"fmt"
	"log/slog"
	"time"

	amqp "github.com/rabbitmq/amqp091-go"
)

// Topology shared with the .NET services (see contracts/events.md).
const (
	exchange           = "ecommerce.events"
	deadLetterExchange = "ecommerce.dlx"
	prefetch           = 10
)

// HandlerFunc processes one message. A returned error sends the message to the dead-letter queue.
type HandlerFunc func(ctx context.Context, eventType string, body []byte) error

// Consumer reads one queue with manual acknowledgements.
type Consumer struct {
	URL         string
	Queue       string
	RoutingKeys []string
	Handler     HandlerFunc
	Logger      *slog.Logger
}

// Run blocks until ctx is cancelled or the connection is lost.
// On connection loss it returns an error and lets the process supervisor (Docker/Kubernetes) restart it.
func (c *Consumer) Run(ctx context.Context) error {
	conn, err := dialWithRetry(ctx, c.URL, c.Logger)
	if err != nil {
		return err
	}
	defer conn.Close()

	ch, err := conn.Channel()
	if err != nil {
		return fmt.Errorf("open channel: %w", err)
	}
	defer ch.Close()

	if err := c.declare(ch); err != nil {
		return err
	}
	if err := ch.Qos(prefetch, 0, false); err != nil {
		return fmt.Errorf("set prefetch: %w", err)
	}
	deliveries, err := ch.ConsumeWithContext(ctx, c.Queue, "notifications", false, false, false, false, nil)
	if err != nil {
		return fmt.Errorf("consume %s: %w", c.Queue, err)
	}
	c.Logger.Info("listening", "queue", c.Queue)

	for {
		select {
		case <-ctx.Done():
			return nil
		case d, ok := <-deliveries:
			if !ok {
				return errors.New("delivery channel closed: connection to RabbitMQ lost")
			}
			c.handle(ctx, d)
		}
	}
}

func (c *Consumer) handle(ctx context.Context, d amqp.Delivery) {
	if err := c.Handler(ctx, d.Type, d.Body); err != nil {
		c.Logger.Error("message dead-lettered", "type", d.Type, "messageId", d.MessageId, "error", err)
		if nackErr := d.Nack(false, false); nackErr != nil {
			c.Logger.Error("nack failed", "error", nackErr)
		}
		return
	}
	if err := d.Ack(false); err != nil {
		c.Logger.Error("ack failed", "error", err)
	}
}

func (c *Consumer) declare(ch *amqp.Channel) error {
	if err := ch.ExchangeDeclare(exchange, amqp.ExchangeTopic, true, false, false, false, nil); err != nil {
		return fmt.Errorf("declare exchange: %w", err)
	}
	if err := ch.ExchangeDeclare(deadLetterExchange, amqp.ExchangeDirect, true, false, false, false, nil); err != nil {
		return fmt.Errorf("declare dead-letter exchange: %w", err)
	}
	dlq := c.Queue + ".dlq"
	if _, err := ch.QueueDeclare(dlq, true, false, false, false, nil); err != nil {
		return fmt.Errorf("declare %s: %w", dlq, err)
	}
	if err := ch.QueueBind(dlq, dlq, deadLetterExchange, false, nil); err != nil {
		return fmt.Errorf("bind %s: %w", dlq, err)
	}
	args := amqp.Table{"x-dead-letter-exchange": deadLetterExchange, "x-dead-letter-routing-key": dlq}
	if _, err := ch.QueueDeclare(c.Queue, true, false, false, false, args); err != nil {
		return fmt.Errorf("declare %s: %w", c.Queue, err)
	}
	for _, key := range c.RoutingKeys {
		if err := ch.QueueBind(c.Queue, key, exchange, false, nil); err != nil {
			return fmt.Errorf("bind %s to %s: %w", c.Queue, key, err)
		}
	}
	return nil
}

func dialWithRetry(ctx context.Context, url string, logger *slog.Logger) (*amqp.Connection, error) {
	const attempts = 10
	for i := 1; ; i++ {
		conn, err := amqp.Dial(url)
		if err == nil {
			return conn, nil
		}
		if i == attempts {
			return nil, fmt.Errorf("connect to RabbitMQ after %d attempts: %w", attempts, err)
		}
		logger.Warn("RabbitMQ not reachable, retrying", "attempt", i, "error", err)
		select {
		case <-ctx.Done():
			return nil, ctx.Err()
		case <-time.After(3 * time.Second):
		}
	}
}
