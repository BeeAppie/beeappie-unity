# Changelog

## 1.0.1

- `BeeAppieSdk.Start` refuses a key that can't be one (spaces, or characters like the "…" in the Quickstart's
  placeholder `bak_test_…`) with a clear message, and BeeAppie stays off. Before, every call failed with "Header value
  contains invalid characters".

## 1.0.0

- First release: users and sign-in, consent, analytics events, sessions, error reports with the steps before them and
  the game's state, leaderboards with seasons, achievements (offline too), polls, announcements, feedback, feature
  surveys, support chat, remote config with feature flags and the update and maintenance status, remote content, user
  properties, user data storage (save slots and progress on every device), entitlements, promos of your other apps, data
  export and deletion, and push tokens.
- The device's identity and token are kept where the platform protects them: encrypted with the Android Keystore, in the
  iOS Keychain, encrypted with DPAPI on Windows. Elsewhere (macOS, Linux, WebGL) they stay in PlayerPrefs.
