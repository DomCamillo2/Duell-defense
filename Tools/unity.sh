#!/bin/bash
set -euo pipefail
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
EDITOR="${DEFENSE_UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.28f1/Unity.app/Contents/MacOS/Unity}"
mkdir -p "$PROJECT/TestResults" "$PROJECT/Builds"
if [ ! -x "$EDITOR" ]; then
  echo "Unity 6000.3.28f1 fehlt. DEFENSE_UNITY_EDITOR auf das passende Editor-Programm setzen." >&2
  exit 2
fi
ACTION="${1:-prepare}"
case "$ACTION" in
  refresh-art) exec "$EDITOR" -batchmode -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.RefreshIntegratedArt -logFile "$PROJECT/TestResults/refresh-art.log" ;;
  prepare) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.Prepare -logFile "$PROJECT/TestResults/prepare.log" ;;
  android) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -buildTarget Android -executeMethod DefenseDuell.Editor.BuildEntry.Android -logFile "$PROJECT/TestResults/android-build.log" ;;
  mac) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOS -logFile "$PROJECT/TestResults/mac-build.log" ;;
  mac-3d) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOS3D -logFile "$PROJECT/TestResults/mac-3d-build.log" ;;
  preview) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSPreview -logFile "$PROJECT/TestResults/preview-build.log" ;;
  next) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSNext -logFile "$PROJECT/TestResults/next-build.log" ;;
  candidate) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSCandidate -logFile "$PROJECT/TestResults/candidate-build.log" ;;
  portraits) exec "$EDITOR" -batchmode -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.ModelPortraitBaker.Bake -logFile "$PROJECT/TestResults/portraits.log" ;;
  verify-portraits) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.ModelPortraitBaker.Verify -logFile "$PROJECT/TestResults/verify-portraits.log" ;;
  art-review) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSArtReview -logFile "$PROJECT/TestResults/art-review-build.log" ;;
  speed-review) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSSpeedReview -logFile "$PROJECT/TestResults/speed-review-build.log" ;;
  wave-review) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSWaveReview -logFile "$PROJECT/TestResults/wave-review-build.log" ;;
  hud-review) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSHudReview -logFile "$PROJECT/TestResults/hud-review-build.log" ;;
  immersive-review) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSImmersiveReview -logFile "$PROJECT/TestResults/immersive-review-build.log" ;;
  fullscreen-review) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSFullscreenReview -logFile "$PROJECT/TestResults/fullscreen-review-build.log" ;;
  economy-review) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSEconomyReview -logFile "$PROJECT/TestResults/economy-review-build.log" ;;
  economy-final-review) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSEconomyFinalReview -logFile "$PROJECT/TestResults/economy-final-review-build.log" ;;
  studio-review) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSStudioReview -logFile "$PROJECT/TestResults/studio-review-build.log" ;;
  connection-review) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSConnectionReview -logFile "$PROJECT/TestResults/connection-review-build.log" ;;
  update16-review) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSBalanceReview -logFile "$PROJECT/TestResults/update16-review-build.log" ;;
  all-features-review) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSAllFeaturesReview -logFile "$PROJECT/TestResults/all-features-review-build.log" ;;
  waves200-review) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSWaves200Review -logFile "$PROJECT/TestResults/waves200-review-build.log" ;;
  host-rules-review) exec "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod DefenseDuell.Editor.BuildEntry.MacOSHostRulesReview -logFile "$PROJECT/TestResults/host-rules-review-build.log" ;;
  edit-tests) exec "$EDITOR" -batchmode -nographics -projectPath "$PROJECT" -runTests -testPlatform EditMode -testResults "$PROJECT/TestResults/editmode.xml" -logFile "$PROJECT/TestResults/editmode.log" ;;
  play-tests) exec "$EDITOR" -batchmode -projectPath "$PROJECT" -runTests -testPlatform PlayMode -testResults "$PROJECT/TestResults/playmode.xml" -logFile "$PROJECT/TestResults/playmode.log" ;;
  open) exec "$EDITOR" -projectPath "$PROJECT" ;;
  *) echo "Usage: bash Tools/unity.sh refresh-art|prepare|portraits|verify-portraits|android|mac|mac-3d|preview|next|candidate|art-review|speed-review|wave-review|hud-review|immersive-review|fullscreen-review|economy-review|economy-final-review|studio-review|connection-review|host-rules-review|waves200-review|all-features-review|edit-tests|play-tests|open" >&2; exit 2 ;;
esac
