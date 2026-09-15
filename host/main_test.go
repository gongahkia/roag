package main

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"os"
	"strings"
	"testing"
	"time"
)

func TestNotifySendsEvent(t *testing.T) {
	var got event
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path != "/event" || r.Method != http.MethodPost {
			t.Fatalf("request = %s %s", r.Method, r.URL.Path)
		}
		if r.Header.Get("Content-Type") != "application/json" {
			t.Fatalf("content type = %q", r.Header.Get("Content-Type"))
		}
		if err := json.NewDecoder(r.Body).Decode(&got); err != nil {
			t.Fatal(err)
		}
		w.WriteHeader(http.StatusOK)
	}))
	defer server.Close()

	old := os.Getenv("CODEX_BEACON_URL")
	defer os.Setenv("CODEX_BEACON_URL", old)
	if err := os.Setenv("CODEX_BEACON_URL", server.URL); err != nil {
		t.Fatal(err)
	}
	if err := notify(event{Type: "done", Project: "yuho"}); err != nil {
		t.Fatal(err)
	}
	if got.Type != "done" || got.Project != "yuho" {
		t.Fatalf("event = %+v", got)
	}
}

func TestNotifyFailsQuicklyWhenDeviceIsUnreachable(t *testing.T) {
	old := os.Getenv("CODEX_BEACON_URL")
	defer os.Setenv("CODEX_BEACON_URL", old)
	if err := os.Setenv("CODEX_BEACON_URL", "http://127.0.0.1:1"); err != nil {
		t.Fatal(err)
	}
	started := time.Now()
	err := notify(event{Type: "done", Project: "offline"})
	if err == nil {
		t.Fatal("expected an error")
	}
	if elapsed := time.Since(started); elapsed > 2*time.Second {
		t.Fatalf("notification took %s", elapsed)
	}
}

func TestBeaconURLValidation(t *testing.T) {
	old := os.Getenv("CODEX_BEACON_URL")
	defer os.Setenv("CODEX_BEACON_URL", old)
	for _, value := range []string{"", "device.local", "ftp://device.local"} {
		if err := os.Setenv("CODEX_BEACON_URL", value); err != nil {
			t.Fatal(err)
		}
		if _, err := beaconURL(); err == nil {
			t.Fatalf("expected %q to be rejected", value)
		}
	}
	if err := os.Setenv("CODEX_BEACON_URL", "http://device.local/"); err != nil {
		t.Fatal(err)
	}
	got, err := beaconURL()
	if err != nil || got != "http://device.local" {
		t.Fatalf("url = %q, err = %v", got, err)
	}
}

func TestCurrentProjectUsesDirectoryName(t *testing.T) {
	dir := t.TempDir()
	projectDir := dir + "/yuho"
	if err := os.Mkdir(projectDir, 0o755); err != nil {
		t.Fatal(err)
	}
	old, err := os.Getwd()
	if err != nil {
		t.Fatal(err)
	}
	if err := os.Chdir(projectDir); err != nil {
		t.Fatal(err)
	}
	defer os.Chdir(old)
	if got := currentProject(); !strings.EqualFold(got, "yuho") {
		t.Fatalf("project = %q", got)
	}
}
