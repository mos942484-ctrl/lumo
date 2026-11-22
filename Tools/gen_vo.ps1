# Generates all voice-over WAV files for Lumo from the question bank, using Windows SAPI TTS.
# 100% offline — no internet, no external APIs. Files follow the fixed naming convention so
# the team can swap in human recordings by filename with zero code changes (BUILD_PROMPT §9).
# Run:  powershell -NoProfile -ExecutionPolicy Bypass -File Tools\gen_vo.ps1
# Add -Force to regenerate files that already exist (default: skip existing files, so
# re-running after a content edit leaves stale audio in place — pass -Force after edits).
# NOTE (Batch 5): the voice is still picked by first match on Zira|Hazel|Female — the
# script records no full voice name, and SAPI voice names are machine-dependent, so
# pinning to one guessed full name would break generation on other machines. If the
# team standardises on one machine/voice, pin its exact full name here.
param([switch]$Force)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Speech

$root    = Split-Path -Parent $PSScriptRoot
$bank    = Get-Content "$root\Assets\Resources\Data\levels.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$outDir  = "$root\Assets\Resources\Audio\Voice"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# ---- pick a friendly female voice if available ----
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$voices = $synth.GetInstalledVoices() | ForEach-Object { $_.VoiceInfo.Name }
Write-Output ("Installed voices: " + ($voices -join ", "))
$preferred = $voices | Where-Object { $_ -match "Zira|Hazel|Female" } | Select-Object -First 1
if ($preferred) { $synth.SelectVoice($preferred) }
$synth.Rate = -1   # slightly slower, kid-friendly
$synth.Volume = 100

$format = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(16000, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)

# ---- collect (name -> text) pairs ----
$jobs = @{}
foreach ($lv in $bank.levels) {
    $jobs["vo_intro_lvl" + $lv.id] = $lv.intro.story + " " + $lv.intro.goal
    foreach ($c in $lv.challenges) {
        $jobs[$c.vo] = $c.prompt
        $jobs["vo_fb_" + $c.id + "_ok"]  = $c.feedback.success
        $jobs["vo_fb_" + $c.id + "_no"]  = $c.feedback.retry
        if ($c.media.pictures) {
            $jobs[$c.media.voWord] = $c.media.word.ToLower()
        }
        if ($c.media.word -and $c.type -eq "spelling") {
            $jobs[$c.media.voWord] = $c.media.word.ToLower()
        }
        if ($c.media.voSentence) {
            $jobs[$c.media.voSentence] = $c.media.sentence
        }
        if ($c.words) { foreach ($w in $c.words) { $jobs["vo_word_" + $w] = $w } }
    }
}
$jobs["vo_story_intro"] = "Hello, young hero! Our magic crystal has been broken into twelve pieces. We need your help! Solve the challenges, collect the stars, and save our kingdom!"
foreach ($v in $bank.variants) {
    $jobs[$v.vo] = $v.prompt
    $jobs["vo_fb_" + $v.id + "_ok"] = $v.feedback.success
    $jobs["vo_fb_" + $v.id + "_no"] = $v.feedback.retry
    if ($v.media.voWord -and $v.media.word) { $jobs[$v.media.voWord] = $v.media.word.ToLower() }
    if ($v.media.voSentence -and $v.media.sentence) { $jobs[$v.media.voSentence] = $v.media.sentence }
    if ($v.words) { foreach ($w in $v.words) { $jobs["vo_word_" + $w] = $w } }
}

# spoken number words for math feedback clarity
$numberWords = @{
    "vo_word_one"="one"; "vo_word_two"="two"; "vo_word_three"="three"; "vo_word_four"="four";
    "vo_word_five"="five"; "vo_word_six"="six"; "vo_word_seven"="seven"; "vo_word_eight"="eight";
    "vo_word_nine"="nine"; "vo_word_ten"="ten"; "vo_word_eleven"="eleven"; "vo_word_twelve"="twelve";
    "vo_word_thirteen"="thirteen"; "vo_word_fourteen"="fourteen"; "vo_word_fifteen"="fifteen";
    "vo_word_sixteen"="sixteen"; "vo_word_seventeen"="seventeen"; "vo_word_eighteen"="eighteen";
    "vo_word_nineteen"="nineteen"; "vo_word_twenty"="twenty"
}
foreach ($k in $numberWords.Keys) { if (-not $jobs.ContainsKey($k)) { $jobs[$k] = $numberWords[$k] } }

# ---- synthesize ----
$done = 0; $skipped = 0
foreach ($name in $jobs.Keys) {
    $text = [string]$jobs[$name]
    if (-not $text.Trim()) { continue }
    $path = Join-Path $outDir ($name + ".wav")
    if ((Test-Path $path) -and -not $Force) { $skipped++; continue }
    $synth.SetOutputToWaveFile($path, $format)
    $synth.Speak($text)
    $synth.SetOutputToNull()
    $done++
}
$synth.Dispose()
Write-Output ("VO generation complete: " + $done + " new files, " + $skipped + " already existed.")
