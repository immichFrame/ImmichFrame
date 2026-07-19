package main

import (
	"path/filepath"
	"testing"
)

func TestOutputPathKeepsProfilesSeparate(t *testing.T) {
	s := service{cfg: config{mediaPath: "/transcoded"}}
	if got, want := s.outputPath("720", "asset-id"), filepath.Join("/transcoded", "720", "asset-id.mp4"); got != want {
		t.Fatalf("output path = %q, want %q", got, want)
	}
}

func TestProfilesContainPlaybackProfile(t *testing.T) {
	if !contains([]string{"720", "1080"}, "720") || contains([]string{"720"}, "1080") {
		t.Fatal("profile membership is incorrect")
	}
}
