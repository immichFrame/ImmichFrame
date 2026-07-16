package main

import (
	"context"
	"database/sql"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"sort"
	"strconv"
	"strings"
	"sync"
	"time"

	_ "github.com/mattn/go-sqlite3"
)

type config struct {
	immichURL, apiKey, databasePath, mediaPath, playbackProfile string
	profiles                                                    []string
	port                                                        string
}

type service struct {
	cfg    config
	db     *sql.DB
	client *http.Client
	wake   chan struct{}
	mu     sync.Mutex
}

type resolveRequest struct {
	AssetID         string `json:"assetId"`
	Checksum        string `json:"checksum"`
	PlaybackProfile string `json:"playbackProfile"`
}

func main() {
	cfg, err := loadConfig()
	if err != nil { log.Fatal(err) }
	if err = os.MkdirAll(filepath.Dir(cfg.databasePath), 0755); err != nil { log.Fatal(err) }
	if err = os.MkdirAll(cfg.mediaPath, 0755); err != nil { log.Fatal(err) }
	db, err := sql.Open("sqlite3", cfg.databasePath+"?_busy_timeout=5000&_journal_mode=WAL")
	if err != nil { log.Fatal(err) }
	s := &service{cfg: cfg, db: db, client: &http.Client{Timeout: 30 * time.Minute}, wake: make(chan struct{}, 1)}
	if err := s.migrate(); err != nil { log.Fatal(err) }
	go s.worker()

	mux := http.NewServeMux()
	mux.HandleFunc("GET /healthz", func(w http.ResponseWriter, _ *http.Request) { w.WriteHeader(http.StatusOK) })
	mux.HandleFunc("POST /v1/resolve", s.resolve)
	log.Printf("immichframe-transcoder listening on :%s; profiles=%s, playback=%s", cfg.port, strings.Join(cfg.profiles, ","), cfg.playbackProfile)
	log.Fatal(http.ListenAndServe(":"+cfg.port, mux))
}

func loadConfig() (config, error) {
	c := config{immichURL: strings.TrimRight(os.Getenv("IMMICH_URL"), "/"), apiKey: os.Getenv("IMMICH_API_KEY"), databasePath: env("TRANSCODER_DB_PATH", "/data/transcoder.db"), mediaPath: env("TRANSCODER_MEDIA_PATH", "/transcoded"), playbackProfile: env("TRANSCODER_PLAYBACK_PROFILE", "720"), port: env("PORT", "8090")}
	if c.immichURL == "" || c.apiKey == "" { return c, errors.New("IMMICH_URL and IMMICH_API_KEY are required") }
	for _, profile := range strings.Split(env("TRANSCODER_PROFILES", "720"), ",") {
		profile = strings.TrimSpace(profile)
		if _, err := strconv.Atoi(profile); err != nil || profile == "" { return c, fmt.Errorf("invalid TRANSCODER_PROFILES value %q", profile) }
		c.profiles = append(c.profiles, profile)
	}
	if !contains(c.profiles, c.playbackProfile) { return c, fmt.Errorf("TRANSCODER_PLAYBACK_PROFILE %q is not enabled", c.playbackProfile) }
	return c, nil
}
func env(name, fallback string) string { if v := os.Getenv(name); v != "" { return v }; return fallback }
func contains(items []string, item string) bool { for _, v := range items { if v == item { return true } }; return false }

func (s *service) migrate() error {
	_, err := s.db.Exec(`
CREATE TABLE IF NOT EXISTS assets (
 asset_id TEXT NOT NULL, checksum TEXT NOT NULL, state TEXT NOT NULL, codec TEXT, error TEXT, updated_at INTEGER NOT NULL,
 PRIMARY KEY(asset_id, checksum)
);
CREATE TABLE IF NOT EXISTS jobs (
 asset_id TEXT NOT NULL, checksum TEXT NOT NULL, profile TEXT NOT NULL, priority INTEGER NOT NULL DEFAULT 10,
 state TEXT NOT NULL DEFAULT 'queued', attempts INTEGER NOT NULL DEFAULT 0, error TEXT, created_at INTEGER NOT NULL, updated_at INTEGER NOT NULL,
 PRIMARY KEY(asset_id, checksum, profile)
);
CREATE INDEX IF NOT EXISTS jobs_next ON jobs(state, priority DESC, created_at ASC);`)
	return err
}

func (s *service) resolve(w http.ResponseWriter, r *http.Request) {
	defer r.Body.Close()
	var req resolveRequest
	if err := json.NewDecoder(http.MaxBytesReader(w, r.Body, 4096)).Decode(&req); err != nil || req.AssetID == "" || req.Checksum == "" {
		http.Error(w, "assetId and checksum are required", http.StatusBadRequest); return
	}
	profile := req.PlaybackProfile
	if profile == "" { profile = s.cfg.playbackProfile }
	if !contains(s.cfg.profiles, profile) { http.Error(w, "unknown playback profile", http.StatusBadRequest); return }
	status, err := s.resolveAsset(r.Context(), req.AssetID, req.Checksum, profile)
	if err != nil { log.Printf("resolve %s: %v", req.AssetID, err); http.Error(w, "transcoder unavailable", http.StatusServiceUnavailable); return }
	w.Header().Set("Content-Type", "application/json")
	_ = json.NewEncoder(w).Encode(map[string]string{"status": status})
}

func (s *service) resolveAsset(ctx context.Context, assetID, checksum, playbackProfile string) (string, error) {
	var state string
	err := s.db.QueryRowContext(ctx, "SELECT state FROM assets WHERE asset_id=? AND checksum=?", assetID, checksum).Scan(&state)
	if err == nil && state == "direct" { return "direct", nil }
	path := s.outputPath(playbackProfile, assetID)
	if err == nil && state == "ready" && fileExists(path) { return "ready", nil }
	now := time.Now().Unix()
	tx, err := s.db.BeginTx(ctx, nil); if err != nil { return "", err }; defer tx.Rollback()
	if _, err = tx.ExecContext(ctx, "INSERT INTO assets(asset_id,checksum,state,updated_at) VALUES(?,?, 'queued', ?) ON CONFLICT(asset_id,checksum) DO UPDATE SET state=CASE WHEN assets.state='direct' THEN 'direct' ELSE 'queued' END, updated_at=excluded.updated_at", assetID, checksum, now); err != nil { return "", err }
	for _, profile := range s.cfg.profiles {
		priority := 10; if profile == playbackProfile { priority = 100 }
		_, err = tx.ExecContext(ctx, "INSERT INTO jobs(asset_id,checksum,profile,priority,state,created_at,updated_at) VALUES(?,?,?,?, 'queued',?,?) ON CONFLICT(asset_id,checksum,profile) DO UPDATE SET priority=MAX(jobs.priority, excluded.priority), state=CASE WHEN jobs.state IN ('failed','done') THEN jobs.state ELSE 'queued' END, updated_at=excluded.updated_at", assetID, checksum, profile, priority, now, now)
		if err != nil { return "", err }
	}
	if err = tx.Commit(); err != nil { return "", err }
	select { case s.wake <- struct{}{}: default: }
	return "pending", nil
}

type job struct { assetID, checksum, profile string; attempts int }
func (s *service) worker() {
	for {
		job, err := s.nextJob(context.Background())
		if err != nil { log.Printf("job query: %v", err); time.Sleep(5*time.Second); continue }
		if job == nil { select { case <-s.wake: case <-time.After(30*time.Second): }; continue }
		if err := s.process(*job); err != nil { log.Printf("transcode %s/%s: %v", job.assetID, job.profile, err); s.fail(*job, err) }
	}
}
func (s *service) nextJob(ctx context.Context) (*job, error) {
	tx, err := s.db.BeginTx(ctx, nil); if err != nil { return nil, err }; defer tx.Rollback()
	var j job
	err = tx.QueryRowContext(ctx, "SELECT asset_id,checksum,profile,attempts FROM jobs WHERE state='queued' ORDER BY priority DESC,created_at ASC LIMIT 1").Scan(&j.assetID,&j.checksum,&j.profile,&j.attempts)
	if errors.Is(err, sql.ErrNoRows) { return nil, nil }; if err != nil { return nil, err }
	_, err = tx.ExecContext(ctx, "UPDATE jobs SET state='running', attempts=attempts+1, updated_at=? WHERE asset_id=? AND checksum=? AND profile=?", time.Now().Unix(),j.assetID,j.checksum,j.profile); if err != nil { return nil, err }
	_, err = tx.ExecContext(ctx, "UPDATE assets SET state='running',updated_at=? WHERE asset_id=? AND checksum=?", time.Now().Unix(),j.assetID,j.checksum); if err != nil { return nil, err }
	return &j, tx.Commit()
}
func (s *service) process(j job) error {
	if fileExists(s.outputPath(j.profile,j.assetID)) { return s.done(j, "ready") }
	tmp, err := os.CreateTemp("", "immichframe-source-*.video"); if err != nil { return err }; source := tmp.Name(); defer os.Remove(source); defer tmp.Close()
	if err = s.download(j.assetID, tmp); err != nil { return err }; if err = tmp.Close(); err != nil { return err }
	codec, err := probe(source); if err != nil { return err }
	if strings.ToLower(codec) != "av1" {
		_, err = s.db.Exec("UPDATE assets SET state='direct',codec=?,updated_at=? WHERE asset_id=? AND checksum=?",codec,time.Now().Unix(),j.assetID,j.checksum)
		if err == nil { _, err = s.db.Exec("UPDATE jobs SET state='done',updated_at=? WHERE asset_id=? AND checksum=?",time.Now().Unix(),j.assetID,j.checksum) }; return err
	}
	output := s.outputPath(j.profile,j.assetID); if err = os.MkdirAll(filepath.Dir(output),0755); err != nil { return err }
	partial := output+".partial"; _ = os.Remove(partial); defer os.Remove(partial)
	height, _ := strconv.Atoi(j.profile)
	cmd := exec.Command("ffmpeg", "-nostdin", "-y", "-i", source, "-map", "0:v:0", "-map", "0:a?", "-vf", fmt.Sprintf("scale=-2:min(%d,ih):force_original_aspect_ratio=decrease",height), "-c:v", "libx264", "-preset", "veryfast", "-crf", "23", "-pix_fmt", "yuv420p", "-c:a", "aac", "-movflags", "+faststart", partial)
	if out, e := cmd.CombinedOutput(); e != nil { return fmt.Errorf("ffmpeg: %w: %s",e,strings.TrimSpace(string(out))) }
	if err = os.Rename(partial,output); err != nil { return err }
	return s.done(j,"ready")
}
func (s *service) download(assetID string, output io.Writer) error {
	req, err := http.NewRequest(http.MethodGet, s.cfg.immichURL+"/api/assets/"+assetID+"/original", nil); if err != nil { return err }
	req.Header.Set("x-api-key",s.cfg.apiKey)
	resp, err := s.client.Do(req); if err != nil { return err }; defer resp.Body.Close()
	if resp.StatusCode < 200 || resp.StatusCode > 299 { return fmt.Errorf("Immich download returned %s",resp.Status) }
	_, err = io.Copy(output,resp.Body); return err
}
func probe(path string) (string,error) { out,err := exec.Command("ffprobe","-v","error","-select_streams","v:0","-show_entries","stream=codec_name","-of","default=nokey=1:noprint_wrappers=1",path).Output(); if err != nil{return "",err}; codec:=strings.TrimSpace(string(out)); if codec=="" {return "",errors.New("no video stream")}; return codec,nil }
func (s *service) outputPath(profile, assetID string) string { return filepath.Join(s.cfg.mediaPath,profile,assetID+".mp4") }
func (s *service) done(j job, state string) error { _,err:=s.db.Exec("UPDATE jobs SET state='done',error=NULL,updated_at=? WHERE asset_id=? AND checksum=? AND profile=?",time.Now().Unix(),j.assetID,j.checksum,j.profile); if err != nil{return err}; _,err=s.db.Exec("UPDATE assets SET state=?,codec='av1',error=NULL,updated_at=? WHERE asset_id=? AND checksum=?",state,time.Now().Unix(),j.assetID,j.checksum); return err }
func (s *service) fail(j job, cause error) { _,err:=s.db.Exec("UPDATE jobs SET state='failed',error=?,updated_at=? WHERE asset_id=? AND checksum=? AND profile=?",cause.Error(),time.Now().Unix(),j.assetID,j.checksum,j.profile); if err!=nil {log.Printf("record failure: %v",err)}; _,_=s.db.Exec("UPDATE assets SET state='failed',error=?,updated_at=? WHERE asset_id=? AND checksum=?",cause.Error(),time.Now().Unix(),j.assetID,j.checksum) }
func fileExists(path string) bool { info,err:=os.Stat(path); return err==nil && info.Mode().IsRegular() }

// Keep deterministic ordering available for tests and future administrative callers.
func orderedProfiles(profiles []string) []string { result:=append([]string(nil),profiles...); sort.Strings(result); return result }
