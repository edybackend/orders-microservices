// Command notifications consumes payment results and exposes the generated notifications over HTTP.
package main

import (
	"context"
	"errors"
	"log/slog"
	"net/http"
	"os"
	"os/signal"
	"path/filepath"
	"syscall"
	"time"

	"golang.org/x/sync/errgroup"

	"github.com/edybackend/orders-microservices/notifications/internal/config"
	"github.com/edybackend/orders-microservices/notifications/internal/httpapi"
	"github.com/edybackend/orders-microservices/notifications/internal/messaging"
	"github.com/edybackend/orders-microservices/notifications/internal/notification"
	"github.com/edybackend/orders-microservices/notifications/internal/storage"
)

func main() {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, nil))
	if err := run(logger); err != nil {
		logger.Error("service stopped", "error", err)
		os.Exit(1)
	}
}

func run(logger *slog.Logger) error {
	cfg := config.Load()
	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	if err := os.MkdirAll(filepath.Dir(cfg.DBPath), 0o755); err != nil {
		return err
	}
	store, err := storage.Open(ctx, cfg.DBPath)
	if err != nil {
		return err
	}
	defer func() {
		if err := store.Close(); err != nil {
			logger.Error("close store", "error", err)
		}
	}()

	service := notification.NewService(store, time.Now)
	consumer := &messaging.Consumer{
		URL:         cfg.AMQPURL,
		Queue:       "notifications.payment-results",
		RoutingKeys: []string{"payment.*"},
		Handler:     service.Handle,
		Logger:      logger,
	}
	server := &http.Server{
		Addr:              cfg.HTTPAddr,
		Handler:           httpapi.NewHandler(store, cfg.AllowedOrigin, logger),
		ReadHeaderTimeout: 5 * time.Second,
	}

	g, ctx := errgroup.WithContext(ctx)
	g.Go(func() error { return consumer.Run(ctx) })
	g.Go(func() error {
		logger.Info("http listening", "addr", cfg.HTTPAddr)
		if err := server.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
			return err
		}
		return nil
	})
	g.Go(func() error {
		<-ctx.Done()
		shutdownCtx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
		defer cancel()
		return server.Shutdown(shutdownCtx)
	})
	return g.Wait()
}
