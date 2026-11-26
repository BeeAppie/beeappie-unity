# BeeAppie for Unity

Users, sign-in, analytics, sessions, error reports, leaderboards, achievements, polls, announcements, feedback, support
chat, remote config, remote content and push tokens for Unity games, without a backend of your own. Unity 2021.3 and
later, on iOS, Android, Windows, macOS and Linux.

## Install

In Unity: **Window → Package Manager → + → Add package from git URL…**, and enter

```
https://github.com/beeappie/beeappie-unity.git#1.0.0
```

(Unity needs Git installed for this.) Or add it to `Packages/manifest.json`:

```json
"com.beeappie.sdk": "https://github.com/beeappie/beeappie-unity.git#1.0.0"
```

Without Git: download `com.beeappie.sdk-1.0.0.tgz` from the
[release](https://github.com/beeappie/beeappie-unity/releases), put it in your project (e.g. in a `Packages/BeeAppie`
folder you commit), and use **+ → Add package from tarball…**. To update, change the version after `#` (or add the new
file).

Unity adds `com.unity.nuget.newtonsoft-json` with it. To try it, import the **Quickstart** sample from the package's
page in the Package Manager.

## Start

Once, from the main thread, e.g. in your first scene's `Awake`:

```csharp
using BeeAppie.Unity;

BeeAppieSdk.Start("your-app-key");
```

That is all: BeeAppie restores this device's player (or creates one) in the background, opens a session while the game
is played and closes it when it is paused, and reports exceptions nobody caught (Unity logs them and carries on). If the
game starts offline, BeeAppie starts when it can. Use your `bak_test_…` key in development builds: their data stays under
**Test** in the dashboard.

## Privacy

Nothing is collected until the player agrees:

```csharp
await BeeAppieSdk.SetConsentAsync(new Consent { EventPermission = true, IssuesPermission = true, SessionPermission = true });
```

No user is created when the app starts: remote config, news and polls work with the app key alone, and a user appears
when someone allows something (as above) or first uses a feature that needs one (chat, feedback, a vote, a score, signing
in). "No thanks" before then stays on the device. Call `await BeeAppieSdk.EnsureUserAsync()` if the app needs the user's id earlier.

`BeeAppieSdk.DeleteUserAsync()` permanently erases the player and all their data; `BeeAppieSdk.ExportUserDataAsync()`
saves a ZIP of it and returns its path.

## Analytics and errors

```csharp
BeeAppieSdk.TrackEvent("level_completed", new Dictionary<string, string> { ["level"] = "3" });
BeeAppieSdk.ReportException(exception);
BeeAppieSdk.AddBreadcrumb("Shop opened");
BeeAppieSdk.Screen("Inventory"); // scenes are recorded already
```

Events and error reports are queued on the device and sent in batches (every 30 seconds, then every 5 minutes once the game has been played 10 minutes, or when 100 are waiting, or when the game is paused), so playing offline loses nothing. Errors are
grouped by the first method of your own code in their stack.

Each report also carries the game's state when it happened: how long it had run, whether it was paused, the device's
memory and whether Unity reported it low, free storage, where it was installed from, orientation and screen, and whether it
was the Editor, an emulator or a likely rooted or modified build. On Android the build number is the bundle version code; on
iOS and desktops Unity can't read it, so set it: `o.AppBuild = "57"` (e.g. from `PlayerSettings.iOS.buildNumber` at build
time).

## Players and sign-in

```csharp
var result = await BeeAppieSdk.SignInAsync(idToken); // Sign in with Apple, Google Play Games via Firebase, your own login…
await BeeAppieSdk.SignOutAsync();                     // the device continues with a new anonymous player
await BeeAppieSdk.SetUserPropertiesAsync(new Dictionary<string, object?> { ["plan"] = "gold" });
BeeAppieSdk.UserChanged += player => Reload();
BeeAppieSdk.Suspended += s => ShowSuspended(s.Until, s.Reason); // suspended from the dashboard: nothing works until it ends
```

## Achievements

Create them in the dashboard (App › Achievements) with a key, a goal (1 for a one-off), an icon and, if you like, other languages. Each call applies at once to a copy on the device and is sent later in a batch with the time it happened, so it works offline and never throws; sending creates the user, as other features do. The UI package shows "Achievement unlocked" at the top of the screen by itself
(`BeeAppieUI.CelebrateAchievements = false` to turn it off).

```csharp
BeeAppieSdk.Achievements.Unlock("first-win");
BeeAppieSdk.Achievements.Increment("play-100-games");          // +1; reaching the goal unlocks it
BeeAppieSdk.Achievements.SetProgress("best-level", 12);        // never goes down
bool done = BeeAppieSdk.Achievements.IsUnlocked("first-win");  // from the copy on the device
var all = BeeAppieSdk.Achievements.Current;                    // in the device's language, with progress and dates
BeeAppieSdk.Achievements.Unlocked += a => Celebrate(a);
```

With **Send achievements only with consent** on in the app's settings, nothing leaves the device (and no user is made for achievements) until the player allows usage statistics; the page, progress and message still work. `Client.UnlockAchievementAsync` and the other raw calls stay for when
you need BeeAppie's answer.

## Scores, feedback and news offline

Scores, feedback and announcement views, taps and confirmations are kept on the device and sent once BeeAppie can be reached, so they work offline and never throw. A score counts in the season it was made in if it arrives within 7 days. Leaderboards and the first 50 announcements are kept for offline reading. News, polls and push notifications come in the device's language when your team added it in the dashboard (otherwise in the app's main language): the SDK sends the language for you.

**Times and "lower is better":** a leaderboard can rank the lowest score first (races, moves) and show scores as times, both set in the dashboard. For a time, submit milliseconds; entries carry `ScoreText` ("1:23.45") to show as it is, `ScoreFormats.Format` formats one you hold, and the player's best (also the one waiting offline) follows the leaderboard's order.

```csharp
BeeAppieSdk.Leaderboards.SubmitScore("high-score", 1250);            // with its time; sent now or later
var board = await BeeAppieSdk.Leaderboards.LoadAsync("high-score");  // offline: the last copy (IsUpToDate false), PendingBest
var all = await BeeAppieSdk.Leaderboards.ListAsync();                // the game's leaderboards; offline: the last list
var past = await BeeAppieSdk.Leaderboards.SeasonsAsync("daily");    // past seasons, named; offline: the last list
var sent = await BeeAppieSdk.Feedback.SendAsync(FeedbackCategory.Problem, "Lag on level 4", 2); // false: waits on the device
var news = await BeeAppieSdk.LoadCommunicationsAsync();              // news.Items; offline: the last copy
await BeeAppieSdk.MarkCommunicationAcknowledgedAsync(news.Items[0].Id); // also offline
```

They are sent with the events (the SDK's tick) and when the game resumes; call them from the main thread. `Client.SubmitScoreAsync` and `Client.SubmitFeedbackAsync` stay for when you need BeeAppie's answer at once.

**Seasons:** a leaderboard with repeating seasons keeps past ones to look back at. `SeasonsAsync` lists them (newest first, kept for offline); each has `Name`: your team's name for it from the dashboard ("Yesterday", "Round 12"), filled in and in the player's language, or none: then use your own words, or let the ready-made leaderboard screen say Today / Yesterday, This week, "October 2026"… Load one with its key as the season.

## Leaderboards and the rest

`BeeAppieSdk.Client` is the API for the current player:

```csharp
var entry = await BeeAppieSdk.Client.SubmitScoreAsync("high-score", 1200);
var top = await BeeAppieSdk.Client.GetTopEntriesAsync("high-score", 10);


var polls = await BeeAppieSdk.Client.GetPollsAsync();
var news = await BeeAppieSdk.Client.GetCommunicationsAsync();
var chat = await BeeAppieSdk.Client.SubmitFeedbackAsync(FeedbackCategory.Idea, "More levels!", rating: 5);
await BeeAppieSdk.Client.SendMessageAsync(chat.Id, "Thanks!");
```

Call them from the main thread. Each throws a `BeeAppieApiException` on failure: branch on `Code` (e.g.
`"consent_required"`); `Status` is 0 when BeeAppie could not be reached.

Also on the client: one leaderboard (`GetRankingAsync`) and deleting an entry, one poll and its results, marking
announcements acknowledged or clicked, renaming, closing and deleting conversations, image messages
(`SendMessageAsync(id, text, imageBytes)`, `GetMessageImageAsync`), linking and unlinking sign-in accounts, and erasing the
player's events, issues or sessions.

### Real-time support chat

```csharp
var chat = await BeeAppieSdk.ChatAsync(); // shared and connected; paused with the game, reconnected on resume
await chat.JoinConversationAsync(conversation.Id);
chat.MessageReceived += message => Show(message);           // on the main thread
chat.Typing += (conversationId, fromTeam, isTyping) => ShowTyping(isTyping);
chat.Reconnected += () => Refetch();                         // messages sent while it was down are not replayed
```

Not available on WebGL (no streaming HTTP there): fetch a conversation's messages again to see the team's replies.

Players can say no to conversations started by your team:

```csharp
await BeeAppieSdk.SetTeamChatsAllowedAsync(false); // or true; BeeAppieSdk.TeamChatsAllowed reads it
```

Your team can't start a conversation with them then; they can still start one, and get your replies.

## Feature surveys

One question about a feature, set up under Surveys in the dashboard and shown by its key, as a card over the game (uGUI on
a Canvas of its own, built in code):

```csharp
using BeeAppie.Unity.UI;

var result = await BeeAppieUI.ShowSurveyAsync("level-value", SurveyPosition.Bottom);  // Answered, Dismissed or NotShown
```

To draw your own, `BeeAppieSdk.GetSurveyAsync(key)` says whether to ask, then `SurveyShown`, `AnswerSurvey` or
`DismissSurvey`.

When there is nothing to ask (not live, not for this platform or version, answered already, asked lately, offline before
surveys were fetched, a plan without surveys) the card returns "not shown", never
an error. Each install is asked once (or again on a new version or after some days, as set in the dashboard), and at
most one survey every few days across all of them; the close button counts as asked. Answers are anonymous unless a user
already exists; a low answer with a comment also arrives under Feedback, so you can reply. Feature surveys are included
in paid plans.

## Promos of your other apps

Once a promo is approved for one of your apps (dashboard › App › Promos), your other games can show it with the
ready-made uGUI views (built in code: no prefab, TextMesh Pro or imported assets):

```csharp
using BeeAppie.Unity.UI;

// On an object under a Canvas: e.g. anchored to the bottom and stretched across it.
var banner = bannerObject.AddComponent<PromoBannerView>();    // full width, 50–90 points high
var card = cardObject.AddComponent<PromoCardView>();          // 300×250 points (CardSize for another size)
banner.NoPromo += () => { };                                  // nothing to show: the view takes no space

await BeeAppieUI.ShowFullscreenPromoAsync();                  // e.g. between levels; false when there was nothing to show
```

The views load, rotate (every minute, while on screen) and count views and taps on their own: a view counts when at
least half of the promo is on screen (inside the screen and any scroll view's mask) for a second with the game in
focus, at least 200 × 40 points for a banner or 120 × 100 for a card, and not faded out by a CanvasGroup; a tap opens
the app's store listing. Sizes are in points, whatever your Canvas Scaler. Tapping the "Also
by us" label lets players report a promo. `BeeAppieUI.Appearance = BeeAppieAppearance.Dark` for dark promos. They need
an EventSystem in the scene (one is added when the scene has none and the legacy Input Manager is on).

The same app shows at most 3 times per game run; a fullscreen at most once every few minutes (set in the dashboard),
never on the game's first launch. Promos need no player or consent: they send no player, device or advertising id, only
a random key for the game run, kept in memory. None of these calls throws.

Anonymous counts: if you chose to count active and new installs for the game (a Yes or No when you add it in the
dashboard, later in its settings), each open is counted with no id, for everyone, whatever the player chose on the consent
screen. The device keeps only the day it was last counted and the week it was installed. Offer a
"Count my visits anonymously" switch on your privacy screen and call `BeeAppieSdk.SetAnonymousCountingEnabled(false)`.

Use your `bak_test_…` key in development builds: its promos are marked "Test", as test ads are, and their views, taps
and reports don't count. In the Unity Editor, an Android emulator or the iOS simulator that holds for the live key too.
Only the ready-made views show promos: they check what is on screen before a view counts.

## Remote config, feature flags and maintenance

```csharp
var config = BeeAppieSdk.Config; // the last values fetched, kept on the device: usable from the first frame
if (config.IsEnabled("double_xp")) { … }
var lives = config.GetLong("lives", 3);

if (config.Status.UpdateRequired) Application.OpenURL(config.Status.StoreUrl);
if (config.Status.MaintenanceMessage != null) ShowMaintenance(config.Status.MaintenanceMessage);
BeeAppieSdk.ConfigChanged += c => Apply(c);
```

Values are fetched at start, when the player changes and when the game resumes after five minutes;
`await BeeAppieSdk.RefreshConfigAsync()` fetches them now.

## Remote content

Lists your team writes in the dashboard under **Remote content** (quotes, tips, levels, word lists), imported from a
spreadsheet or edited item by item, then published without a new release. Each item is any JSON under a stable key; your
game decides what the fields mean (the collection's **Code** tab writes the model). Nothing downloads until the game asks;
after the first time only what changed is sent, and a copy stays on the device for offline. It needs no user and no
consent. Anyone with your app can read it: never put passwords, keys or private data in it.

```csharp
[Serializable] public class Level { public string grid; public int moves; }

var content = await BeeAppieSdk.Content.GetAsync<Level>("levels-easy"); // timeout and forceRefresh optional
foreach (var item in content.Items)                                     // Key and Value
    Load(item.Value.grid);
if (content.Items.Count == 0 && !content.IsUpToDate) { /* first launch offline: your built-in copy */ }
var raw = await BeeAppieSdk.Content.GetAsync("levels-easy");             // JToken items
var cached = BeeAppieSdk.Content.GetCached<Level>("levels-easy");        // the device copy, no request
```

Items are read with Newtonsoft.Json, so fields named as in the JSON work. A call within 5 minutes of the last one for the same collection answers from the device without a request
(`forceRefresh` checks anyway); with a timeout you get the device copy when it runs out, while the download finishes in
the background. It never throws: offline, a collection that isn't published, or the month's downloads used up give the
copy on the device (empty the first time) with `IsUpToDate` false. An item that doesn't fit your type is skipped and
logged. Items have no order: sort them in the app. For the very first launch offline, ship a copy from **Export** in your
game.

**Release on a date** (a daily puzzle, a word of the day): give an item an **Available from** time in the dashboard, or
`_availableFrom` when importing, and publish it ahead. Until then BeeAppie doesn't send the item or its key, whatever
the device's clock says; at its time it arrives as a change. `content.NextAt` says when the next one is due (null when
nothing is scheduled; never what it is), and once it has passed the 5-minute check no longer holds a call back. For
example `if (content.NextAt is { } next) ShowCountdown(next);`. Once released, everything in a collection is readable by
anyone with your game, as before.

## Entitlements

Access your team grants in the dashboard (a player's page, a conversation) or from your server, e.g. `pro` for someone who
bought your game on another platform. They add to your store purchases and never replace them: revoking one in BeeAppie never touches a purchase, so never lock
a player out because BeeAppie does not list an entitlement.

```csharp
var isPro = store.Owns("pro") || BeeAppieSdk.HasEntitlement("pro"); // kept on the device, works offline
BeeAppieSdk.EntitlementsChanged += e => isPro = store.Owns("pro") || e.Has("pro");
await BeeAppieSdk.RefreshEntitlementsAsync(); // e.g. on "Restore purchases"; fetched at start and when the game resumes
```

## User data

Small JSON objects your game stores for each **signed-in** player (save slots, progress, settings, unlocked items), the same
on all their devices and back after a reinstall. Turn on **User data** in the app's settings in the dashboard (paid
plans). Reads come from the copy on the device, so they work offline; writes apply at once and are sent when BeeAppie
can be reached (last write wins). Writes made before the player signs in wait and are sent once they do.

It is encrypted in transit and at rest, but not end-to-end: BeeAppie's servers can read it, and it is included in your
game's data export. Your team's dashboard shows how much each player stores, not what. Don't store passwords, keys,
health or financial data there; if you must, encrypt it in your game before saving.

```csharp
public class SaveSlot { public int Level { get; set; } public int Coins { get; set; } }

var data = BeeAppieSdk.UserData;
data.Put("saves", "slot-1", new SaveSlot { Level = 3, Coins = 120 });
var slot = data.Get<SaveSlot>("saves", "slot-1"); // or Get("saves", "slot-1") for a JObject
var slots = data.List<SaveSlot>("saves");         // every item of a collection, by key
data.Delete("saves", "slot-3");
data.Changed += Reload;                           // another device changed something
data.WriteRefused += r => Debug.Log(r.Error);     // e.g. item_too_large, user_storage_full
```

## Push notifications

BeeAppie sends the notifications; your push plugin receives them. Give BeeAppie the device's token, e.g. from Firebase
Cloud Messaging for Unity: the FCM registration token on Android, the APNs token (hex) on iOS.

```csharp
BeeAppieSdk.SetPushToken(token);
// When the player taps a notification: counts BeeAppie's as opened, and raises PushOpened with its data.
BeeAppieSdk.HandlePushOpened(data);
BeeAppieSdk.PushOpened += data => { if (data.TryGetValue("conversationId", out var id)) OpenChat(id); };
```

There are no ready-made screens in Unity, so the game opens its own. When a notification was sent with *When tapped,
open*, its data has `beeappie_screen` (`poll`, `communication`, `leaderboard`, `polls`, `communications`, `rankings`,
`achievements`, `feedback`, `support` or `profile`) and, for `poll`, `communication` and `leaderboard`, the item's id
in `beeappie_screen_id`.

Players can turn BeeAppie's notifications off in your settings and on again later; tell BeeAppie what the phone allows, from
your push plugin, at startup and when the game comes back to the foreground:

```csharp
await BeeAppieSdk.SetPushNotificationsEnabledAsync(false); // or true; the token is kept, so on again works at once
BeeAppieSdk.SetPushPermission("Denied");                   // "Allowed", "Provisional" (iOS), "Denied" or "Unknown"
bool on = BeeAppieSdk.PushNotificationsAllowed;            // turned on in the game, and not blocked in the settings
```

BeeAppie sends nothing to a device turned off in the game or blocked in the settings, and those devices use none of the push
allowance.

## Options

```csharp
BeeAppieSdk.Start("your-app-key", o =>
{
    o.AppVersion = "1.2.0";     // defaults to Application.version
    o.CaptureErrors = false;    // when the game already has a crash reporter
    o.Log = true;               // writes what BeeAppie does to the console
});
```
