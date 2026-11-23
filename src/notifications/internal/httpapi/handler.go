// Package httpapi exposes notifications over HTTP.
package httpapi

import (
	"context"
	"encoding/json"
	"log/slog"
	"net/http"

	"github.com/edybackend/orders-microservices/notifications/internal/notification"
)

const listLimit = 50

// Lister is the only thing the HTTP layer needs from storage.
type Lister interface {
	List(ctx context.Context, orderID string, limit int) ([]notification.Notification, error)
}

// NewHandler returns the HTTP routes. allowedOrigin enables CORS for the web UI served by the Orders API.
func NewHandler(store Lister, allowedOrigin string, logger *slog.Logger) http.Handler {
	mux := http.NewServeMux()
	mux.HandleFunc("GET /health", func(w http.ResponseWriter, _ *http.Request) {
		w.Write([]byte("Healthy"))
	})
	mux.HandleFunc("GET /notifications", func(w http.ResponseWriter, r *http.Request) {
		items, err := store.List(r.Context(), r.URL.Query().Get("orderId"), listLimit)
		if err != nil {
			logger.Error("list notifications", "error", err)
			http.Error(w, "internal error", http.StatusInternalServerError)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		if err := json.NewEncoder(w).Encode(items); err != nil {
			logger.Error("encode response", "error", err)
		}
	})
	return withCORS(mux, allowedOrigin)
}

func withCORS(next http.Handler, allowedOrigin string) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Access-Control-Allow-Origin", allowedOrigin)
		w.Header().Set("Vary", "Origin")
		next.ServeHTTP(w, r)
	})
}
