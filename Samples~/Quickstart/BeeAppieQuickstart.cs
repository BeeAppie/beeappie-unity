using System.Collections.Generic;
using BeeAppie.Unity;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Starts BeeAppie, asks for consent, tracks an event, submits a score, unlocks an achievement, stores the player's settings
/// and checks an entitlement. Put it on an object in your first scene, paste your app key, and press Play: the data appears
/// in the dashboard (under Test with a test key). Create a leaderboard "high-score", an achievement "first-win" and an
/// entitlement "premium" in the dashboard first, and turn on User data.
/// </summary>
public class BeeAppieQuickstart : MonoBehaviour
{
    [SerializeField] private string appKey = "bak_test_…";

    private void Awake() => BeeAppieSdk.Start(appKey, o => o.Log = true);

    private async void Start()
    {
        if (!await BeeAppieSdk.EnsureStartedAsync())
        {
            Debug.LogWarning("BeeAppie could not start (offline, or a wrong app key?).");
            return;
        }

        // In a real game, ask first (e.g. on a privacy screen) and pass what the player chose.
        await BeeAppieSdk.SetConsentAsync(new Consent { EventPermission = true, IssuesPermission = true, SessionPermission = true });

        BeeAppieSdk.TrackEvent("quickstart_started", new Dictionary<string, string> { ["scene"] = gameObject.scene.name });

        try
        {
            var entry = await BeeAppieSdk.Client.SubmitScoreAsync("high-score", Random.Range(100, 1000));
            Debug.Log($"Score {entry.Score}: position {entry.Position}");

            // On the device at once (also offline), sent later with its time; never throws.
            BeeAppieSdk.Achievements.Unlock("first-win");
        }
        catch (BeeAppieApiException e)
        {
            Debug.LogWarning($"BeeAppie: {e.Message} ({e.Code})");
        }

        var config = BeeAppieSdk.Config;
        Debug.Log($"double_xp is {(config.IsEnabled("double_xp") ? "on" : "off")}");

        // The player's own data, the same on all their devices. Needs the app's "User data" switch, and is sent once the
        // player signs in (BeeAppieSdk.SignInAsync); until then it stays on the device. Nothing here throws.
        var settings = BeeAppieSdk.UserData.Get("settings", "game") ?? new JObject();
        settings["launches"] = (settings.Value<int?>("launches") ?? 0) + 1;
        BeeAppieSdk.UserData.Put("settings", "game", settings);
        Debug.Log($"Launches: {settings["launches"]}, {BeeAppieSdk.UserData.PendingWrites} write(s) waiting");

        // Access granted in the dashboard (Users), next to the game's own store purchases.
        await BeeAppieSdk.RefreshEntitlementsAsync();
        Debug.Log(BeeAppieSdk.HasEntitlement("premium") ? "Premium unlocked" : "No premium (grant it to this player in the dashboard)");
    }
}
