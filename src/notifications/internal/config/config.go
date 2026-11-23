// Package config reads settings from environment variables, with defaults for local runs.
package config

import "os"

// Config holds the runtime settings of the service.
type Config struct {
	AMQPURL       string
	DBPath        string
	HTTPAddr      string
	AllowedOrigin string
}

// Load reads the environment.
func Load() Config {
	return Config{
		AMQPURL:       env("AMQP_URL", "amqp://guest:guest@localhost:5672/"),
		DBPath:        env("DB_PATH", "data/notifications.db"),
		HTTPAddr:      env("HTTP_ADDR", ":8081"),
		AllowedOrigin: env("ALLOWED_ORIGIN", "http://localhost:8080"),
	}
}

func env(key, fallback string) string {
	if v, ok := os.LookupEnv(key); ok && v != "" {
		return v
	}
	return fallback
}
